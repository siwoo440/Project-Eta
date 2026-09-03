using NUnit.Framework; // EditMode 단위 테스트 기능을 사용하기 위한 네임스페이스
using ProjectEta.Battle; // TurnManager와 TurnState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class TurnManagerTests // 초기 배치·자유 배치·일반 턴 흐름을 검증하는 테스트 모음
    {
        [Test] // 새 전투가 초기 배치 턴으로 시작하는지 검증
        public void NewTurnManager_StartsAtInitialDeploymentTurn()
        {
            var turnManager = new TurnManager(); // 새 턴 매니저 생성

            Assert.That(turnManager.CurrentState, Is.EqualTo(TurnState.DeploymentTurn)); // 전투 시작 즉시 배치 턴
            Assert.That(turnManager.TurnNumber, Is.EqualTo(1)); // 첫 일반 턴 번호는 1로 대기
            Assert.That(turnManager.IsInitialDeployment, Is.True); // 시작 배치 상태
            Assert.That(turnManager.CanDeploy, Is.True); // 자유 배치 가능
        }

        [Test] // 시작 배치는 킹을 놓기 전에는 종료할 수 없는지 검증
        public void InitialDeployment_CannotEndBeforeKingPlacement()
        {
            var turnManager = new TurnManager(); // 시작 배치 상태 생성

            bool result = turnManager.TryEndDeploymentTurn(); // 킹 없이 배치 턴 종료 시도

            Assert.That(result, Is.False); // 종료 실패
            Assert.That(turnManager.CurrentState, Is.EqualTo(TurnState.DeploymentTurn)); // 배치 턴 유지
            Assert.That(turnManager.IsInitialKingPlaced, Is.False); // 킹 미배치 상태
        }

        [Test] // 시작 배치에서 킹을 등록해도 즉시 일반 턴으로 넘어가지 않는지 검증
        public void InitialKingPlacement_DoesNotAutoEndDeploymentTurn()
        {
            var turnManager = new TurnManager(); // 시작 배치 상태 생성

            turnManager.MarkInitialKingPlaced(); // 킹 배치 완료 등록

            Assert.That(turnManager.CurrentState, Is.EqualTo(TurnState.DeploymentTurn)); // 여전히 배치 턴
            Assert.That(turnManager.CanDeploy, Is.True); // 추가 카드도 자유롭게 배치 가능
            Assert.That(turnManager.IsInitialKingPlaced, Is.True); // 킹 배치 완료 상태
        }

        [Test] // 킹 배치 후 사용자가 턴 종료하면 1턴 PlayerTurn이 시작되는지 검증
        public void InitialDeployment_AfterKingPlacement_EndTurnStartsPlayerTurnOne()
        {
            var turnManager = new TurnManager(); // 시작 배치 상태 생성
            turnManager.MarkInitialKingPlaced(); // 킹 배치 완료 등록

            bool result = turnManager.TryEndDeploymentTurn(); // 배치 턴 종료

            Assert.That(result, Is.True); // 종료 성공
            Assert.That(turnManager.CurrentState, Is.EqualTo(TurnState.PlayerTurn)); // 첫 일반 턴 시작
            Assert.That(turnManager.TurnNumber, Is.EqualTo(1)); // 초기 배치는 턴을 소비하지 않음
            Assert.That(turnManager.IsInitialDeployment, Is.False); // 시작 배치 종료
        }

        [Test] // 5턴 종료 후 주기 배치에서도 여러 장을 배치할 수 있도록 배치 가능 상태가 유지되는지 검증
        public void PeriodicDeployment_RemainsOpenUntilExplicitEnd()
        {
            var turnManager = CreateStartedBattle(); // 1턴 PlayerTurn 상태 생성
            AdvanceToPlayerTurn(turnManager, 5); // 5턴까지 진행
            Assert.That(turnManager.TryCompletePlayerAction(), Is.True); // 5턴 플레이어 행동 완료
            Assert.That(turnManager.CompleteEnemyTurn(), Is.True); // 주기 배치 턴 진입

            turnManager.RegisterDeployment(); // 첫 카드 배치 등록
            turnManager.RegisterDeployment(); // 두 번째 카드 배치 등록

            Assert.That(turnManager.CurrentState, Is.EqualTo(TurnState.DeploymentTurn)); // 여러 장 배치해도 계속 배치 턴
            Assert.That(turnManager.CanDeploy, Is.True); // 계속 추가 배치 가능
            Assert.That(turnManager.DeployedCardCount, Is.EqualTo(2)); // 배치 카드 수 누적
        }

        [Test] // 주기 배치 턴은 사용자가 명시적으로 종료해야 다음 턴으로 넘어가는지 검증
        public void PeriodicDeployment_ExplicitEndStartsNextPlayerTurn()
        {
            var turnManager = CreatePeriodicDeploymentTurn(); // 5턴 종료 후 주기 배치 상태 생성
            turnManager.RegisterDeployment(); // 카드 1장 배치

            Assert.That(turnManager.TryEndDeploymentTurn(), Is.True); // 명시적 배치 턴 종료
            Assert.That(turnManager.CurrentState, Is.EqualTo(TurnState.PlayerTurn)); // 다음 일반 턴
            Assert.That(turnManager.TurnNumber, Is.EqualTo(6)); // 6턴 시작
        }

        [Test] // 주기 배치에서는 한 장도 배치하지 않고 턴 종료할 수 있는지 검증
        public void PeriodicDeployment_CanEndWithoutPlacement()
        {
            var turnManager = CreatePeriodicDeploymentTurn(); // 주기 배치 상태 생성

            Assert.That(turnManager.TryEndDeploymentTurn(), Is.True); // 바로 턴 종료 가능
            Assert.That(turnManager.CurrentState, Is.EqualTo(TurnState.PlayerTurn)); // 다음 일반 턴
            Assert.That(turnManager.TurnNumber, Is.EqualTo(6)); // 6턴 시작
        }

        private static TurnManager CreateStartedBattle() // 초기 킹 배치 후 1턴 PlayerTurn 상태를 만드는 보조 메서드
        {
            var turnManager = new TurnManager(); // 시작 배치 상태
            turnManager.MarkInitialKingPlaced(); // 킹 배치 완료 등록
            Assert.That(turnManager.TryEndDeploymentTurn(), Is.True); // 사용자가 배치 턴 종료
            return turnManager; // 1턴 PlayerTurn 반환
        }

        private static TurnManager CreatePeriodicDeploymentTurn() // 첫 주기 배치 턴 상태를 만드는 보조 메서드
        {
            var turnManager = CreateStartedBattle(); // 1턴 PlayerTurn부터 시작
            AdvanceToPlayerTurn(turnManager, 5); // 5턴까지 진행
            Assert.That(turnManager.TryCompletePlayerAction(), Is.True); // 5턴 플레이어 행동 완료
            Assert.That(turnManager.CompleteEnemyTurn(), Is.True); // 5턴 적 행동 완료 후 배치 턴
            return turnManager; // 주기 배치 상태 반환
        }

        private static void AdvanceToPlayerTurn(TurnManager turnManager, int targetTurn) // 지정한 일반 턴까지 진행하는 보조 메서드
        {
            while (turnManager.TurnNumber < targetTurn) // 목표 턴까지 반복
            {
                Assert.That(turnManager.TryCompletePlayerAction(), Is.True); // 플레이어 행동 완료
                Assert.That(turnManager.CompleteEnemyTurn(), Is.True); // 적 행동 완료
                Assert.That(turnManager.CurrentState, Is.EqualTo(TurnState.PlayerTurn)); // 다음 플레이어 턴 확인
            }
        }
    }
}
