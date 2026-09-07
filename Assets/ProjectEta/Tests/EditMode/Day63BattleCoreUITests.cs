using NUnit.Framework; // EditMode 테스트 특성과 Assert를 사용하기 위한 네임스페이스
using ProjectEta.Battle; // TurnState를 사용하기 위한 네임스페이스
using ProjectEta.UI; // 63일차 전투 UI 표시 계산을 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class Day63BattleCoreUITests // 63일차 전투 핵심 UI 표시 규칙 회귀 테스트 모음
    {
        [Test] // 최초 배치에서 킹 우선 안내가 표시되는지 검증하는 테스트
        public void BuildInstruction_InitialDeploymentBeforeKing_RequiresKing() // 최초 배치 킹 필수 문구 검증 메서드
        {
            string result = BattleInteractionPresentation.BuildInstruction(TurnState.DeploymentTurn, true, false, false, 0, 0); // 킹 미배치 최초 배치 상태의 안내 문구 계산
            Assert.That(result, Does.Contain("KING FIRST")); // 킹 우선 안내 포함 여부 검증
        }

        [Test] // 선택 기물의 이동·공격 후보 수가 안내에 포함되는지 검증하는 테스트
        public void BuildInstruction_SelectedPiece_ShowsMoveAndAttackCounts() // 선택 기물 후보 수 문구 검증 메서드
        {
            string result = BattleInteractionPresentation.BuildInstruction(TurnState.PlayerTurn, false, true, true, 4, 2); // 이동 4칸·공격 2칸 선택 상태 안내 계산
            Assert.That(result, Does.Contain("MOVE 4")); // 이동 후보 수 표시 검증
            Assert.That(result, Does.Contain("ATTACK 2")); // 공격 후보 수 표시 검증
        }

        [Test] // 적 턴에서 플레이어 조작 안내 대신 적 행동 안내가 표시되는지 검증하는 테스트
        public void BuildInstruction_EnemyTurn_ShowsEnemyState() // 적 턴 문구 검증 메서드
        {
            string result = BattleInteractionPresentation.BuildInstruction(TurnState.EnemyTurn, false, true, false, 0, 0); // 적 턴 상태 안내 계산
            Assert.That(result, Does.StartWith("ENEMY TURN")); // 적 턴 전용 안내 시작 문구 검증
        }

        [Test] // 전투 종료 후 조작 안내가 숨겨지는지 검증하는 테스트
        public void BuildInstruction_BattleEnded_ReturnsEmpty() // 전투 종료 숨김 문구 검증 메서드
        {
            string result = BattleInteractionPresentation.BuildInstruction(TurnState.BattleEnded, false, true, false, 0, 0); // 전투 종료 상태 안내 계산
            Assert.That(result, Is.Empty); // 종료 상태 빈 문구 반환 검증
        }

        [Test] // 실제 피해량이 음수 형태의 Floating Text로 변환되는지 검증하는 테스트
        public void FormatDamage_PositiveAmount_ReturnsNegativeLabel() // 피해량 표시 문자열 검증 메서드
        {
            string result = CombatFloatingTextUI.FormatDamage(7); // 7 피해 화면 표시 문자열 계산
            Assert.AreEqual("-7", result); // 최종 피해 숫자 문구 검증
        }

        [Test] // 0 피해는 불필요한 Floating Text를 만들지 않도록 빈 문자열인지 검증하는 테스트
        public void FormatDamage_ZeroAmount_ReturnsEmpty() // 0 피해 숨김 문자열 검증 메서드
        {
            string result = CombatFloatingTextUI.FormatDamage(0); // 0 피해 화면 표시 문자열 계산
            Assert.That(result, Is.Empty); // 0 피해 표시 생략용 빈 문자열 검증
        }
    }
}
