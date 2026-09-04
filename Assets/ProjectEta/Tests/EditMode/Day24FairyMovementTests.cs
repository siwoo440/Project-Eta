using NUnit.Framework; // [Test]와 Assert를 사용하기 위한 네임스페이스
using UnityEditor; // 실제 PieceDefinition·PieceDatabase 에셋을 로드하기 위한 네임스페이스
using UnityEngine; // Vector2Int와 ScriptableObject를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState, MovementResolver를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition, PieceDatabase, PieceMovementType을 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class Day24FairyMovementTests // 24일차 페어리 기물 대량 확장과 Rider 규칙을 검증하는 테스트 모음
    {
        private const string DataRoot = "Assets/ProjectEta/Data/"; // 실제 기물 데이터 에셋 공통 경로

        [Test] // Ferz가 대각선 1칸 Step만 사용하는지 검증
        public void Ferz_DataRule_MovesExactlyOneDiagonalStep()
        {
            var ferz = LoadPiece("Ferz"); // 실제 Ferz 에셋 로드
            var result = MovementResolver.GetReachableTiles(ferz, new Vector2Int(4, 4), true, new BoardState()); // 중앙에서 이동 후보 계산

            Assert.AreEqual(PieceMovementType.Custom, ferz.MovementType); // 신규 기물은 전용 enum 없이 Custom을 사용해야 함
            Assert.AreEqual(4, result.MoveTiles.Count); // 빈 중앙에서는 대각선 4칸만 이동 가능
            Assert.Contains(new Vector2Int(5, 5), result.MoveTiles); // 우상 대각선 1칸
            Assert.Contains(new Vector2Int(3, 5), result.MoveTiles); // 좌상 대각선 1칸
            Assert.IsFalse(result.MoveTiles.Contains(new Vector2Int(4, 5))); // 직교 1칸은 금지
            Assert.IsFalse(result.MoveTiles.Contains(new Vector2Int(6, 6))); // 대각선 2칸은 금지
        }

        [Test] // Dabbaba의 2칸 직교 Leap가 중간 기물을 무시하는지 검증
        public void Dabbaba_Leap_JumpsOverIntermediatePiece()
        {
            var dabbaba = LoadPiece("Dabbaba"); // 실제 Dabbaba 에셋 로드
            var board = new BoardState(); // 빈 보드 생성
            PlacePiece(board, new Vector2Int(5, 4), true); // 오른쪽 1칸 중간 칸을 아군으로 막음

            var result = MovementResolver.GetReachableTiles(dabbaba, new Vector2Int(4, 4), true, board); // 이동 후보 계산

            Assert.Contains(new Vector2Int(6, 4), result.MoveTiles); // 중간 기물을 뛰어넘어 오른쪽 2칸 착지 가능
            Assert.IsFalse(result.MoveTiles.Contains(new Vector2Int(5, 4))); // 중간 칸 자체는 착지 후보가 아님
        }

        [Test] // Camel과 Zebra가 각자의 장거리 비대칭 Leap 벡터를 사용하는지 검증
        public void CamelAndZebra_UseDifferentLongLeapOffsets()
        {
            var camel = LoadPiece("Camel"); // Camel 에셋 로드
            var zebra = LoadPiece("Zebra"); // Zebra 에셋 로드
            var board = new BoardState(); // 공통 빈 보드 생성
            var origin = new Vector2Int(4, 4); // 중앙 기준점

            var camelResult = MovementResolver.GetReachableTiles(camel, origin, true, board); // Camel 이동 계산
            var zebraResult = MovementResolver.GetReachableTiles(zebra, origin, true, board); // Zebra 이동 계산

            Assert.Contains(new Vector2Int(5, 7), camelResult.MoveTiles); // Camel (1,3) 허용
            Assert.IsFalse(camelResult.MoveTiles.Contains(new Vector2Int(6, 7))); // Camel은 (2,3) 금지
            Assert.Contains(new Vector2Int(6, 7), zebraResult.MoveTiles); // Zebra (2,3) 허용
            Assert.IsFalse(zebraResult.MoveTiles.Contains(new Vector2Int(5, 7))); // Zebra는 (1,3) 금지
        }

        [Test] // Centaur가 Mann 8방향 Step과 Knight Leap를 동시에 사용하는지 검증
        public void Centaur_CombinesMannAndKnightMovement()
        {
            var centaur = LoadPiece("Centaur"); // 실제 Centaur 에셋 로드
            var result = MovementResolver.GetReachableTiles(centaur, new Vector2Int(4, 4), true, new BoardState()); // 복합 이동 계산

            Assert.Contains(new Vector2Int(5, 5), result.MoveTiles); // Mann 성분의 인접 대각 1칸
            Assert.Contains(new Vector2Int(5, 4), result.MoveTiles); // Mann 성분의 인접 직교 1칸
            Assert.Contains(new Vector2Int(6, 5), result.MoveTiles); // Knight 성분의 (2,1) 도약
        }

        [Test] // Waffle/Phoenix가 확정된 Wazir + Alfil 조합인지 검증
        public void Waffle_CombinesWazirAndAlfil()
        {
            var waffle = LoadPiece("Waffle"); // 실제 Waffle 에셋 로드
            var result = MovementResolver.GetReachableTiles(waffle, new Vector2Int(4, 4), true, new BoardState()); // 복합 이동 계산

            Assert.Contains(new Vector2Int(5, 4), result.MoveTiles); // Wazir 성분: 직교 1칸
            Assert.Contains(new Vector2Int(6, 6), result.MoveTiles); // Alfil 성분: 대각 2칸 도약
            Assert.IsFalse(result.MoveTiles.Contains(new Vector2Int(4, 6))); // Dabbaba식 직교 2칸은 포함되면 안 됨
        }

        [Test] // Nightrider가 같은 Knight 벡터를 반복하고 반복 착지점에서 차단되는지 검증
        public void Nightrider_RepeatsKnightVector_AndStopsAtBlockedLanding()
        {
            var nightrider = LoadPiece("Nightrider"); // 실제 Nightrider 에셋 로드
            var board = new BoardState(); // 빈 보드 생성
            var origin = new Vector2Int(1, 1); // 두 번 이상 반복 가능한 위치
            PlacePiece(board, new Vector2Int(3, 5), true); // (1,2) 벡터의 두 번째 반복 착지점을 아군으로 차단

            var result = MovementResolver.GetReachableTiles(nightrider, origin, true, board); // Rider 이동 계산

            Assert.Contains(new Vector2Int(2, 3), result.MoveTiles); // 첫 번째 반복 착지점은 허용
            Assert.IsFalse(result.MoveTiles.Contains(new Vector2Int(3, 5))); // 아군 차단 착지점은 이동 불가
            Assert.IsFalse(result.MoveTiles.Contains(new Vector2Int(4, 7))); // 차단점 뒤 세 번째 반복은 탐색 중지
        }

        [Test] // Camelrider가 Camel의 (1,3) 벡터를 반복하는지 검증
        public void Camelrider_RepeatsCamelVector()
        {
            var camelrider = LoadPiece("Camelrider"); // 실제 Camelrider 에셋 로드
            var result = MovementResolver.GetReachableTiles(camelrider, new Vector2Int(1, 1), true, new BoardState()); // Rider 이동 계산

            Assert.Contains(new Vector2Int(2, 4), result.MoveTiles); // 첫 번째 (1,3) 착지
            Assert.Contains(new Vector2Int(3, 7), result.MoveTiles); // 동일 방향 두 번째 반복 착지
        }

        [Test] // 기존 합성 3종이 실제 PieceDefinition 데이터 규칙으로 이전됐는지 검증
        public void ExistingFusionPieces_HaveSerializedMovementRules()
        {
            Assert.That(LoadPiece("Archbishop").MovementRules.Length, Is.GreaterThanOrEqualTo(2)); // Bishop + Knight
            Assert.That(LoadPiece("Chancellor").MovementRules.Length, Is.GreaterThanOrEqualTo(2)); // Rook + Knight
            Assert.That(LoadPiece("Amazon").MovementRules.Length, Is.GreaterThanOrEqualTo(2)); // Queen + Knight
        }

        [Test] // 24일차 신규 기물 전부가 PieceDatabase에서 id로 조회되는지 검증
        public void PieceDatabase_ContainsAllDay24Pieces()
        {
            var database = AssetDatabase.LoadAssetAtPath<PieceDatabase>(DataRoot + "PieceDatabase.asset"); // 실제 데이터베이스 로드
            Assert.IsNotNull(database); // 데이터베이스 누락 방지

            string[] ids = // 23일차 Wazir를 포함한 24일차 확장 검증 id 목록
            {
                "wazir", "ferz", "mann", "dabbaba", "alfil", "camel", "zebra",
                "centaur", "waffle", "nightrider", "camelrider"
            };

            foreach (var id in ids) // 모든 신규/검증 기물을 순회
            {
                Assert.IsNotNull(database.FindById(id), $"PieceDatabase에 {id}가 등록되어야 합니다."); // id 조회 성공 검증
            }
        }

        private static PieceDefinition LoadPiece(string assetName) // 실제 데이터 에셋을 이름으로 로드하는 공통 도우미
        {
            var definition = AssetDatabase.LoadAssetAtPath<PieceDefinition>(DataRoot + assetName + ".asset"); // 지정 이름의 PieceDefinition 로드
            Assert.IsNotNull(definition, $"{assetName}.asset이 존재해야 합니다."); // 에셋 누락을 테스트 실패로 표시
            return definition; // 정상 정의 반환
        }

        private static PieceRuntimeState PlacePiece(BoardState board, Vector2Int position, bool isPlayerPiece) // Rider 차단 검증용 임시 기물 배치 도우미
        {
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 임시 정의 생성
            var piece = new PieceRuntimeState(definition, position, isPlayerPiece); // 런타임 기물 생성
            board.GetTile(position).OccupyingPiece = piece; // 지정 타일에 점유 등록
            return piece; // 생성한 기물 반환
        }
    }
}
