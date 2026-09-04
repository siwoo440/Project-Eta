using NUnit.Framework; // [Test]와 Assert를 사용하기 위한 네임스페이스
using UnityEditor; // 실제 PieceDefinition·PieceDatabase 에셋을 로드하기 위한 네임스페이스
using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Board; // 이동 규칙과 보드 상태를 사용하기 위한 네임스페이스
using ProjectEta.Battle; // 원거리 처치 후 점유 정책을 검증하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition과 PieceRuntimeState를 사용하기 위한 네임스페이스
using ProjectEta.Run; // 저장/복원 회귀 검증을 위한 RunState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public class Day25SpecialMovementTests // 25일차 특수 이동과 원거리 공격 규칙 회귀 테스트
    {
        private const string DataRoot = "Assets/ProjectEta/Data/"; // 실제 데이터 에셋 공통 경로

        [Test] // Cannon이 십자 한 칸만 이동하는지 검증
        public void Cannon_MovesOnlyOneOrthogonalStep()
        {
            var cannon = LoadPiece("Cannon"); // 실제 Cannon 데이터 로드
            var result = MovementResolver.GetReachableTiles(cannon, new Vector2Int(4, 4), true, new BoardState()); // 중앙에서 후보 계산

            Assert.AreEqual(4, result.MoveTiles.Count); // 빈 보드에서는 직교 4칸만 이동 후보
            Assert.Contains(new Vector2Int(5, 4), result.MoveTiles); // 오른쪽 1칸 허용
            Assert.Contains(new Vector2Int(4, 5), result.MoveTiles); // 위쪽 1칸 허용
            Assert.IsFalse(result.MoveTiles.Contains(new Vector2Int(6, 4))); // 직교 2칸 이동 금지
            Assert.IsFalse(result.MoveTiles.Contains(new Vector2Int(5, 5))); // 대각선 이동 금지
        }

        [Test] // Cannon이 룩 방향의 첫 적을 원거리 공격 후보로 찾는지 검증
        public void Cannon_AttacksFirstEnemyAlongRookLine()
        {
            var cannon = LoadPiece("Cannon"); // Cannon 데이터 로드
            var board = new BoardState(); // 빈 보드 생성
            PlacePiece(board, LoadPiece("Pawn"), new Vector2Int(4, 8), false); // 같은 열 먼 거리에 적 배치

            var result = MovementResolver.GetReachableTiles(cannon, new Vector2Int(4, 4), true, board); // Cannon 후보 계산

            Assert.Contains(new Vector2Int(4, 8), result.AttackTiles); // 먼 적이 공격 후보여야 함
            Assert.IsFalse(result.MoveTiles.Contains(new Vector2Int(4, 6))); // 공격 사거리 때문에 이동 거리가 늘어나면 안 됨
        }

        [Test] // Cannon의 원거리 공격이 아군을 관통하지 않는지 검증
        public void Cannon_RangedAttack_IsBlockedByFirstFriendlyPiece()
        {
            var cannon = LoadPiece("Cannon"); // Cannon 데이터 로드
            var board = new BoardState(); // 빈 보드 생성
            PlacePiece(board, LoadPiece("Pawn"), new Vector2Int(4, 6), true); // 중간 아군 배치
            PlacePiece(board, LoadPiece("Pawn"), new Vector2Int(4, 8), false); // 그 뒤 적 배치

            var result = MovementResolver.GetReachableTiles(cannon, new Vector2Int(4, 4), true, board); // 공격 후보 계산

            Assert.IsFalse(result.AttackTiles.Contains(new Vector2Int(4, 8))); // 아군 뒤 적은 공격 불가
        }

        [Test] // Cannon이 원거리 역할이라 처치 후 대상 칸을 점유하지 않는 정책인지 검증
        public void Cannon_LethalAttack_DoesNotAdvanceToDefenderTile()
        {
            var cannon = LoadPiece("Cannon"); // Cannon 데이터 로드

            Assert.IsTrue((cannon.RoleTags & PieceRoleTag.Ranged) != 0); // Cannon은 Ranged 역할 태그를 가져야 함
            Assert.IsFalse(CombatMovementPolicy.ShouldOccupyDefenderTileAfterKill(cannon)); // 원거리 처치 후 전진하지 않아야 함
        }

        [Test] // Grasshopper가 첫 발판 바로 뒤 한 칸만 착지하는지 검증
        public void Grasshopper_JumpsOverFirstHurdle_AndLandsImmediatelyBehind()
        {
            var grasshopper = LoadPiece("Grasshopper"); // Grasshopper 데이터 로드
            var board = new BoardState(); // 빈 보드 생성
            PlacePiece(board, LoadPiece("Pawn"), new Vector2Int(4, 6), true); // 같은 열의 첫 발판 생성

            var result = MovementResolver.GetReachableTiles(grasshopper, new Vector2Int(4, 4), true, board); // Hopper 후보 계산

            Assert.Contains(new Vector2Int(4, 7), result.MoveTiles); // 첫 발판 바로 뒤 한 칸 착지
            Assert.IsFalse(result.MoveTiles.Contains(new Vector2Int(4, 5))); // 발판 전 빈 칸에는 이동 불가
            Assert.IsFalse(result.MoveTiles.Contains(new Vector2Int(4, 8))); // 발판 뒤 두 칸에는 이동 불가
        }

        [Test] // Canvasser·Caliph·Squirrel이 기존 규칙 데이터 조합만으로 이동하는지 검증
        public void CompositeSpecialPieces_UseExistingRuleCombinations()
        {
            var board = new BoardState(); // 공통 빈 보드
            var origin = new Vector2Int(4, 4); // 중앙 기준점

            var canvasser = MovementResolver.GetReachableTiles(LoadPiece("Canvasser"), origin, true, board); // Rook + Camel
            Assert.Contains(new Vector2Int(4, 8), canvasser.MoveTiles); // Rook Slide 성분
            Assert.Contains(new Vector2Int(5, 7), canvasser.MoveTiles); // Camel Leap 성분

            var caliph = MovementResolver.GetReachableTiles(LoadPiece("Caliph"), origin, true, board); // Bishop + Camel
            Assert.Contains(new Vector2Int(7, 7), caliph.MoveTiles); // Bishop Slide 성분
            Assert.Contains(new Vector2Int(5, 7), caliph.MoveTiles); // Camel Leap 성분

            var squirrel = MovementResolver.GetReachableTiles(LoadPiece("Squirrel"), origin, true, board); // Dabbaba + Knight + Alfil
            Assert.Contains(new Vector2Int(4, 6), squirrel.MoveTiles); // Dabbaba 성분
            Assert.Contains(new Vector2Int(5, 6), squirrel.MoveTiles); // Knight 성분
            Assert.Contains(new Vector2Int(6, 6), squirrel.MoveTiles); // Alfil 성분
        }

        [Test] // Chameleon이 N→B→R→Q 순환 이동 데이터를 사용하는지 검증
        public void Chameleon_CyclesKnightBishopRookQueen()
        {
            var definition = LoadPiece("Chameleon"); // Chameleon 데이터 로드
            var piece = new PieceRuntimeState(definition, new Vector2Int(4, 4), true); // 런타임 기물 생성
            var board = new BoardState(); // 빈 보드 생성

            var knight = MovementResolver.GetReachableTiles(piece, board); // 초기 단계 Knight
            Assert.Contains(new Vector2Int(5, 6), knight.MoveTiles); // Knight 도약 확인

            piece.AdvanceMovementCycle(); // Bishop 단계로 전환
            var bishop = MovementResolver.GetReachableTiles(piece, board); // Bishop 후보 계산
            Assert.Contains(new Vector2Int(6, 6), bishop.MoveTiles); // 대각선 Slide 확인

            piece.AdvanceMovementCycle(); // Rook 단계로 전환
            var rook = MovementResolver.GetReachableTiles(piece, board); // Rook 후보 계산
            Assert.Contains(new Vector2Int(4, 8), rook.MoveTiles); // 직교 Slide 확인

            piece.AdvanceMovementCycle(); // Queen 단계로 전환
            var queen = MovementResolver.GetReachableTiles(piece, board); // Queen 후보 계산
            Assert.Contains(new Vector2Int(8, 8), queen.MoveTiles); // 대각 장거리 확인

            piece.AdvanceMovementCycle(); // 다시 Knight 단계로 순환
            Assert.AreEqual(0, piece.MovementCycleIndex); // 4단계 뒤 0으로 복귀
        }

        [Test] // Chameleon 이동 단계가 저장/불러오기 후 유지되는지 검증
        public void Chameleon_MovementCycleIndex_IsSavedAndRestored()
        {
            var database = AssetDatabase.LoadAssetAtPath<PieceDatabase>(DataRoot + "PieceDatabase.asset"); // 실제 DB 로드
            var chameleon = database.FindById("chameleon"); // Chameleon 정의 조회
            var run = new RunState(3); // 테스트 런 생성
            var piece = new PieceRuntimeState(chameleon, new Vector2Int(3, 3), true); // Chameleon 배치
            piece.AdvanceMovementCycle(); // Bishop
            piece.AdvanceMovementCycle(); // Rook
            run.Board.GetTile(piece.BoardPosition).OccupyingPiece = piece; // 보드 점유 등록

            var restored = RunState.FromSaveData(run.ToSaveData(), database); // 저장 후 즉시 복원
            var restoredPiece = restored.Board.GetTile(new Vector2Int(3, 3)).OccupyingPiece; // 복원 기물 조회

            Assert.IsNotNull(restoredPiece); // 기물 존재 확인
            Assert.AreEqual(2, restoredPiece.MovementCycleIndex); // Rook 단계 유지 확인
        }

        [Test] // PieceDatabase가 25일차 종료 기준 26종을 모두 보유하는지 검증
        public void PieceDatabase_ContainsTwentySixDefinitions()
        {
            var databaseAsset = AssetDatabase.LoadAssetAtPath<PieceDatabase>(DataRoot + "PieceDatabase.asset"); // DB 로드
            string[] day25Ids = { "grasshopper", "cannon", "canvasser", "caliph", "squirrel", "chameleon" }; // 25일차 신규 id 목록

            foreach (var id in day25Ids) // 신규 6종 순회
            {
                Assert.IsNotNull(databaseAsset.FindById(id), $"PieceDatabase에 {id}가 등록되어야 합니다."); // 조회 성공 검증
            }
        }

        private static PieceDefinition LoadPiece(string assetName) // 실제 데이터 에셋 로드 공통 도우미
        {
            var definition = AssetDatabase.LoadAssetAtPath<PieceDefinition>(DataRoot + assetName + ".asset"); // 이름으로 정의 로드
            Assert.IsNotNull(definition, $"{assetName}.asset이 존재해야 합니다."); // 누락 시 명확한 실패
            return definition; // 정의 반환
        }

        private static PieceRuntimeState PlacePiece(BoardState board, PieceDefinition definition, Vector2Int position, bool isPlayerPiece) // 테스트용 기물 배치
        {
            var piece = new PieceRuntimeState(definition, position, isPlayerPiece); // 런타임 상태 생성
            board.GetTile(position).OccupyingPiece = piece; // 보드 점유 연결
            return piece; // 생성 기물 반환
        }
    }
}
