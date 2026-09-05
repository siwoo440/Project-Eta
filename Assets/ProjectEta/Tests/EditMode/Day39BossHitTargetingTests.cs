using System.Collections.Generic; // List<T>를 사용하기 위한 네임스페이스
using NUnit.Framework; // Test와 Assert를 사용하기 위한 네임스페이스
using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState를 사용하기 위한 네임스페이스
using ProjectEta.Boss; // LargePiecePlayerAttackTargetResolver를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class Day39BossHitTargetingTests // 2x2 보스 모델 클릭이 실제 공격 가능한 점유 칸으로 변환되는지 검증하는 회귀 테스트
    {
        [Test] // 보스 네 칸 중 한 칸만 공격 가능해도 같은 보스 런타임으로 올바른 공격 칸을 찾는지 검증
        public void Resolver_FindsAttackableCellInsideSameLargeBossFootprint()
        {
            var board = new BoardState(); // 빈 10x10 보드 생성
            var boss = new PieceRuntimeState(null, new Vector2Int(4, 4), false); // 테스트용 적 보스 런타임 상태 생성

            board.GetTile(new Vector2Int(4, 4)).OccupyingPiece = boss; // 보스 좌하단 점유
            board.GetTile(new Vector2Int(5, 4)).OccupyingPiece = boss; // 보스 우하단 점유
            board.GetTile(new Vector2Int(4, 5)).OccupyingPiece = boss; // 보스 좌상단 점유
            board.GetTile(new Vector2Int(5, 5)).OccupyingPiece = boss; // 보스 우상단 점유

            var attackTiles = new List<Vector2Int> // 현재 선택 기물의 실제 공격 가능 칸 목록 생성
            {
                new Vector2Int(4, 4) // 네 칸 중 좌하단만 공격 가능한 상황 재현
            };

            bool resolved = LargePiecePlayerAttackTargetResolver.TryResolveAttackCell(board, boss, attackTiles, out var attackCell); // 같은 보스가 점유한 공격 가능 칸 탐색

            Assert.IsTrue(resolved); // 같은 보스를 공격할 수 있으므로 성공해야 함
            Assert.AreEqual(new Vector2Int(4, 4), attackCell); // 실제 MovementResult가 허용한 칸을 반환해야 함
        }

        [Test] // 공격 후보 칸이 다른 적을 가리키면 잘못해서 보스 공격으로 변환하지 않는지 검증
        public void Resolver_DoesNotUseAttackCellOccupiedByDifferentPiece()
        {
            var board = new BoardState(); // 빈 보드 생성
            var boss = new PieceRuntimeState(null, new Vector2Int(4, 4), false); // 클릭한 보스 런타임
            var otherEnemy = new PieceRuntimeState(null, new Vector2Int(3, 4), false); // 별도 적 런타임

            board.GetTile(new Vector2Int(4, 4)).OccupyingPiece = boss; // 보스 점유
            board.GetTile(new Vector2Int(3, 4)).OccupyingPiece = otherEnemy; // 다른 적 점유

            var attackTiles = new List<Vector2Int> // 현재 공격 가능한 칸에는 다른 적만 존재
            {
                new Vector2Int(3, 4)
            };

            bool resolved = LargePiecePlayerAttackTargetResolver.TryResolveAttackCell(board, boss, attackTiles, out _); // 클릭한 보스 대상으로 변환 시도

            Assert.IsFalse(resolved); // 다른 적의 공격 칸을 보스 공격으로 사용하면 안 됨
        }

        [Test] // 공격 후보가 없는 경우 안전하게 실패하는지 검증
        public void Resolver_ReturnsFalseWhenLargeBossIsNotCurrentlyAttackable()
        {
            var board = new BoardState(); // 빈 보드 생성
            var boss = new PieceRuntimeState(null, new Vector2Int(4, 4), false); // 테스트 보스 생성
            board.GetTile(new Vector2Int(4, 4)).OccupyingPiece = boss; // 보스 점유 등록

            bool resolved = LargePiecePlayerAttackTargetResolver.TryResolveAttackCell( // 빈 공격 후보로 해결 시도
                board,
                boss,
                new List<Vector2Int>(),
                out _);

            Assert.IsFalse(resolved); // 공격 범위 밖 보스를 클릭했다고 공격이 강제로 실행되면 안 됨
        }
    }
}
