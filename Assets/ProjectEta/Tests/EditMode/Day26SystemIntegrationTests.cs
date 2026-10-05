using System.Collections.Generic; // HashSet 사용
using NUnit.Framework; // 테스트 도구 사용
using UnityEditor; // 실제 에셋 로드
using UnityEngine; // Vector2Int·Mathf 사용
using ProjectEta.Battle; // 전투 이동 정책 사용
using ProjectEta.Fusion; // 합성 DB 사용
using ProjectEta.Pieces; // 기물 데이터 사용
using ProjectEta.Run; // RunState 저장·복원 사용

namespace ProjectEta.Tests.EditMode
{
    public class Day26SystemIntegrationTests
    {
        private const string PieceDatabasePath = "Assets/ProjectEta/Data/PieceDatabase.asset";
        private const string FusionDatabasePath = "Assets/ProjectEta/Data/FusionRecipeDatabase.asset";

        [Test]
        public void AllRegisteredPieces_RoundTripThroughRunSaveData()
        {
            PieceDatabase database = LoadPieceDatabase();
            var run = new RunState(3);

            Assert.AreEqual(55, database.Definitions.Count); // 87일차 현재 등록 55종

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

            RunSaveData saveData = run.ToSaveData();
            RunState restored = RunState.FromSaveData(saveData, database);

            Assert.AreEqual(55, saveData.boardPieces.Count);

            for (int i = 0; i < database.Definitions.Count; i++)
            {
                PieceDefinition definition = database.Definitions[i];
                var position = new Vector2Int(i % ProjectEta.Board.BoardState.Width, i / ProjectEta.Board.BoardState.Width);
                PieceRuntimeState restoredPiece = restored.Board.GetTile(position).OccupyingPiece;

                Assert.IsNotNull(restoredPiece, $"{definition.PieceId}: 복원 기물이 필요합니다.");
                Assert.AreEqual(definition.PieceId, restoredPiece.Definition.PieceId);
                Assert.AreEqual(i % 2 == 0, restoredPiece.IsPlayerPiece);
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

            RunState restored = RunState.FromSaveData(run.ToSaveData(), database);

            Assert.AreEqual(0, restored.Deck.OwnedCardPool.Count);
            Assert.AreEqual(1, restored.Deck.DeadCardPile.Count);
            Assert.AreEqual(1, restored.CountOwnedCopies(knight));
        }

        [Test]
        public void CombatMovementPolicy_DistinguishesRangedFromMeleePieces()
        {
            PieceDatabase database = LoadPieceDatabase();

            Assert.IsFalse(CombatMovementPolicy.ShouldOccupyDefenderTileAfterKill(database.FindById("cannon")));
            Assert.IsFalse(CombatMovementPolicy.ShouldOccupyDefenderTileAfterKill(database.FindById("sniper")));
            Assert.IsTrue(CombatMovementPolicy.ShouldOccupyDefenderTileAfterKill(database.FindById("pawn")));
        }

        [Test]
        public void FusionRecipes_ReferenceRegisteredPieceDefinitions()
        {
            PieceDatabase pieceDatabase = LoadPieceDatabase();
            FusionRecipeDatabase fusionDatabase = AssetDatabase.LoadAssetAtPath<FusionRecipeDatabase>(FusionDatabasePath);
            Assert.IsNotNull(fusionDatabase);

            var uniqueRecipeIds = new HashSet<string>();

            foreach (FusionRecipe recipe in fusionDatabase.Recipes)
            {
                Assert.IsNotNull(recipe);
                Assert.IsNotNull(recipe.MaterialA);
                Assert.IsNotNull(recipe.MaterialB);
                Assert.IsNotNull(recipe.Result);
                Assert.IsTrue(uniqueRecipeIds.Add(recipe.RecipeId), $"중복 RecipeId: {recipe.RecipeId}");

                Assert.AreSame(recipe.MaterialA, pieceDatabase.FindById(recipe.MaterialA.PieceId));
                Assert.AreSame(recipe.MaterialB, pieceDatabase.FindById(recipe.MaterialB.PieceId));
                Assert.AreSame(recipe.Result, pieceDatabase.FindById(recipe.Result.PieceId));
            }
        }

        private static PieceDatabase LoadPieceDatabase()
        {
            PieceDatabase database = AssetDatabase.LoadAssetAtPath<PieceDatabase>(PieceDatabasePath);
            Assert.IsNotNull(database);
            return database;
        }
    }
}
