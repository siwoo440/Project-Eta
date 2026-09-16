using System; // Type 목록 사용
using System.IO; // 중앙 부트스트랩 소스 검사 사용
using System.Linq; // 특성 검색 사용
using System.Reflection; // 비공개 정적 메서드 검사 사용
using NUnit.Framework; // EditMode 테스트 사용
using UnityEngine; // RuntimeInitializeOnLoadMethodAttribute 사용
using ProjectEta.AI; // 적 AI 관리자 사용
using ProjectEta.Battle; // 전투 관리자 사용
using ProjectEta.Board; // 경로 지도 관리자 사용
using ProjectEta.Boss; // 보스 관리자 사용
using ProjectEta.Cards; // 시작 덱 관리자 사용
using ProjectEta.Environment; // 전투방 관리자 사용
using ProjectEta.King; // 킹 관리자 사용
using ProjectEta.Meta; // 메타 관리자 사용
using ProjectEta.Pieces; // 전투 연출 관리자 사용
using ProjectEta.Round; // 라운드 관리자 사용
using ProjectEta.Run; // 런 관리자 사용
using ProjectEta.SceneFlow; // 런타임 진단 사용
using ProjectEta.UI; // 전투 UI 관리자 사용

namespace ProjectEta.Tests.EditMode // EditMode 테스트 네임스페이스
{ // 네임스페이스 범위
    public sealed class Day83RuntimeBootstrapTests // 83일차 런타임 초기화 회귀 테스트
    { // 테스트 클래스 범위
        [Test] // 누락·중복 집계 검증
        public void BattleRuntimeDiagnostics_누락과중복을_정확히집계한다() // 비정상 초기화 진단 확인
        { // 테스트 범위
            RuntimeComponentStatus[] statuses = // 테스트 관리자 상태 목록
            { // 배열 범위
                new RuntimeComponentStatus("BattleController", 1), // 정상 관리자 상태
                new RuntimeComponentStatus("EnemyAITurnDriver", 0), // 누락 관리자 상태
                new RuntimeComponentStatus("RouteMapBoardController", 2) // 중복 관리자 상태
            }; // 배열 종료

            BattleRuntimeDiagnostics diagnostics = BattleRuntimeDiagnostics.Create("Battle", 12.5f, statuses); // 진단 결과 생성

            Assert.That(diagnostics.RequiredCount, Is.EqualTo(3)); // 필수 관리자 수 검증
            Assert.That(diagnostics.ReadyCount, Is.EqualTo(2)); // 준비 관리자 수 검증
            Assert.That(diagnostics.MissingNames, Is.EqualTo(new[] { "EnemyAITurnDriver" })); // 누락 목록 검증
            Assert.That(diagnostics.DuplicateNames, Is.EqualTo(new[] { "RouteMapBoardController ×2" })); // 중복 목록 검증
            Assert.That(diagnostics.IsHealthy, Is.False); // 비정상 상태 검증
        } // 테스트 범위 종료

        [Test] // 정상 집계 검증
        public void BattleRuntimeDiagnostics_모든관리자가하나이면_정상을반환한다() // 정상 초기화 진단 확인
        { // 테스트 범위
            RuntimeComponentStatus[] statuses = // 정상 관리자 상태 목록
            { // 배열 범위
                new RuntimeComponentStatus("BattleController", 1), // 정상 전투 관리자
                new RuntimeComponentStatus("EnemyAITurnDriver", 1), // 정상 AI 관리자
                new RuntimeComponentStatus("RouteMapBoardController", 1) // 정상 지도 관리자
            }; // 배열 종료

            BattleRuntimeDiagnostics diagnostics = BattleRuntimeDiagnostics.Create("Battle", 8f, statuses); // 정상 진단 결과 생성

            Assert.That(diagnostics.RequiredCount, Is.EqualTo(3)); // 필수 관리자 수 검증
            Assert.That(diagnostics.ReadyCount, Is.EqualTo(3)); // 준비 관리자 수 검증
            Assert.That(diagnostics.MissingNames, Is.Empty); // 누락 없음 검증
            Assert.That(diagnostics.DuplicateNames, Is.Empty); // 중복 없음 검증
            Assert.That(diagnostics.IsHealthy, Is.True); // 정상 상태 검증
        } // 테스트 범위 종료

        [Test] // 분산 자동 생성 제거 검증
        public void Battle자동생성기_중앙부트스트랩통합뒤_개별진입점이없다() // Battle 초기화 단일화 확인
        { // 테스트 범위
            Type[] centralizedTypes = // 중앙 관리 대상 형식 목록
            { // 배열 범위
                typeof(BattleController), // 전투 관리자
                typeof(RoundRuntimeController), // 라운드 관리자
                typeof(Day41BattleRoomBootstrap), // 전투방 관리자
                typeof(LargePieceLifecycleController), // 대형 기물 생명주기
                typeof(LargePiecePlayerAttackBridge), // 대형 기물 공격 연결
                typeof(LargePieceTurnEndStatusBridge), // 대형 기물 상태 연결
                typeof(EnemyAITurnDriver), // 적 AI 관리자
                typeof(PrototypeBoss37Spawner), // 보스 생성 관리자
                typeof(PrototypePlayerDeck26Bootstrap), // 시작 덱 관리자
                typeof(RouteMapBoardController), // 경로 지도 관리자
                typeof(FullRouteMapPreviewController), // 전체 지도 관리자
                typeof(StageTransitionController), // 스테이지 전환 관리자
                typeof(CardRewardController), // 카드 보상 관리자
                typeof(StageActivityController), // 스테이지 활동 관리자
                typeof(MetaProgressRunResultController), // 메타 결과 관리자
                typeof(KingAbilityController), // 킹 능력 관리자
                typeof(RunPersistenceController), // 저장 관리자
                typeof(RoundStateBattleBridge), // 라운드 상태 연결
                typeof(LethalAttackVisualBridge), // 치명타 연출 연결
                typeof(PlayerActionTurnDelayController), // 턴 지연 관리자
                typeof(BattleInteractionStatusUI), // 전투 안내 UI
                typeof(CombatFloatingTextUI), // 전투 숫자 UI
                typeof(Day64FusionUI), // 합성 UI
                typeof(Day65StageActivityHUD), // 활동 HUD
                typeof(Day66HandCardMotionController), // 손패 모션
                typeof(Day66BoardTransitionFX), // 보드 전환 연출
                typeof(Day67BattleAnnouncementCoordinator), // 전투 알림 관리자
                typeof(FirstRunTutorialController), // 최초 튜토리얼
                typeof(SystemToastUI) // 시스템 알림 UI
            }; // 배열 종료

            foreach (Type type in centralizedTypes) // 중앙 관리 형식 순회
            { // 반복 범위
                MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public); // 정적 메서드 조회
                bool hasBattleAutoCreate = methods.Any(method => method.Name == "AutoCreateForBattleScene" && method.GetCustomAttribute<RuntimeInitializeOnLoadMethodAttribute>() != null); // 분산 자동 진입점 확인
                Assert.That(hasBattleAutoCreate, Is.False, $"{type.Name}에 개별 Battle 자동 생성기가 남아 있습니다."); // 개별 진입점 제거 검증
            } // 반복 종료
        } // 테스트 범위 종료
        [Test] // 중앙 생성 등록 회귀 검증
        public void SceneRuntimeBootstrap_Battle관리대상을_중앙생성단계에등록한다() // 중앙 초기화 누락 방지 확인
        { // 테스트 범위
            string sourcePath = Path.Combine(Application.dataPath, "ProjectEta/Scripts/SceneFlow/SceneRuntimeBootstrap.cs"); // 중앙 부트스트랩 소스 경로 계산
            string source = File.ReadAllText(sourcePath); // 현재 중앙 부트스트랩 구현 읽기
            string[] requiredTokens = // 필수 중앙 생성 호출 목록
            { // 배열 범위
                "EnsureComponent<BattleController>", // 전투 관리자 생성 호출
                "EnsureComponent<RoundRuntimeController>", // 라운드 관리자 생성 호출
                "EnsureComponent<RoundStateBattleBridge>", // 라운드 연결 생성 호출
                "EnsureComponent<PrototypePlayerDeck26Bootstrap>", // 시작 덱 생성 호출
                "EnsureComponent<Day41BattleRoomBootstrap>", // 전투방 생성 호출
                "EnsureComponent<LargePieceLifecycleController>", // 대형 기물 생명주기 생성 호출
                "EnsureComponent<LargePiecePlayerAttackBridge>", // 대형 기물 공격 연결 생성 호출
                "EnsureComponent<LargePieceTurnEndStatusBridge>", // 대형 기물 상태 연결 생성 호출
                "EnsureComponent<EnemyAITurnDriver>", // 적 AI 생성 호출
                "EnsureComponent<PrototypeBoss37Spawner>", // 보스 생성 호출
                "EnsureComponent<PlayerActionTurnDelayController>", // 턴 지연 생성 호출
                "EnsureComponent<LethalAttackVisualBridge>", // 치명타 연출 생성 호출
                "EnsureKingRuntime();", // 킹 복합 호스트 생성 호출
                "EnsureComponent<RouteMapBoardController>", // 경로 지도 생성 호출
                "EnsureComponent<FullRouteMapPreviewController>", // 전체 지도 생성 호출
                "EnsureComponent<StageTransitionController>", // 스테이지 전환 생성 호출
                "EnsureComponent<CardRewardController>", // 카드 보상 생성 호출
                "EnsureComponent<StageActivityController>", // 스테이지 활동 생성 호출
                "EnsureComponent<MetaProgressRunResultController>", // 메타 결과 생성 호출
                "EnsureComponent<RunPersistenceController>", // 저장 관리자 생성 호출
                "EnsureComponent<BattleInteractionStatusUI>", // 전투 안내 UI 생성 호출
                "EnsureComponent<CombatFloatingTextUI>", // 전투 숫자 UI 생성 호출
                "EnsureComponent<Day64FusionUI>", // 합성 UI 생성 호출
                "EnsureComponent<Day65StageActivityHUD>", // 활동 HUD 생성 호출
                "EnsureComponent<Day66HandCardMotionController>", // 손패 모션 생성 호출
                "EnsureComponent<Day66BoardTransitionFX>", // 보드 전환 연출 생성 호출
                "EnsureBattleAnnouncementRuntime();", // 전투 알림 복합 호스트 생성 호출
                "EnsureComponent<FirstRunTutorialController>", // 튜토리얼 생성 호출
                "EnsureComponent<SystemToastUI>" // 시스템 Toast 생성 호출
            }; // 배열 종료

            foreach (string token in requiredTokens) // 필수 생성 호출 순회
            { // 반복 범위
                StringAssert.Contains(token, source, $"중앙 Battle 초기화 등록 누락: {token}"); // 중앙 생성 호출 존재 검증
            } // 반복 종료
        } // 테스트 범위 종료
        [Test] // 기존 전투 알림 호스트 보강 검증
        public void Day67전투알림_코디네이터뒤UI가추가되어도_참조를복구한다() // 초기화 순서 차이 대응 확인
        { // 테스트 범위
            GameObject host = new GameObject("Day67Announcement_Existing_Test"); // 기존 씬 호스트 생성

            try // 테스트 객체 정리 보장
            { // 정리 범위
                Day67BattleAnnouncementCoordinator coordinator = host.AddComponent<Day67BattleAnnouncementCoordinator>(); // UI 없는 코디네이터 우선 생성
                Day67BattleAnnouncementUI announcementUI = host.AddComponent<Day67BattleAnnouncementUI>(); // 중앙 부트스트랩의 UI 보강 재현
                MethodInfo update = typeof(Day67BattleAnnouncementCoordinator).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 갱신 메서드 조회
                FieldInfo uiField = typeof(Day67BattleAnnouncementCoordinator).GetField("_announcementUI", BindingFlags.Instance | BindingFlags.NonPublic); // 내부 UI 참조 필드 조회
                Assert.That(update, Is.Not.Null); // 갱신 메서드 존재 검증
                Assert.That(uiField, Is.Not.Null); // UI 참조 필드 존재 검증

                update.Invoke(coordinator, null); // 보강 다음 프레임 갱신 재현

                Assert.That(uiField.GetValue(coordinator), Is.SameAs(announcementUI)); // 뒤늦게 추가된 UI 참조 복구 검증
            } // 정리 범위 종료
            finally // 성공·실패 공통 정리
            { // 정리 범위
                UnityEngine.Object.DestroyImmediate(host); // 테스트 호스트 제거
            } // 정리 범위 종료
        } // 테스트 범위 종료
    } // 테스트 클래스 종료
} // 네임스페이스 종료
