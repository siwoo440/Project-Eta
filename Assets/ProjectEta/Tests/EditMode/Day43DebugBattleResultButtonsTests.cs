using NUnit.Framework; // EditMode 테스트·Assert 사용
using ProjectEta.Battle; // TurnManager·BattleOutcome 사용
using ProjectEta.Run; // RunFlowState·RunFlowPhase 사용
using ProjectEta.UI; // DebugBattleResultButtons 규칙 사용

namespace ProjectEta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public class Day43DebugBattleResultButtonsTests // 43일차 개발용 승리·패배 버튼 회귀 테스트
    {
        [Test] // 진행 중 전투에서 버튼 사용 가능 여부 검증
        public void CanUseResultButtons_DuringBattleFlow_ReturnsTrue() // 전투 흐름에서 결과 버튼 허용 확인
        {
            var turnManager = new TurnManager(); // 전투 턴 상태 생성
            var flow = new RunFlowState(); // 기본 Battle 흐름 생성

            bool canUse = DebugBattleResultButtons.CanUseResultButtons(turnManager, flow.Phase); // 사용 가능 여부 계산

            Assert.IsTrue(canUse); // 전투 중 버튼 활성화 검증
        }

        [Test] // 전투 종료 뒤 결과 버튼 중복 사용 차단 검증
        public void CanUseResultButtons_AfterBattleEnded_ReturnsFalse() // 전투 종료 상태에서 결과 버튼 차단 확인
        {
            var turnManager = new TurnManager(); // 전투 턴 상태 생성
            var flow = new RunFlowState(); // 기본 Battle 흐름 생성
            turnManager.EndBattle(BattleOutcome.Victory); // 전투를 승리로 종료

            bool canUse = DebugBattleResultButtons.CanUseResultButtons(turnManager, flow.Phase); // 사용 가능 여부 계산

            Assert.IsFalse(canUse); // 중복 결과 입력 차단 검증
        }

        [Test] // 지도 흐름 진입 뒤 결과 버튼 사용 차단 검증
        public void CanUseResultButtons_InMapFlow_ReturnsFalse() // 지도 상태에서 개발 결과 버튼 차단 확인
        {
            var turnManager = new TurnManager(); // 전투 턴 상태 생성
            var flow = new RunFlowState(); // 기본 Battle 흐름 생성
            flow.EnterMap(); // 지도 선택 흐름 진입

            bool canUse = DebugBattleResultButtons.CanUseResultButtons(turnManager, flow.Phase); // 사용 가능 여부 계산

            Assert.IsFalse(canUse); // 지도 상태 버튼 차단 검증
        }

        [Test] // 런 완료 뒤 결과 버튼 사용 차단 검증
        public void CanUseResultButtons_AfterRunCompleted_ReturnsFalse() // 완료 상태에서 개발 결과 버튼 차단 확인
        {
            var turnManager = new TurnManager(); // 전투 턴 상태 생성
            var flow = new RunFlowState(); // 기본 Battle 흐름 생성
            flow.CompleteRun(); // 런 완료 상태 전환

            bool canUse = DebugBattleResultButtons.CanUseResultButtons(turnManager, flow.Phase); // 사용 가능 여부 계산

            Assert.IsFalse(canUse); // 완료 뒤 결과 입력 차단 검증
        }
    }
}
