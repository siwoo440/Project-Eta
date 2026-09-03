using NUnit.Framework; // EditMode 단위 테스트 기능을 사용하기 위한 네임스페이스
using ProjectEta.Battle; // TurnManager와 TurnState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class TurnManagerTests // 10일차 턴 흐름과 행동 권한을 검증하는 테스트 모음
    {
        [Test] // 새 전투가 1턴 플레이어 턴으로 시작하는지 검증
        public void NewTurnManager_StartsAtPlayerTurnOne()
        {
            var turnManager = new TurnManager(); // 새 턴 매니저 생성

            Assert.That(turnManager.CurrentState, Is.EqualTo(TurnState.PlayerTurn)); // 시작 상태가 플레이어 턴인지 검증
            Assert.That(turnManager.TurnNumber, Is.EqualTo(1)); // 시작 턴 번호가 1인지 검증
            Assert.That(turnManager.CanPlayerAct, Is.True); // 첫 플레이어 행동이 가능한지 검증
            Assert.That(turnManager.HasPlayerActed, Is.False); // 아직 행동하지 않은 상태인지 검증
        }

        [Test] // 플레이어 행동 1회가 적 턴으로 전환되고 중복 행동을 막는지 검증
        public void TryCompletePlayerAction_AllowsOnlyOneActionAndChangesToEnemyTurn()
        {
            var turnManager = new TurnManager(); // 새 턴 매니저 생성

            bool firstResult = turnManager.TryCompletePlayerAction(); // 첫 플레이어 행동 완료 처리
            bool secondResult = turnManager.TryCompletePlayerAction(); // 같은 턴에 두 번째 행동 완료를 다시 시도

            Assert.That(firstResult, Is.True); // 첫 행동 완료는 성공해야 함
            Assert.That(secondResult, Is.False); // 같은 턴의 두 번째 행동은 실패해야 함
            Assert.That(turnManager.CurrentState, Is.EqualTo(TurnState.EnemyTurn)); // 첫 행동 후 적 턴이어야 함
            Assert.That(turnManager.HasPlayerActed, Is.True); // 이번 턴에 행동했음을 기억해야 함
            Assert.That(turnManager.CanPlayerAct, Is.False); // 적 턴에는 플레이어 행동이 불가능해야 함
        }

        [Test] // 적 턴 종료 후 다음 플레이어 턴으로 돌아오며 턴 번호가 증가하는지 검증
        public void CompleteEnemyTurn_ReturnsToPlayerAndIncrementsTurnNumber()
        {
            var turnManager = new TurnManager(); // 새 턴 매니저 생성
            turnManager.TryCompletePlayerAction(); // 플레이어 행동을 끝내 적 턴으로 전환

            bool result = turnManager.CompleteEnemyTurn(); // 적 턴 종료 처리

            Assert.That(result, Is.True); // 정상적인 적 턴 종료는 성공해야 함
            Assert.That(turnManager.CurrentState, Is.EqualTo(TurnState.PlayerTurn)); // 다음 플레이어 턴으로 돌아와야 함
            Assert.That(turnManager.TurnNumber, Is.EqualTo(2)); // 턴 번호가 2로 증가해야 함
            Assert.That(turnManager.HasPlayerActed, Is.False); // 새 플레이어 턴에서는 행동 여부가 초기화돼야 함
            Assert.That(turnManager.CanPlayerAct, Is.True); // 새 플레이어 턴에서 다시 행동할 수 있어야 함
        }

        [Test] // 전투 종료 상태에서는 더 이상 어느 쪽 턴도 진행되지 않는지 검증
        public void EndBattle_BlocksFurtherTurnProgress()
        {
            var turnManager = new TurnManager(); // 새 턴 매니저 생성
            turnManager.EndBattle(); // 전투 종료 처리

            bool playerResult = turnManager.TryCompletePlayerAction(); // 종료 후 플레이어 행동을 시도
            bool enemyResult = turnManager.CompleteEnemyTurn(); // 종료 후 적 턴 완료를 시도

            Assert.That(turnManager.CurrentState, Is.EqualTo(TurnState.BattleEnded)); // 상태가 전투 종료로 유지돼야 함
            Assert.That(playerResult, Is.False); // 종료 후 플레이어 행동은 실패해야 함
            Assert.That(enemyResult, Is.False); // 종료 후 적 턴 진행도 실패해야 함
            Assert.That(turnManager.CanPlayerAct, Is.False); // 종료 상태에서는 플레이어 행동이 불가능해야 함
        }
    }
}
