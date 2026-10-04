using System.Collections.Generic; // HashSet 사용
using NUnit.Framework; // 테스트 도구 사용
using UnityEditor; // 실제 에셋 로드
using UnityEngine; // Vector2Int와 Mathf 사용
using ProjectEta.Battle; // 전투 이동 정책 사용
using ProjectEta.Fusion; // 합성 DB 사용
using ProjectEta.Pieces; // 기물 데이터 사용
using ProjectEta.Run; // RunState 저장·복원 사용

namespace ProjectEta.Tests.EditMode
{
    public class Day26SystemIntegrationTests
    {
        private const string PieceDatabasePath = "Assets/ProjectEta/Data/PieceDatabase.asset"; // 기물 DB 경로
        private const string FusionDatabasePath = "Assets/ProjectEta/Data/FusionRecipeDatabase.asset"; // 합성 DB 경로

        [Test]
        public void AllRegisteredPieces_RoundTripThroughRunSaveData()
        {
            PieceDatabase database = LoadPieceDatabase(); // 실제 DB 로드
            var run = new RunState(3); // 테스트 런 생성

            Assert.AreEqual(32, database.Definitions.Count); // 86일차 현재 등록 32종 검증

            for (int i = 0; i < database.Definitions.Count; i++)
            {
                PieceDefinition definition = database.Definitions[i];
                var position = new Vector2Int(i % ProjectEta.Board.BoardState.Width, i / ProjectEta.Board.BoardState.Width);
                bool isPlayerPiece = i % 2 == 0;
                var piece = new PieceRuntimeState(definition, position, isPlayerPiece);
                piece.CurrentHp = Mathf.Max(1, definition.BaseHp - (i % 2));

                if (definition.PieceId == "chameleon")
                {
                    piece.AdvanceMovementCycle();
                    piece.AdvanceMovementCycle();
                }

                run.Board.GetTile(position).OccupyingPiece = piece;
            }

            RunSaveData saveData = run.ToSaveData(); // 현재 32종 저장
            RunState restored = RunState.FromSaveData(saveData, database); // 같은 DB로 복원

            Assert.AreEqual(32, saveData.boardPieces.Count); // 저장 데이터 32종 검증

            for (int i = 0; i < database.Definitions.Count; i++)
            {
                PieceDefinition definition = database.Definitions[i];
                var position = new Vector2Int(i % ProjectEta.Board.BoardState.Width, i / ProjectEta.Board.BoardState.Width);
                PieceRuntimeState restoredPiece = restored.Board.GetTile(position).OccupyingPiece;

                Assert.IsNotNull(restoredPiece, $"{definition.PieceId}: 복원 기물이 필요합니다.");
                Assert.AreEqual(definition.PieceId, restoredPiece.Definition.PieceId, $"{definition.PieceId}: id 복원 실패");
                Assert.AreEqual(i % 2 == 0, restoredPiece.IsPlayerPiece, $"{definition.PieceId}: 진영 복원 실패");

                if (definition.PieceId == "chameleon")
                {
                    Assert.AreEqual(2, restoredPiece.MovementCycleIndex);
                }
            }
        }

        [Test]
        public void DeadCardOwnership_RemainsSingleCopyAfterSaveRestore()
        {
            PieceDatabase database = LoadPieceDatabase();
            PieceDefinition knight = database.FindById("knight");
            var run = new RunState(3);

            run.Deck.AddToOwnedPool(knight);
            run.Deck.MoveToDeadPile(knight);

            Assert.AreEqual(0, run.Deck.OwnedCardPool.Count);
            Assert.AreEqual(1, run.Deck.DeadCardPile.Count);
            Assert.AreEqual(1, run.CountOwnedCopies(knight));

            RunState restored = RunState.FromSaveData(run.ToSaveData(), database);

            Assert.AreEqual(0, restored.Deck.OwnedCardPool.Count);
            Assert.AreEqual(1, restored.Deck.DeadCardPile.Count);
            Assert.AreEqual(1, restored.CountOwnedCopies(knight));
        }

        [Test]
        public void CombatMovementPolicy_DistinguishesCannonFromMeleePieces()
        {
            PieceDatabase database = LoadPieceDatabase();
            PieceDefinition cannon = database.FindById("cannon");
            PieceDefinition pawn = database.FindById("pawn");

            Assert.IsFalse(CombatMovementPolicy.ShouldOccupyDefenderTileAfterKill(cannon));
            Assert.IsTrue(CombatMovementPolicy.ShouldOccupyDefenderTileAfterKill(pawn));
        }

        [Test]
        public void FusionRecipes_ReferenceRegisteredPieceDefinitions()
        {
            PieceDatabase pieceDatabase = LoadPieceDatabase();
            FusionRecipeDatabase fusionDatabase = AssetDatabase.LoadAssetAtPath<FusionRecipeDatabase>(FusionDatabasePath);
            Assert.IsNotNull(fusionDatabase, "FusionRecipeDatabase.asset이 존재해야 합니다.");

            var uniqueRecipeIds = new HashSet<string>();

            foreach (FusionRecipe recipe in fusionDatabase.Recipes)
            {
                Assert.IsNotNull(recipe, "FusionRecipeDatabase에는 null 레시피가 들어가면 안 됩니다.");
                Assert.IsNotNull(recipe.MaterialA, $"{recipe.name}: MaterialA가 필요합니다.");
                Assert.IsNotNull(recipe.MaterialB, $"{recipe.name}: MaterialB가 필요합니다.");
                Assert.IsNotNull(recipe.Result, $"{recipe.name}: Result가 필요합니다.");
                Assert.IsTrue(uniqueRecipeIds.Add(recipe.RecipeId), $"중복 RecipeId: {recipe.RecipeId}");

                Assert.AreSame(recipe.MaterialA, pieceDatabase.FindById(recipe.MaterialA.PieceId));
                Assert.AreSame(recipe.MaterialB, pieceDatabase.FindById(recipe.MaterialB.PieceId));
                Assert.AreSame(recipe.Result, pieceDatabase.FindById(recipe.Result.PieceId));
            }
        }

        private static PieceDatabase LoadPieceDatabase()
        {
            PieceDatabase database = AssetDatabase.LoadAssetAtPath<PieceDatabase>(PieceDatabasePath);
            Assert.IsNotNull(database, "PieceDatabase.asset이 존재해야 합니다.");
            return database;
        }
    }
}
