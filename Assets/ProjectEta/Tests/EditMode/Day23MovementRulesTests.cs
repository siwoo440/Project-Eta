using NUnit.Framework; // [Test]와 Assert를 사용하기 위한 네임스페이스
using UnityEditor; // 실제 Wazir PieceDefinition 에셋을 로드하기 위한 네임스페이스
using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState와 MovementResolver를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition과 이동 규칙 타입을 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class Day23MovementRulesTests // 23일차 데이터 기반 이동 규칙과 기존 API 호환을 검증하는 테스트 모음
    {
        private const string WazirAssetPath = "Assets/ProjectEta/Data/Wazir.asset"; // 실제 Wazir 데이터 에셋 경로

        [Test] // Wazir가 Custom enum 추가 없이 PieceDefinition 데이터만으로 직교 1칸 이동하는지 검증
        public void Wazir_UsesPieceDefinitionRuleData_ForOrthogonalOneStepMovement()
        {
            var wazir = AssetDatabase.LoadAssetAtPath<PieceDefinition>(WazirAssetPath); // 실제 Wazir 에셋 로드
            Assert.IsNotNull(wazir, "Wazir.asset이 존재해야 합니다."); // 에셋 누락을 즉시 식별
            Assert.AreEqual(PieceMovementType.Custom, wazir.MovementType, "Wazir는 전용 enum 추가 없이 Custom 호환 타입을 사용해야 합니다."); // 새 enum 의존 금지
            Assert.That(wazir.MovementRules.Length, Is.GreaterThan(0), "Wazir는 데이터 기반 이동 규칙을 가져야 합니다."); // 데이터 규칙 존재 확인

            var board = new BoardState(); // 비어 있는 10×10 보드 생성
            var origin = new Vector2Int(4, 4); // 중앙 부근 기준 좌표
            var result = MovementResolver.GetReachableTiles(wazir, origin, isPlayerPiece: true, board); // PieceDefinition 오버로드로 이동 후보 계산

            Assert.Contains(new Vector2Int(5, 4), result.MoveTiles); // 오른쪽 1칸 허용
            Assert.Contains(new Vector2Int(3, 4), result.MoveTiles); // 왼쪽 1칸 허용
            Assert.Contains(new Vector2Int(4, 5), result.MoveTiles); // 위쪽 1칸 허용
            Assert.Contains(new Vector2Int(4, 3), result.MoveTiles); // 아래쪽 1칸 허용
            Assert.AreEqual(4, result.MoveTiles.Count); // 빈 보드 중앙에서는 정확히 직교 4칸만 이동 가능
            Assert.IsFalse(result.MoveTiles.Contains(new Vector2Int(5, 5))); // 대각선은 Wazir 이동에 포함되지 않음
            Assert.IsFalse(result.MoveTiles.Contains(new Vector2Int(6, 4))); // 직교 2칸도 포함되지 않음
        }

        [Test] // 데이터 기반 Step 규칙이 아군을 통과하지 않고 적 칸을 공격 후보로 처리하는지 검증
        public void Wazir_DataRule_RespectsOccupancyAndAttackClassification()
        {
            var wazir = AssetDatabase.LoadAssetAtPath<PieceDefinition>(WazirAssetPath); // 실제 Wazir 에셋 로드
            Assert.IsNotNull(wazir, "Wazir.asset이 존재해야 합니다."); // 에셋 누락 방지

            var board = new BoardState(); // 테스트 보드 생성
            var origin = new Vector2Int(4, 4); // Wazir 기준 좌표
            PlacePiece(board, new Vector2Int(5, 4), isPlayerPiece: true); // 오른쪽 1칸에 아군 배치
            PlacePiece(board, new Vector2Int(4, 5), isPlayerPiece: false); // 위쪽 1칸에 적 배치

            var result = MovementResolver.GetReachableTiles(wazir, origin, isPlayerPiece: true, board); // 데이터 기반 Wazir 이동 계산

            Assert.IsFalse(result.MoveTiles.Contains(new Vector2Int(5, 4))); // 아군 칸에는 이동할 수 없어야 함
            Assert.IsFalse(result.AttackTiles.Contains(new Vector2Int(5, 4))); // 아군 칸은 공격 대상도 아니어야 함
            Assert.Contains(new Vector2Int(4, 5), result.AttackTiles); // 적 칸은 공격 후보여야 함
        }

        [Test] // 기존 PieceMovementType 기반 호출이 23일차 리팩터링 뒤에도 동일하게 작동하는지 검증
        public void LegacyMovementTypeApi_RemainsCompatibleForExistingPieces()
        {
            var board = new BoardState(); // 비어 있는 보드 생성
            var origin = new Vector2Int(4, 4); // 중앙 기준 좌표

            var rook = MovementResolver.GetReachableTiles(PieceMovementType.Rook, origin, true, board); // 기존 enum API로 룩 계산
            var knight = MovementResolver.GetReachableTiles(PieceMovementType.Knight, origin, true, board); // 기존 enum API로 나이트 계산
            var archbishop = MovementResolver.GetReachableTiles(PieceMovementType.Archbishop, origin, true, board); // 기존 enum API로 아크비숍 계산

            Assert.Contains(new Vector2Int(9, 4), rook.MoveTiles); // 룩이 오른쪽 보드 끝까지 이동 가능해야 함
            Assert.Contains(new Vector2Int(6, 5), knight.MoveTiles); // 나이트 L자 도약이 유지돼야 함
            Assert.Contains(new Vector2Int(5, 5), archbishop.MoveTiles); // 아크비숍의 비숍 성분이 유지돼야 함
            Assert.Contains(new Vector2Int(6, 5), archbishop.MoveTiles); // 아크비숍의 나이트 성분도 유지돼야 함
        }

        [Test] // 현재 프로젝트에서 확정된 폰 전진·공격 규칙이 Conditional 모듈로 옮겨져도 보존되는지 검증
        public void LegacyPawnRule_PreservesProjectEtaTwoStepAndFrontAttackBehavior()
        {
            var board = new BoardState(); // 테스트 보드 생성
            var origin = new Vector2Int(4, 1); // 아군 폰 기준 좌표
            PlacePiece(board, new Vector2Int(5, 2), isPlayerPiece: false); // 전방 대각선에 적 배치

            var result = MovementResolver.GetReachableTiles(PieceMovementType.Pawn, origin, true, board); // 기존 enum API로 폰 계산

            Assert.Contains(new Vector2Int(4, 2), result.MoveTiles); // 전방 1칸 이동 유지
            Assert.Contains(new Vector2Int(4, 3), result.MoveTiles); // 매 턴 최대 2칸 이동 규칙 유지
            Assert.Contains(new Vector2Int(5, 2), result.AttackTiles); // 전방 대각선 공격 유지
        }

        private static PieceRuntimeState PlacePiece(BoardState board, Vector2Int position, bool isPlayerPiece) // 테스트용 점유 기물을 배치하는 도우미
        {
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 임시 PieceDefinition 생성
            var piece = new PieceRuntimeState(definition, position, isPlayerPiece); // 런타임 기물 상태 생성
            board.GetTile(position).OccupyingPiece = piece; // 대상 타일에 기물 배치
            return piece; // 생성한 기물 반환
        }
    }
}
