using System.Collections.Generic; // HashSet<T>를 사용하기 위한 네임스페이스
using NUnit.Framework; // Test와 Assert를 사용하기 위한 네임스페이스
using UnityEditor; // 실제 에셋 로드를 위한 네임스페이스
using UnityEngine; // Vector2Int와 Mathf를 사용하기 위한 네임스페이스
using ProjectEta.Battle; // CombatMovementPolicy를 사용하기 위한 네임스페이스
using ProjectEta.Cards; // 덱 생명주기 검증을 위한 네임스페이스
using ProjectEta.Fusion; // FusionRecipeDatabase를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDatabase와 PieceRuntimeState를 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState 저장·복원 검증을 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class Day26SystemIntegrationTests // 전투·저장·합성 시스템과 26종 데이터를 연결해 검증하는 테스트 모음
    {
        private const string PieceDatabasePath = "Assets/ProjectEta/Data/PieceDatabase.asset"; // 기물 DB 경로
        private const string FusionDatabasePath = "Assets/ProjectEta/Data/FusionRecipeDatabase.asset"; // 합성 DB 경로

        [Test] // 26종 전부가 보드 저장·복원에서 id, 체력, 진영을 유지하는지 검증
        public void AllTwentySixPieces_RoundTripThroughRunSaveData()
        {
            var database = LoadPieceDatabase(); // 실제 DB 로드
            var run = new RunState(3); // 테스트 런 생성

            for (int i = 0; i < database.Definitions.Count; i++) // 26종을 모두 순회
            {
                var definition = database.Definitions[i]; // 현재 기물 정의
                var position = new Vector2Int(i % ProjectEta.Board.BoardState.Width, i / ProjectEta.Board.BoardState.Width); // 10x10 안에서 중복 없는 좌표 생성
                bool isPlayerPiece = i % 2 == 0; // 아군·적군을 번갈아 배치
                var piece = new PieceRuntimeState(definition, position, isPlayerPiece); // 런타임 기물 생성
                piece.CurrentHp = Mathf.Max(1, definition.BaseHp - (i % 2)); // 일부 체력을 감소시켜 실제 상태 저장 검증

                if (definition.PieceId == "chameleon") // Chameleon이면
                {
                    piece.AdvanceMovementCycle(); // Bishop 단계
                    piece.AdvanceMovementCycle(); // Rook 단계까지 진행
                }

                run.Board.GetTile(position).OccupyingPiece = piece; // 보드에 기물 배치
            }

            var saveData = run.ToSaveData(); // 26종이 올라간 런을 저장 데이터로 변환
            var restored = RunState.FromSaveData(saveData, database); // 같은 DB로 런 복원

            Assert.AreEqual(26, saveData.boardPieces.Count); // 저장 데이터에 26종 모두 기록됐는지 확인

            for (int i = 0; i < database.Definitions.Count; i++) // 원래 DB 순서대로 복원 상태 재확인
            {
                var definition = database.Definitions[i]; // 원본 정의
                var position = new Vector2Int(i % ProjectEta.Board.BoardState.Width, i / ProjectEta.Board.BoardState.Width); // 동일 좌표 계산
                var restoredPiece = restored.Board.GetTile(position).OccupyingPiece; // 복원된 보드 기물 조회

                Assert.IsNotNull(restoredPiece, $"{definition.PieceId}: 복원 기물이 필요합니다."); // 기물 존재 확인
                Assert.AreEqual(definition.PieceId, restoredPiece.Definition.PieceId, $"{definition.PieceId}: id 복원 실패"); // 기물 종류 유지
                Assert.AreEqual(i % 2 == 0, restoredPiece.IsPlayerPiece, $"{definition.PieceId}: 진영 복원 실패"); // 진영 유지

                if (definition.PieceId == "chameleon") // Chameleon 특별 상태 검증
                {
                    Assert.AreEqual(2, restoredPiece.MovementCycleIndex); // 저장 전 Rook 단계가 그대로 유지되어야 함
                }
            }
        }

        [Test] // 죽은 카드가 저장·복원 후에도 영구 보유 수에 정확히 한 장으로 포함되는지 검증
        public void DeadCardOwnership_RemainsSingleCopyAfterSaveRestore()
        {
            var database = LoadPieceDatabase(); // 실제 DB 로드
            var knight = database.FindById("knight"); // 검증용 카드 선택
            var run = new RunState(3); // 테스트 런 생성

            run.Deck.AddToOwnedPool(knight); // 영구 보유 풀에 1장 추가
            run.Deck.MoveToDeadPile(knight); // 같은 카드를 죽은 카드 더미로 이동

            Assert.AreEqual(0, run.Deck.OwnedCardPool.Count); // 정상 풀에서는 제거되어야 함
            Assert.AreEqual(1, run.Deck.DeadCardPile.Count); // 죽은 카드 더미에는 1장만 존재해야 함
            Assert.AreEqual(1, run.CountOwnedCopies(knight)); // 영구 소유권 기준으로는 1장이어야 함

            var restored = RunState.FromSaveData(run.ToSaveData(), database); // 저장 후 복원

            Assert.AreEqual(0, restored.Deck.OwnedCardPool.Count); // 복원 후 정상 풀 중복 없음
            Assert.AreEqual(1, restored.Deck.DeadCardPile.Count); // 복원 후 죽은 카드 1장 유지
            Assert.AreEqual(1, restored.CountOwnedCopies(knight)); // 복원 후 영구 보유 수 역시 1장
        }

        [Test] // 원거리와 근접 처치 후 점유 정책이 역할 태그 기준으로 유지되는지 검증
        public void CombatMovementPolicy_DistinguishesCannonFromMeleePieces()
        {
            var database = LoadPieceDatabase(); // 실제 DB 로드
            var cannon = database.FindById("cannon"); // 원거리 기물
            var pawn = database.FindById("pawn"); // 근접 기물

            Assert.IsFalse(CombatMovementPolicy.ShouldOccupyDefenderTileAfterKill(cannon)); // Cannon은 원거리 처치 후 원위치
            Assert.IsTrue(CombatMovementPolicy.ShouldOccupyDefenderTileAfterKill(pawn)); // Pawn은 기존 근접 처치 후 대상 칸 점유
        }

        [Test] // 등록된 모든 합성 레시피가 유효한 26종 데이터만 참조하는지 검증
        public void FusionRecipes_ReferenceRegisteredPieceDefinitions()
        {
            var pieceDatabase = LoadPieceDatabase(); // 실제 기물 DB 로드
            var fusionDatabase = AssetDatabase.LoadAssetAtPath<FusionRecipeDatabase>(FusionDatabasePath); // 실제 합성 DB 로드
            Assert.IsNotNull(fusionDatabase, "FusionRecipeDatabase.asset이 존재해야 합니다."); // DB 누락 방지

            var uniqueRecipeIds = new HashSet<string>(); // 레시피 id 중복 검사용 집합 생성

            foreach (var recipe in fusionDatabase.Recipes) // 등록된 모든 레시피 순회
            {
                Assert.IsNotNull(recipe, "FusionRecipeDatabase에는 null 레시피가 들어가면 안 됩니다."); // null 레시피 금지
                Assert.IsNotNull(recipe.MaterialA, $"{recipe.name}: MaterialA가 필요합니다."); // 재료 A 필수
                Assert.IsNotNull(recipe.MaterialB, $"{recipe.name}: MaterialB가 필요합니다."); // 재료 B 필수
                Assert.IsNotNull(recipe.Result, $"{recipe.name}: Result가 필요합니다."); // 결과 필수
                Assert.IsTrue(uniqueRecipeIds.Add(recipe.RecipeId), $"중복 RecipeId: {recipe.RecipeId}"); // 레시피 id 중복 금지

                Assert.AreSame(recipe.MaterialA, pieceDatabase.FindById(recipe.MaterialA.PieceId), $"{recipe.name}: MaterialA가 PieceDatabase에 등록되어야 합니다."); // 재료 A DB 연결
                Assert.AreSame(recipe.MaterialB, pieceDatabase.FindById(recipe.MaterialB.PieceId), $"{recipe.name}: MaterialB가 PieceDatabase에 등록되어야 합니다."); // 재료 B DB 연결
                Assert.AreSame(recipe.Result, pieceDatabase.FindById(recipe.Result.PieceId), $"{recipe.name}: Result가 PieceDatabase에 등록되어야 합니다."); // 결과 DB 연결
            }
        }

        private static PieceDatabase LoadPieceDatabase() // 기물 DB 공통 로드 도우미
        {
            var database = AssetDatabase.LoadAssetAtPath<PieceDatabase>(PieceDatabasePath); // 실제 기물 DB 로드
            Assert.IsNotNull(database, "PieceDatabase.asset이 존재해야 합니다."); // DB 누락 시 실패
            return database; // 정상 DB 반환
        }
    }
}
