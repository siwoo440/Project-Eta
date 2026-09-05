using System.Reflection; // PieceDefinition private 필드에 테스트 값을 넣기 위한 네임스페이스
using NUnit.Framework; // Test와 Assert를 사용하기 위한 네임스페이스
using UnityEngine; // ScriptableObject와 Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition과 PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class Day38CountPiecesCompatibilityTests // 37일차 대형 기물 카운트와 기존 13~20일차 테스트 호환성을 함께 검증하는 회귀 테스트
    {
        [Test] // 기존 BoardStateTests처럼 BaseHp를 설정하지 않은 임시 PieceDefinition도 점유 기물로 세는지 검증
        public void CountPieces_CountsLegacyZeroHpTestDefinitions()
        {
            var board = new BoardState(); // 빈 보드 생성
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 기존 테스트와 동일하게 HP를 따로 설정하지 않은 임시 정의 생성

            board.GetTile(new Vector2Int(1, 1)).OccupyingPiece = new PieceRuntimeState(definition, new Vector2Int(1, 1), true); // BaseHp 0인 아군 테스트 기물 1
            board.GetTile(new Vector2Int(2, 1)).OccupyingPiece = new PieceRuntimeState(definition, new Vector2Int(2, 1), true); // BaseHp 0인 아군 테스트 기물 2
            board.GetTile(new Vector2Int(7, 8)).OccupyingPiece = new PieceRuntimeState(definition, new Vector2Int(7, 8), false); // BaseHp 0인 적 테스트 기물 1

            Assert.AreEqual(2, board.CountPieces(true)); // 기존 BoardStateTests 기대값 유지
            Assert.AreEqual(1, board.CountPieces(false)); // 기존 적 카운트 기대값 유지
        }

        [Test] // 37일차 목적이었던 실제 HP가 존재하는 사망 대형 기물 제외는 그대로 유지되는지 검증
        public void CountPieces_StillIgnoresConfiguredDeadLargePiece()
        {
            var board = new BoardState(); // 빈 보드 생성
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 실제 데이터 형태의 임시 정의 생성

            SetPrivateField(definition, "_baseHp", 30); // 실제 보스처럼 양수 HP 설정
            SetPrivateField(definition, "_occupancySize", new Vector2Int(2, 2)); // 2x2 점유 설정

            var boss = new PieceRuntimeState(definition, new Vector2Int(4, 7), false); // HP 30인 대형 적 생성
            Assert.IsTrue(board.TryOccupyArea(boss.BoardPosition, new Vector2Int(2, 2), boss)); // 네 칸 점유

            boss.CurrentHp = 0; // 실제 사망 상태 재현

            Assert.AreEqual(0, board.CountPieces(false)); // 점유가 잠깐 남아 있어도 실제 사망 보스는 제외
        }

        [Test] // 살아 있는 2x2 대형 기물은 네 칸이 아니라 한 기물로 세는 37일차 동작을 유지하는지 검증
        public void CountPieces_StillCountsLivingLargePieceOnce()
        {
            var board = new BoardState(); // 빈 보드 생성
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 테스트 보스 정의 생성

            SetPrivateField(definition, "_baseHp", 30); // 생존 상태가 되도록 HP 설정
            SetPrivateField(definition, "_occupancySize", new Vector2Int(2, 2)); // 대형 기물 점유 설정

            var boss = new PieceRuntimeState(definition, new Vector2Int(3, 7), false); // 살아 있는 2x2 적 생성
            Assert.IsTrue(board.TryOccupyArea(boss.BoardPosition, new Vector2Int(2, 2), boss)); // 네 칸 점유

            Assert.AreEqual(1, board.CountPieces(false)); // 네 타일이 아니라 한 런타임 기물로 계산
        }

        private static void SetPrivateField<T>(PieceDefinition target, string fieldName, T value) // private 직렬화 필드 주입 도우미
        {
            var field = typeof(PieceDefinition).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic); // 지정 필드 탐색
            Assert.IsNotNull(field, $"PieceDefinition.{fieldName} 필드를 찾을 수 있어야 합니다."); // 구조 변경 시 명확히 실패
            field.SetValue(target, value); // 테스트 값 적용
        }
    }
}
