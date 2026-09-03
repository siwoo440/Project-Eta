using NUnit.Framework; // [Test], Assert 등을 사용하기 위한 네임스페이스
using UnityEditor; // AssetDatabase를 사용하기 위한 네임스페이스
using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition, PieceDatabase, PieceRuntimeState를 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 테스트 코드를 모아두는 네임스페이스
{
    public class RunStateSaveTests // RunState 저장/복원 왕복을 검증하는 테스트 클래스
    {
        private const string KingAssetPath = "Assets/ProjectEta/Data/King.asset"; // 테스트에 쓸 킹 데이터 에셋 경로
        private const string DatabaseAssetPath = "Assets/ProjectEta/Data/PieceDatabase.asset"; // 테스트에 쓸 데이터베이스 에셋 경로

        [Test] // NUnit이 테스트로 인식하게 하는 속성
        public void SaveData_RoundTrip_Preserves_King_And_Board_Piece() // 저장 후 복원해도 데이터가 같은지 확인하는 테스트
        {
            var king = AssetDatabase.LoadAssetAtPath<PieceDefinition>(KingAssetPath); // 킹 에셋 로드
            var database = AssetDatabase.LoadAssetAtPath<PieceDatabase>(DatabaseAssetPath); // 데이터베이스 에셋 로드
            Assert.IsNotNull(king, "King.asset를 찾을 수 없습니다."); // 킹 에셋이 정상 로드됐는지 검증
            Assert.IsNotNull(database, "PieceDatabase.asset를 찾을 수 없습니다."); // 데이터베이스 에셋이 정상 로드됐는지 검증

            var original = new RunState(startingKingHp: 2) // 테스트용 원본 런 상태 생성
            {
                CurrentRound = 5, // 테스트용 라운드 값 지정
                MetaCurrency = 7 // 테스트용 메타 재화 값 지정
            };
            var boardPosition = new Vector2Int(3, 1); // 테스트용 기물 배치 좌표
            original.Board.GetTile(boardPosition).OccupyingPiece = // 해당 칸에 기물 배치
                new PieceRuntimeState(king, boardPosition, isPlayerPiece: true) { CurrentHp = 2 }; // 테스트용 킹 런타임 상태 생성

            var saveData = original.ToSaveData(); // 원본 상태를 저장용 데이터로 변환
            var restored = RunState.FromSaveData(saveData, database); // 저장용 데이터로 상태 복원

            Assert.AreEqual(original.KingHp, restored.KingHp); // 킹 체력이 동일한지 검증
            Assert.AreEqual(original.CurrentRound, restored.CurrentRound); // 라운드가 동일한지 검증
            Assert.AreEqual(original.MetaCurrency, restored.MetaCurrency); // 메타 재화가 동일한지 검증

            var restoredPiece = restored.Board.GetTile(boardPosition).OccupyingPiece; // 복원된 보드에서 기물 조회
            Assert.IsNotNull(restoredPiece); // 기물이 복원됐는지 검증
            Assert.AreEqual(king.PieceId, restoredPiece.Definition.PieceId); // 기물 종류가 동일한지 검증
            Assert.AreEqual(2, restoredPiece.CurrentHp); // 기물 체력이 동일한지 검증
        }
    }
}
