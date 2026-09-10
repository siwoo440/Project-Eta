using NUnit.Framework; // EditMode 테스트 사용
using UnityEngine; // Vector2Int·ScriptableObject 사용
using ProjectEta.Battle; // BattleController·BattleOutcome 사용
using ProjectEta.Board; // BoardState 사용
using ProjectEta.Pieces; // PieceDefinition·PieceRuntimeState 사용
using ProjectEta.Run; // RunState 사용

namespace ProjectEta.Tests.EditMode
{
    public sealed class Day67BattleResultStateNormalizerTests
    {
        [Test]
        public void ApplyOutcomeState_DefeatSetsKingHpToZero()
        {
            var runState = new RunState(3); // 체력이 남아 있는 테스트 런 생성

            BattleController.ApplyOutcomeState(runState, BattleOutcome.Defeat); // BattleController 소유 패배 정리 실행

            Assert.That(runState.KingHp, Is.EqualTo(0)); // 실제 패배 런과 동일한 킹 체력 확인
        }

        [Test]
        public void ApplyOutcomeState_VictoryKeepsKingHp()
        {
            var runState = new RunState(3); // 정상 체력 테스트 런 생성

            BattleController.ApplyOutcomeState(runState, BattleOutcome.Victory); // 승리 상태 정리 실행

            Assert.That(runState.KingHp, Is.EqualTo(3)); // 승리 시 킹 체력 보존 확인
        }

        [Test]
        public void ApplyOutcomeState_NoneDoesNotChangeKingHp()
        {
            var runState = new RunState(2); // 기본 테스트 런 생성

            BattleController.ApplyOutcomeState(runState, BattleOutcome.None); // 미확정 결과 정리 요청

            Assert.That(runState.KingHp, Is.EqualTo(2)); // 미확정 결과 체력 변경 없음 확인
        }

        [Test]
        public void ClearEnemyOccupancy_ClearsMultiTileEnemyAndKeepsPlayer()
        {
            var board = new BoardState(); // 10x10 테스트 보드 생성
            PieceDefinition definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 최소 런타임 기물 정의 생성
            var enemy = new PieceRuntimeState(definition, new Vector2Int(2, 2), false); // 2x2 적 기물 생성
            var player = new PieceRuntimeState(definition, new Vector2Int(0, 0), true); // 보존할 아군 기물 생성

            Assert.That(board.TryOccupyArea(new Vector2Int(2, 2), new Vector2Int(2, 2), enemy), Is.True); // 적 2x2 점유 구성
            Assert.That(board.TryOccupyArea(new Vector2Int(0, 0), Vector2Int.one, player), Is.True); // 아군 1x1 점유 구성

            int cleared = BattleController.ClearEnemyOccupancy(board); // 전투 종료 적 점유 정리 실행

            Assert.That(cleared, Is.EqualTo(1)); // 2x2 네 칸을 적 한 기로 계산 확인
            Assert.That(board.GetTile(new Vector2Int(2, 2)).OccupyingPiece, Is.Null); // 적 기준 칸 해제 확인
            Assert.That(board.GetTile(new Vector2Int(3, 2)).OccupyingPiece, Is.Null); // 적 확장 X 칸 해제 확인
            Assert.That(board.GetTile(new Vector2Int(2, 3)).OccupyingPiece, Is.Null); // 적 확장 Y 칸 해제 확인
            Assert.That(board.GetTile(new Vector2Int(3, 3)).OccupyingPiece, Is.Null); // 적 확장 대각 칸 해제 확인
            Assert.That(board.GetTile(new Vector2Int(0, 0)).OccupyingPiece, Is.SameAs(player)); // 아군 점유 보존 확인

            Object.DestroyImmediate(definition); // 테스트 ScriptableObject 정리
        }
    }
}
