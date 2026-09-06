using NUnit.Framework; // EditMode 테스트 사용
using ProjectEta.Run; // RunSaveData·RunContinueInfo 사용
using ProjectEta.UI; // MainMenuNavigationState 사용

namespace ProjectEta.Tests.EditMode
{
    public class Day55MainMenuTests
    {
        [Test]
        public void MainMenuNavigation_NewGameWithContinueData_RequiresConfirmation()
        {
            var navigation = new MainMenuNavigationState(); // 독립 메뉴 내비게이션 상태 생성

            bool startImmediately = navigation.RequestNewGame(true); // 이어하기 데이터가 있는 새 게임 요청

            Assert.IsFalse(startImmediately); // 기존 런이 있으면 즉시 시작하지 않음 검증
            Assert.AreEqual(MainMenuPanel.NewGameConfirm, navigation.CurrentPanel); // 새 게임 확인 팝업 상태 검증
        }

        [Test]
        public void MainMenuNavigation_NewGameWithoutContinueData_StartsImmediately()
        {
            var navigation = new MainMenuNavigationState(); // 독립 메뉴 내비게이션 상태 생성

            bool startImmediately = navigation.RequestNewGame(false); // 이어하기 데이터가 없는 새 게임 요청

            Assert.IsTrue(startImmediately); // 저장 데이터가 없으면 즉시 시작 허용 검증
            Assert.AreEqual(MainMenuPanel.Main, navigation.CurrentPanel); // 메인 패널 상태 유지 검증
        }

        [Test]
        public void MainMenuNavigation_BackFromSubPanel_ReturnsMain()
        {
            var navigation = new MainMenuNavigationState(); // 독립 메뉴 내비게이션 상태 생성
            navigation.ShowSettings(); // 설정 패널 진입

            Assert.IsTrue(navigation.TryBack()); // 뒤로가기 처리 성공 검증
            Assert.AreEqual(MainMenuPanel.Main, navigation.CurrentPanel); // 메인 패널 복귀 검증
        }

        [Test]
        public void RunSaveSystem_ContinueInfo_ExposesMenuSummary()
        {
            RunSaveData data = CreateContinueData(RunFlowPhase.Shop); // 안전 Shop 체크포인트 저장 데이터 생성

            bool created = RunSaveSystem.TryCreateContinueInfo(data, out RunContinueInfo info); // 메뉴 표시용 이어하기 요약 생성

            Assert.IsTrue(created); // 유효 저장 데이터 요약 생성 검증
            Assert.AreEqual(4, info.Stage); // 현재 Stage 요약 검증
            Assert.AreEqual(RunFlowPhase.Shop, info.FlowPhase); // 현재 흐름 요약 검증
            Assert.AreEqual(73, info.Gold); // 현재 Gold 요약 검증
            Assert.AreEqual(2, info.KingHp); // 현재 King HP 요약 검증
            Assert.AreEqual(3, info.VisitedNodeCount); // 방문 경로 개수 요약 검증
        }

        private static RunSaveData CreateContinueData(RunFlowPhase phase)
        {
            var data = new RunSaveData
            {
                saveVersion = RunSaveData.CurrentVersion, // 최신 저장 버전 적용
                runId = "day55_menu_test", // 테스트 RunId 적용
                currentRound = 4, // 테스트 현재 Stage 적용
                kingHp = 2, // 테스트 King HP 적용
                runCurrency = 73, // 테스트 Gold 적용
                flowPhase = (int)phase, // 테스트 Run Flow 적용
                routeMap = new RouteMapSaveData
                {
                    mapSeed = 550055, // 테스트 Map Seed 적용
                    currentDepth = 4, // 테스트 Route Depth 적용
                    currentNodeId = "depth_4_1_shop", // 현재 King 노드 적용
                    selectedNodeId = "depth_4_1_shop", // Shop 선택 노드 적용
                    kingX = 4, // 지도 King X 적용
                    kingY = 3 // 지도 King Y 적용
                }
            };

            data.routeMap.nodes.Add(new RouteNodeSaveData
            {
                nodeId = "depth_4_1_shop", // 현재 노드 ID 등록
                x = 4, // 현재 노드 X 등록
                y = 3, // 현재 노드 Y 등록
                depth = 4, // 현재 노드 Depth 등록
                stageDefinitionId = "stage_4_shop", // 현재 StageDefinition 등록
                visited = true // 현재 노드 방문 상태 등록
            });
            data.routeMap.visitedNodeIds.Add("depth_2_0_battle"); // 방문 경로 1 등록
            data.routeMap.visitedNodeIds.Add("depth_3_1_reward"); // 방문 경로 2 등록
            data.routeMap.visitedNodeIds.Add("depth_4_1_shop"); // 방문 경로 3 등록
            return data; // 완성 테스트 저장 데이터 반환
        }
    }
}
