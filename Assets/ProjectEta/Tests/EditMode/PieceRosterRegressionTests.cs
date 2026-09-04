using System.Linq; // IReadOnlyList<PieceDefinition>.Contains 확장 메서드를 사용하기 위한 네임스페이스
using System.Collections.Generic; // List<T>, Dictionary<TKey, TValue>를 사용하기 위한 네임스페이스
using NUnit.Framework; // EditMode 테스트 어트리뷰트와 Assert를 사용하기 위한 네임스페이스
using UnityEditor; // 실제 프로젝트 에셋을 경로로 불러오기 위한 네임스페이스(EditMode 전용 테스트 어셈블리)
using UnityEngine; // Vector2Int, Object를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState, MovementResolver를 사용하기 위한 네임스페이스
using ProjectEta.Fusion; // FusionRecipe, FusionRecipeDatabase, FusionRuleValidator를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition, PieceGrade, PieceCategory, PieceRuntimeState를 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class PieceRosterRegressionTests // 22일차: 기본 6종 + 합성 3종(총 9종) 실제 에셋이 데이터·규칙·이동·카드 순환에서 일관되게 동작하는지 확인하는 통합 회귀 테스트
    {
        private const string DataFolderPath = "Assets/ProjectEta/Data"; // 실제 기물·레시피 에셋이 모여 있는 폴더 경로

        private static readonly string[] BasicPieceAssetNames = { "King", "Pawn", "Knight", "Bishop", "Rook", "Queen" }; // 기본 6종 에셋 이름
        private static readonly string[] FusionPieceAssetNames = { "Archbishop", "Chancellor", "Amazon" }; // 합성 3종 에셋 이름

        [Test] // 기본 6종 에셋이 모두 존재하고 식별자·등급·스탯이 정상 범위인지 검증
        public void BasicPieces_HaveValidIdentityAndStats()
        {
            foreach (var assetName in BasicPieceAssetNames) // 기본 6종을 순회하며
            {
                var definition = LoadPiece(assetName); // 실제 에셋 로드

                Assert.IsFalse(string.IsNullOrEmpty(definition.PieceId), $"{assetName}: PieceId가 비어 있습니다."); // 저장·복원에 쓰이는 식별자는 반드시 존재해야 함
                Assert.AreEqual(PieceGrade.OneStar, definition.Grade, $"{assetName}: 기본 기물은 1성이어야 합니다."); // 기본 6종은 모두 1성 기준선
                Assert.Greater(definition.BaseHp, 0, $"{assetName}: 기본 체력은 1 이상이어야 합니다."); // 체력이 0이면 배치 즉시 사망하므로 금지
                Assert.Greater(definition.BaseAtk, 0, $"{assetName}: 기본 공격력은 1 이상이어야 합니다."); // 공격력 0은 전투가 성립하지 않으므로 금지
                Assert.AreEqual(Vector2Int.one, definition.OccupancySize, $"{assetName}: 기본 기물은 1×1 점유여야 합니다."); // 2×2 점유는 보스 전용
            }
        }

        [Test] // King만 합성 재료에서 제외되고 나머지 기본 5종은 재료로 사용 가능한지 검증
        public void King_IsExcludedFromFusionMaterials()
        {
            var king = LoadPiece("King"); // 실제 King 에셋 로드

            Assert.AreEqual(PieceCategory.Special, king.Category, "King은 합성 대상에서 제외되도록 Special 분류여야 합니다."); // 22일차 기본 6종 데이터 정리 결과
            Assert.IsFalse(FusionRuleValidator.IsFusableMaterial(king), "King은 합성 재료로 사용할 수 없어야 합니다."); // 규칙 검증기에서도 제외돼야 함

            foreach (var assetName in BasicPieceAssetNames) // 기본 6종을 순회하며
            {
                if (assetName == "King") continue; // King은 위에서 이미 확인했으므로 건너뜀

                var definition = LoadPiece(assetName); // 실제 에셋 로드
                Assert.AreEqual(PieceCategory.Basic, definition.Category, $"{assetName}: King을 제외한 기본 기물은 Basic 분류여야 합니다."); // 나머지는 합성 재료로 사용 가능
                Assert.IsTrue(FusionRuleValidator.IsFusableMaterial(definition), $"{assetName}: 합성 재료로 사용할 수 있어야 합니다."); // 규칙 검증기에서도 허용돼야 함
            }
        }

        [Test] // 합성 3종이 Fusion 분류이고 시작 카드가 아닌 합성 전용 기물인지 검증
        public void FusionPieces_AreFusionCategoryWithHigherGrade()
        {
            foreach (var assetName in FusionPieceAssetNames) // 합성 3종을 순회하며
            {
                var definition = LoadPiece(assetName); // 실제 에셋 로드

                Assert.AreEqual(PieceCategory.Fusion, definition.Category, $"{assetName}: 합성 결과는 Fusion 분류여야 합니다."); // 획득 경로가 합성으로 고정
                Assert.AreEqual(PieceGrade.TwoStar, definition.Grade, $"{assetName}: 현재 합성 3종은 모두 2성이어야 합니다."); // 22일차 Amazon 강등 이후의 기준선
                Assert.Greater(definition.BaseHp, 0, $"{assetName}: 기본 체력은 1 이상이어야 합니다."); // 체력 검증
                Assert.Greater(definition.BaseAtk, 0, $"{assetName}: 기본 공격력은 1 이상이어야 합니다."); // 공격력 검증
            }
        }

        [Test] // 실제 등록된 모든 레시피가 22일차 등급·재료 규칙을 만족하는지 검증
        public void AllRegisteredRecipes_SatisfyFusionRules()
        {
            var recipes = LoadAllRecipes(); // 프로젝트에 존재하는 모든 레시피 에셋 로드
            Assert.IsNotEmpty(recipes, "등록된 합성 레시피 에셋이 하나도 없습니다."); // 최소 1개는 존재해야 함

            foreach (var recipe in recipes) // 모든 레시피를 순회하며
            {
                var reason = FusionRuleValidator.ValidateRecipe(recipe); // 재료 분류·등급 상승 규칙 검증
                Assert.AreEqual(FusionBlockReason.None, reason, $"{recipe.name}: {FusionRuleValidator.DescribeBlockReason(reason)}"); // 위반 시 어떤 레시피가 왜 막혔는지 그대로 보고
            }
        }

        [Test] // 레시피 데이터베이스가 3종 합성 결과를 실제로 만들어낼 수 있는지 검증
        public void RecipeDatabase_ProducesAllThreeFusionPieces()
        {
            var database = LoadAsset<FusionRecipeDatabase>("FusionRecipeDatabase"); // 실제 레시피 데이터베이스 로드
            var producedResults = new HashSet<string>(); // 레시피로 만들 수 있는 결과 기물 이름 집합

            foreach (var recipe in LoadAllRecipes()) // 모든 레시피를 순회하며
            {
                Assert.IsTrue(database.TryFindRecipe(recipe.MaterialA, recipe.MaterialB, out _), $"{recipe.name}: 데이터베이스에 등록되지 않았습니다."); // 모든 레시피가 조회 가능해야 함
                if (recipe.Result != null) producedResults.Add(recipe.Result.name); // 결과 기물 이름 누적
            }

            foreach (var assetName in FusionPieceAssetNames) // 합성 3종을 순회하며
            {
                Assert.IsTrue(producedResults.Contains(assetName), $"{assetName}: 이 기물을 만들 수 있는 레시피가 없습니다."); // 3종 모두 합성 경로가 있어야 함
            }
        }

        [Test] // 9종 전부가 자기 이동 규칙으로 실제 이동 후보를 계산할 수 있는지 검증
        public void AllNinePieces_ProduceMovementCandidatesOnEmptyBoard()
        {
            var board = new BoardState(); // 비어 있는 10×10 보드 생성
            var origin = new Vector2Int(4, 4); // 보드 중앙 부근에서 이동 후보를 계산

            foreach (var definition in LoadAllNinePieces()) // 기본 6종 + 합성 3종을 순회하며
            {
                var result = MovementResolver.GetReachableTiles(definition.MovementType, origin, isPlayerPiece: true, board); // 이동 후보 계산

                Assert.IsNotNull(result.MoveTiles, $"{definition.name}: 이동 후보 목록이 null입니다."); // 결과 자체가 비정상이면 실패
                Assert.IsNotEmpty(result.MoveTiles, $"{definition.name}({definition.MovementType}): 빈 보드 중앙에서 이동할 수 있는 칸이 없습니다."); // 빈 보드 중앙이면 최소 1칸은 이동 가능해야 함

                foreach (var tile in result.MoveTiles) // 계산된 이동 후보를 순회하며
                {
                    Assert.IsTrue(board.IsInsideBoard(tile), $"{definition.name}: 보드 밖 좌표 {tile}가 이동 후보에 포함됐습니다."); // 보드 경계를 벗어나면 안 됨
                    Assert.AreNotEqual(origin, tile, $"{definition.name}: 제자리 칸이 이동 후보에 포함됐습니다."); // 자기 자신 칸은 이동 후보가 아님
                }
            }
        }

        [Test] // 합성형 3종의 이동 후보가 재료 기물들의 이동 후보를 모두 포함하는지 검증
        public void FusionPieces_CoverBothMaterialMovementSets()
        {
            var board = new BoardState(); // 비어 있는 10×10 보드 생성
            var origin = new Vector2Int(4, 4); // 이동 후보를 계산할 기준 칸

            var compositions = new Dictionary<string, PieceMovementType[]> // 합성형 기물과 그 구성 이동 규칙 대응표
            {
                { "Archbishop", new[] { PieceMovementType.Bishop, PieceMovementType.Knight } }, // Archbishop = Bishop + Knight
                { "Chancellor", new[] { PieceMovementType.Rook, PieceMovementType.Knight } }, // Chancellor = Rook + Knight
                { "Amazon", new[] { PieceMovementType.Queen, PieceMovementType.Knight } } // Amazon = Queen + Knight
            };

            foreach (var pair in compositions) // 합성형 3종을 순회하며
            {
                var definition = LoadPiece(pair.Key); // 실제 합성 기물 에셋 로드
                var fusionTiles = new HashSet<Vector2Int>(MovementResolver.GetReachableTiles(definition.MovementType, origin, true, board).MoveTiles); // 합성 기물의 이동 후보 집합

                foreach (var materialMovement in pair.Value) // 구성 이동 규칙을 순회하며
                {
                    foreach (var tile in MovementResolver.GetReachableTiles(materialMovement, origin, true, board).MoveTiles) // 재료 이동 후보를 순회하며
                    {
                        Assert.IsTrue(fusionTiles.Contains(tile), $"{pair.Key}: {materialMovement}의 이동 후보 {tile}가 합성 이동 집합에 없습니다."); // 재료 이동은 모두 포함돼야 함
                    }
                }
            }
        }

        [Test] // 9종 전부가 배치 → 전투 사망 → 죽은 카드 → 라운드 종료 복귀까지 카드 수 유실 없이 순환하는지 검증
        public void AllNinePieces_SurviveFullCardLifecycle()
        {
            foreach (var definition in LoadAllNinePieces()) // 기본 6종 + 합성 3종을 순회하며
            {
                var runState = new RunState(3); // 기물마다 독립된 런 상태 생성
                runState.Deck.AddToOwnedPool(definition); // 보유 카드 풀에 카드 1장 등록
                runState.Deck.RebuildDrawPileFromOwnedPool(new System.Random(0)); // 고정 시드로 드로우 더미 구성

                Assert.IsTrue(runState.Deck.TryDrawToHand(runState.Hand), $"{definition.name}: 덱에서 손패로 드로우하지 못했습니다."); // 드로우 성공
                Assert.AreEqual(1, runState.Hand.Hand.Count, $"{definition.name}: 손패에 카드가 들어오지 않았습니다."); // 손패에 1장

                var cell = new Vector2Int(3, 2); // 아군 영역의 임의 배치 칸
                var piece = new PieceRuntimeState(definition, cell, isPlayerPiece: true); // 카드 데이터로 기물 생성
                runState.Board.GetTile(cell).OccupyingPiece = piece; // 보드에 배치
                Assert.IsTrue(runState.Hand.RemoveCard(definition), $"{definition.name}: 배치한 카드를 손패에서 제거하지 못했습니다."); // 배치한 카드는 손패에서 빠짐

                Assert.AreEqual(definition.BaseHp, piece.CurrentHp, $"{definition.name}: 배치 시 현재 체력이 기본 체력과 다릅니다."); // 배치 직후 체력은 기본값
                Assert.AreEqual(1, runState.CountDeployedCopies(definition), $"{definition.name}: 보드 배치 수가 정확하지 않습니다."); // 배치 수 집계 확인

                piece.CurrentHp = 0; // 전투로 사망한 상황을 재현
                Assert.IsTrue(piece.IsDead, $"{definition.name}: 체력 0에서 사망 판정이 되지 않았습니다."); // 사망 판정 확인
                runState.Board.GetTile(cell).OccupyingPiece = null; // 사망한 기물을 보드에서 제거
                runState.Deck.MoveToDeadPile(definition); // 사망 카드를 죽은 카드 더미로 이동

                Assert.AreEqual(0, runState.CountDeployedCopies(definition), $"{definition.name}: 사망 후에도 보드 배치 수가 남아 있습니다."); // 보드에서 완전히 제거
                Assert.AreEqual(1, runState.Deck.DeadCardPile.Count, $"{definition.name}: 죽은 카드 더미에 카드가 들어가지 않았습니다."); // 죽은 카드 1장

                Assert.AreEqual(1, runState.CountOwnedCopies(definition), $"{definition.name}: 사망은 보유 카드 수를 바꾸지 않아야 합니다."); // 사망은 라운드 내 위치 변화일 뿐 보유 자체는 유지

                runState.Deck.ReturnDeadPileToOwnedPool(); // 라운드 종료로 죽은 카드를 보유 풀로 복귀

                Assert.AreEqual(0, runState.Deck.DeadCardPile.Count, $"{definition.name}: 라운드 종료 후에도 죽은 카드가 남아 있습니다."); // 죽은 카드 더미는 비워짐
                Assert.IsTrue(runState.Deck.OwnedCardPool.Contains(definition), $"{definition.name}: 라운드 종료 후 카드가 보유 풀에 남아 있지 않습니다."); // 카드가 유실되지 않고 다음 라운드에 다시 쓸 수 있어야 함
            }
        }

        [Test] // 9종 전부가 저장·복원을 거쳐도 보드 위 상태를 그대로 유지하는지 검증
        public void AllNinePieces_RoundTripThroughSaveData()
        {
            var pieceDatabase = LoadAsset<PieceDatabase>("PieceDatabase"); // 실제 기물 데이터베이스 로드
            var runState = new RunState(3); // 저장 대상 런 상태 생성
            var definitions = LoadAllNinePieces(); // 기본 6종 + 합성 3종 로드

            for (int i = 0; i < definitions.Count; i++) // 9종을 서로 다른 칸에 배치하며
            {
                var cell = new Vector2Int(i, 1); // 겹치지 않는 좌표 계산
                runState.Board.GetTile(cell).OccupyingPiece = new PieceRuntimeState(definitions[i], cell, isPlayerPiece: true); // 보드에 배치
            }

            var restored = RunState.FromSaveData(runState.ToSaveData(), pieceDatabase); // 저장 후 즉시 복원

            for (int i = 0; i < definitions.Count; i++) // 배치했던 순서대로 순회하며
            {
                var cell = new Vector2Int(i, 1); // 원래 좌표 재계산
                var restoredPiece = restored.Board.GetTile(cell).OccupyingPiece; // 복원된 기물 조회

                Assert.IsNotNull(restoredPiece, $"{definitions[i].name}: 저장·복원 후 기물이 사라졌습니다."); // 기물이 유지돼야 함
                Assert.AreEqual(definitions[i].PieceId, restoredPiece.Definition.PieceId, $"{definitions[i].name}: 복원된 기물 종류가 다릅니다."); // 종류가 유지돼야 함
                Assert.AreEqual(definitions[i].BaseHp, restoredPiece.CurrentHp, $"{definitions[i].name}: 복원된 체력이 다릅니다."); // 체력이 유지돼야 함
                Assert.IsTrue(restoredPiece.IsPlayerPiece, $"{definitions[i].name}: 복원된 진영이 다릅니다."); // 진영이 유지돼야 함
            }
        }

        private static List<PieceDefinition> LoadAllNinePieces() // 기본 6종 + 합성 3종 에셋을 한 번에 불러오는 도우미 메서드
        {
            var definitions = new List<PieceDefinition>(); // 결과 목록

            foreach (var assetName in BasicPieceAssetNames) definitions.Add(LoadPiece(assetName)); // 기본 6종 추가
            foreach (var assetName in FusionPieceAssetNames) definitions.Add(LoadPiece(assetName)); // 합성 3종 추가

            return definitions; // 총 9종 목록 반환
        }

        private static List<FusionRecipe> LoadAllRecipes() // Data 폴더에 존재하는 모든 합성 레시피 에셋을 불러오는 도우미 메서드
        {
            var recipes = new List<FusionRecipe>(); // 결과 목록
            var guids = AssetDatabase.FindAssets("t:FusionRecipe", new[] { DataFolderPath }); // 레시피 타입 에셋의 GUID 목록 조회

            foreach (var guid in guids) // 조회된 GUID를 순회하며
            {
                var recipe = AssetDatabase.LoadAssetAtPath<FusionRecipe>(AssetDatabase.GUIDToAssetPath(guid)); // GUID를 경로로 바꿔 에셋 로드
                if (recipe != null) recipes.Add(recipe); // 정상 로드된 레시피만 추가
            }

            return recipes; // 레시피 목록 반환
        }

        private static PieceDefinition LoadPiece(string assetName) // 이름으로 기물 정의 에셋을 불러오는 도우미 메서드
        {
            return LoadAsset<PieceDefinition>(assetName); // 공통 로더에 위임
        }

        private static T LoadAsset<T>(string assetName) where T : Object // Data 폴더에서 이름으로 에셋을 불러오는 공통 도우미 메서드
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>($"{DataFolderPath}/{assetName}.asset"); // 고정 폴더 경로에서 에셋 로드
            Assert.IsNotNull(asset, $"{DataFolderPath}/{assetName}.asset 에셋을 찾을 수 없습니다."); // 에셋이 없으면 어떤 파일이 빠졌는지 명확히 실패
            return asset; // 로드된 에셋 반환
        }
    }
}
