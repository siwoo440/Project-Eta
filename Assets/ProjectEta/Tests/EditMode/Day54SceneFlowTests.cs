using System.IO; // 소스 회귀 검사 사용
using NUnit.Framework; // EditMode 테스트 사용
using UnityEditor; // Build Settings 씬 목록 검사
using UnityEngine; // Application 경로 사용
using ProjectEta.Run; // RunSaveData·RunFlowPhase 사용
using ProjectEta.SceneFlow; // 54일차 씬 전환 구조 사용

namespace ProjectEta.Tests.EditMode
{
    public class Day54SceneFlowTests
    {
        [Test]
        public void SceneTransitionGate_BlocksDuplicateUntilCompleted()
        {
            var gate = new SceneTransitionGate(); // 독립 씬 전환 게이트 생성

            Assert.IsTrue(gate.TryBegin()); // 첫 전환 요청 허용 검증
            Assert.IsTrue(gate.IsTransitioning); // 전환 중 상태 검증
            Assert.IsFalse(gate.TryBegin()); // 중복 전환 요청 차단 검증

            gate.Complete(); // 씬 로드 완료 상태 재현

            Assert.IsFalse(gate.IsTransitioning); // 전환 완료 상태 검증
            Assert.IsTrue(gate.TryBegin()); // 완료 후 새 전환 요청 허용 검증
        }

        [Test]
        public void RunSaveSystem_ContinueValidation_AcceptsStableMapCheckpoint()
        {
            RunSaveData data = CreateContinueData(RunFlowPhase.Map, selectedNodeId: string.Empty); // 선택 전 Map 안전 세이브 생성

            Assert.IsTrue(RunSaveSystem.IsContinueDataValid(data)); // 안정적인 Map 세이브 이어하기 허용 검증
        }

        [Test]
        public void RunSaveSystem_ContinueValidation_AcceptsSelectedNonBattleCheckpoint()
        {
            RunSaveData reward = CreateContinueData(RunFlowPhase.Reward, selectedNodeId: "depth_2_1_reward"); // Reward 노드 진입 세이브 생성
            RunSaveData shop = CreateContinueData(RunFlowPhase.Shop, selectedNodeId: "depth_2_1_shop"); // Shop 노드 진입 세이브 생성
            RunSaveData eventData = CreateContinueData(RunFlowPhase.Event, selectedNodeId: "depth_2_1_event"); // Event 노드 진입 세이브 생성

            Assert.IsTrue(RunSaveSystem.IsContinueDataValid(reward)); // Reward 이어하기 허용 검증
            Assert.IsTrue(RunSaveSystem.IsContinueDataValid(shop)); // Shop 이어하기 허용 검증
            Assert.IsTrue(RunSaveSystem.IsContinueDataValid(eventData)); // Event 이어하기 허용 검증
        }

        [Test]
        public void RunSaveSystem_ContinueValidation_RejectsBattleOldAndIncompleteData()
        {
            RunSaveData battle = CreateContinueData(RunFlowPhase.Battle, selectedNodeId: string.Empty); // 전투 중 세이브 생성
            RunSaveData old = CreateContinueData(RunFlowPhase.Map, selectedNodeId: string.Empty); // 구버전 세이브 생성 준비
            RunSaveData incomplete = CreateContinueData(RunFlowPhase.Map, selectedNodeId: string.Empty); // 손상 지도 세이브 생성 준비
            old.saveVersion = RunSaveData.CurrentVersion - 1; // 구버전 포맷 적용
            incomplete.routeMap.currentNodeId = string.Empty; // 현재 노드 ID 제거

            Assert.IsFalse(RunSaveSystem.IsContinueDataValid(battle)); // 전투 중 세이브 이어하기 차단 검증
            Assert.IsFalse(RunSaveSystem.IsContinueDataValid(old)); // 구버전 자동 이어하기 차단 검증
            Assert.IsFalse(RunSaveSystem.IsContinueDataValid(incomplete)); // 불완전 지도 세이브 차단 검증
        }

        [Test]
        public void SceneRuntimeBootstrap_EnsuresLateLoadedBattleLegacyAutocreators()
        {
            string sourcePath = Path.Combine(Application.dataPath, "ProjectEta/Scripts/SceneFlow/SceneRuntimeBootstrap.cs"); // 실제 Bootstrap 소스 경로 계산
            string source = File.ReadAllText(sourcePath); // 현재 Bootstrap 구현 읽기

            StringAssert.Contains("EnsureComponent<Day41BattleRoomBootstrap>", source); // 41일차 전투방 늦은 Battle 로드 복구 검증
            StringAssert.Contains("EnsureComponent<PrototypeBoss37Spawner>", source); // 개발용 2x2 보스 자동 스폰 복구 검증
            StringAssert.Contains("EnsureComponent<LargePieceLifecycleController>", source); // 대형 기물 점유 생명주기 복구 검증
            StringAssert.Contains("EnsureComponent<LargePiecePlayerAttackBridge>", source); // 대형 기물 클릭 공격 브리지 복구 검증
            StringAssert.Contains("EnsureComponent<LargePieceTurnEndStatusBridge>", source); // 대형 기물 상태 정산 브리지 복구 검증
            StringAssert.Contains("EnsureComponent<EnemyAITurnDriver>", source); // 보스 AI·HP·페이즈 UI 연결 복구 검증
        }

        [Test]
        public void BuildSettings_ContainsOnlyBootMainMenuBattleInRuntimeOrder()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes; // 현재 Unity Build Settings 씬 목록 조회

            Assert.AreEqual(3, scenes.Length); // 정식 런타임 씬 3개만 포함 검증
            Assert.IsTrue(scenes[0].enabled); // Boot 활성화 검증
            Assert.IsTrue(scenes[1].enabled); // MainMenu 활성화 검증
            Assert.IsTrue(scenes[2].enabled); // Battle 활성화 검증
            Assert.AreEqual(SceneFlowController.BootScenePath, scenes[0].path); // Boot 0번 씬 검증
            Assert.AreEqual(SceneFlowController.MainMenuScenePath, scenes[1].path); // MainMenu 1번 씬 검증
            Assert.AreEqual(SceneFlowController.BattleScenePath, scenes[2].path); // Battle 2번 씬 검증
        }

        private static RunSaveData CreateContinueData(RunFlowPhase phase, string selectedNodeId)
        {
            var data = new RunSaveData
            {
                saveVersion = RunSaveData.CurrentVersion, // 최신 저장 포맷 적용
                runId = "day54_test_run", // 정상 런 고유 ID 적용
                currentRound = 2, // 테스트 현재 단계 적용
                flowPhase = (int)phase, // 테스트 Run Flow 적용
                routeMap = new RouteMapSaveData
                {
                    mapSeed = 540054, // 테스트 Map Seed 적용
                    currentDepth = 2, // 테스트 현재 깊이 적용
                    currentNodeId = string.IsNullOrWhiteSpace(selectedNodeId) ? "depth_2_0_battle" : selectedNodeId, // 선택 노드 진입 시 현재 King 노드도 같은 ID로 적용
                    selectedNodeId = selectedNodeId, // 요청된 선택 노드 상태 적용
                    kingX = 4, // 지도 King X 적용
                    kingY = 1 // 지도 King Y 적용
                }
            };

            data.routeMap.nodes.Add(new RouteNodeSaveData
            {
                nodeId = data.routeMap.currentNodeId, // 현재 노드 ID 등록
                x = 4, // 현재 노드 X 등록
                y = 1, // 현재 노드 Y 등록
                depth = 2, // 현재 노드 깊이 등록
                stageDefinitionId = string.IsNullOrWhiteSpace(selectedNodeId) ? "stage_2_battle" : selectedNodeId, // 현재 StageDefinition ID 등록
                visited = true // 현재 노드 방문 상태 등록
            });


            return data; // 완성 이어하기 테스트 DTO 반환
        }
    }
}
