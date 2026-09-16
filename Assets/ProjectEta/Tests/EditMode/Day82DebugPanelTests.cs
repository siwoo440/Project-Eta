using System.IO; // 소스 파일 존재 검사
using NUnit.Framework; // EditMode 테스트 사용
using UnityEngine; // Rect 사용
using ProjectEta.Battle; // TurnManager·BattleOutcome 사용
using ProjectEta.Debugging; // F1 디버그 패널 사용
using ProjectEta.Run; // RunFlowState 사용

namespace ProjectEta.Tests.EditMode // EditMode 테스트 네임스페이스
{ // 네임스페이스 범위
    public sealed class Day82DebugPanelTests // 82일차 F1 디버그 패널 회귀 테스트
    { // 테스트 클래스 범위
        [Test] // 왼쪽 패널 배치 API 검증
        public void CalculateWindowRect_1920화면에서_왼쪽좁은영역을사용한다() // 왼쪽 고정 패널 규칙 확인
        { // 테스트 범위
            Rect rect = ProjectEtaDebugWindow.CalculateWindowRect(1920, 1080); // 1920×1080 배치 계산

            Assert.That(rect.x, Is.LessThanOrEqualTo(20f)); // 왼쪽 여백 검증
            Assert.That(rect.y, Is.LessThanOrEqualTo(20f)); // 위쪽 여백 검증
            Assert.That(rect.width, Is.InRange(380f, 460f)); // 좁은 패널 폭 검증
            Assert.That(rect.height, Is.LessThanOrEqualTo(760f)); // 화면 가림 제한 검증
        } // 테스트 범위 종료

        [Test] // 작은 화면 대응 검증
        public void CalculateWindowRect_작은화면에서_화면경계를넘지않는다() // 작은 해상도 안전 배치 확인
        { // 테스트 범위
            Rect rect = ProjectEtaDebugWindow.CalculateWindowRect(640, 480); // 640×480 배치 계산

            Assert.That(rect.xMax, Is.LessThanOrEqualTo(640f)); // 가로 경계 검증
            Assert.That(rect.yMax, Is.LessThanOrEqualTo(480f)); // 세로 경계 검증
        } // 테스트 범위 종료

        [Test] // 프로젝트 기본 해상도 가림 비율 검증
        public void CalculateWindowRect_1024화면에서_폭340이하를사용한다() // 기본 해상도 왼쪽 여백 확보
        { // 테스트 범위
            Rect rect = ProjectEtaDebugWindow.CalculateWindowRect(1024, 768); // 프로젝트 기본 해상도 배치 계산

            Assert.That(rect.width, Is.LessThanOrEqualTo(340f)); // 기본 화면 가림 폭 제한 검증
        } // 테스트 범위 종료

        [TestCase(true, false, true)] // 에디터 허용 사례
        [TestCase(false, true, true)] // 개발 빌드 허용 사례
        [TestCase(false, false, false)] // 출시 빌드 차단 사례
        public void ShouldCreateDebugPanel_개발환경에서만_생성한다(bool isEditor, bool isDebugBuild, bool expected) // 빌드 정책 확인
        { // 테스트 범위
            bool actual = ProjectEtaDebugWindow.ShouldCreateDebugPanel(isEditor, isDebugBuild); // 생성 정책 계산

            Assert.That(actual, Is.EqualTo(expected)); // 기대 정책 검증
        } // 테스트 범위 종료

        [Test] // 전투 중 결과 버튼 허용 검증
        public void CanUseBattleResult_진행중전투에서_참을반환한다() // 전투 도구 활성 규칙 확인
        { // 테스트 범위
            var turnManager = new TurnManager(); // 진행 중 턴 상태 생성
            var flow = new RunFlowState(); // 전투 흐름 생성

            bool actual = ProjectEtaDebugWindow.CanUseBattleResult(turnManager, flow.Phase); // 사용 가능 여부 계산

            Assert.That(actual, Is.True); // 전투 중 허용 검증
        } // 테스트 범위 종료

        [Test] // 전투 종료 뒤 결과 버튼 차단 검증
        public void CanUseBattleResult_종료된전투에서_거짓을반환한다() // 중복 결과 차단 확인
        { // 테스트 범위
            var turnManager = new TurnManager(); // 진행 중 턴 상태 생성
            var flow = new RunFlowState(); // 전투 흐름 생성
            turnManager.EndBattle(BattleOutcome.Victory); // 전투 종료 처리

            bool actual = ProjectEtaDebugWindow.CanUseBattleResult(turnManager, flow.Phase); // 사용 가능 여부 계산

            Assert.That(actual, Is.False); // 종료 뒤 차단 검증
        } // 테스트 범위 종료

        [Test] // 상시 노출 UI 제거 검증
        public void StandaloneDebugButtons_통합뒤에는_소스파일이남지않는다() // 독립 디버그 Canvas 제거 확인
        { // 테스트 범위
            string resultButtonsPath = Path.Combine(Application.dataPath, "ProjectEta/Scripts/UI/DebugBattleResultButtons.cs"); // 기존 승패 버튼 경로 계산
            string speedButtonsPath = Path.Combine(Application.dataPath, "ProjectEta/Scripts/UI/DebugCombatSpeedButtons.cs"); // 기존 배속 버튼 경로 계산

            Assert.That(File.Exists(resultButtonsPath), Is.False); // 기존 승패 Canvas 제거 검증
            Assert.That(File.Exists(speedButtonsPath), Is.False); // 기존 배속 Canvas 제거 검증
        } // 테스트 범위 종료

        [Test] // 세 페이지 구조 검증
        public void PageCount_상태전투AI_세페이지를제공한다() // 탭 세분화 확인
        { // 테스트 범위
            int pageCount = ProjectEtaDebugWindow.PageCount; // 페이지 수 읽기

            Assert.That(pageCount, Is.EqualTo(3)); // 상태·전투·AI 구성 검증
        } // 테스트 범위 종료

        [Test] // 패널 클릭 관통 차단 좌표 검증
        public void IsScreenPointInsidePanel_왼쪽패널좌표에서_참을반환한다() // Input System 좌표 변환 확인
        { // 테스트 범위
            var panelRect = new Rect(12f, 12f, 420f, 720f); // 왼쪽 위 패널 영역 생성
            var screenPoint = new Vector2(100f, 900f); // Input System 왼쪽 아래 기준 좌표 생성

            bool actual = ProjectEtaDebugWindow.IsScreenPointInsidePanel(panelRect, 1080, screenPoint); // 패널 내부 여부 계산

            Assert.That(actual, Is.True); // 패널 내부 판정 검증
        } // 테스트 범위 종료

        [Test] // 대형 보스 클릭 관통 차단 검증
        public void LargePieceAttackBridge_F1패널위클릭을_공격으로전달하지않는다() // 대형 보스 별도 입력 경로 보호
        { // 테스트 범위
            string sourcePath = Path.Combine(Application.dataPath, "ProjectEta/Scripts/Boss/LargePiecePlayerAttackBridge.cs"); // 대형 보스 입력 브리지 경로 계산
            string source = File.ReadAllText(sourcePath); // 대형 보스 입력 구현 읽기

            StringAssert.Contains("ProjectEtaDebugWindow.IsPointerOverPanel", source); // F1 패널 클릭 차단 규칙 연결 검증
        } // 테스트 범위 종료

        [TestCase(RunFlowPhase.Battle, 1f, true)] // 진행 중 전투 허용 사례
        [TestCase(RunFlowPhase.Battle, 0f, false)] // 일시정지 차단 사례
        [TestCase(RunFlowPhase.Map, 1f, false)] // 지도 흐름 차단 사례
        public void CanChangeCombatSpeed_진행중전투에서만_참을반환한다(RunFlowPhase flowPhase, float timeScale, bool expected) // 배속 변경 안전 규칙 확인
        { // 테스트 범위
            bool actual = ProjectEtaDebugWindow.CanChangeCombatSpeed(flowPhase, timeScale); // 배속 변경 가능 여부 계산

            Assert.That(actual, Is.EqualTo(expected)); // 기대 규칙 검증
        } // 테스트 범위 종료

        [Test] // 카메라 입력 관통 차단 검증
        public void SeatedCameraRig_F1패널위포인터를_회전과줌으로전달하지않는다() // 상대 시점 별도 입력 경로 보호
        { // 테스트 범위
            string sourcePath = Path.Combine(Application.dataPath, "ProjectEta/Scripts/Environment/Day41SeatedCameraRig.cs"); // 좌석 카메라 리그 경로 계산
            string source = File.ReadAllText(sourcePath); // 카메라 입력 구현 읽기

            StringAssert.Contains("ProjectEtaDebugWindow.IsPointerOverPanel", source); // F1 패널 카메라 입력 차단 규칙 연결 검증
        } // 테스트 범위 종료
    } // 테스트 클래스 종료
} // 네임스페이스 종료
