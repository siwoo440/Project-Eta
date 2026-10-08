using System; // 기능 참조
using System.Collections; // 기능 참조
using System.Collections.Generic; // 기능 참조
using System.IO; // 기능 참조
using System.Linq; // 기능 참조
using System.Reflection; // 기능 참조
using NUnit.Framework; // 기능 참조
using UnityEditor; // 기능 참조
using UnityEngine; // 기능 참조
using ProjectEta.Battle; // 기능 참조
using ProjectEta.Board; // 기능 참조
using ProjectEta.Pieces; // 기능 참조
using ProjectEta.Run; // 기능 참조
using ProjectEta.Fusion; // 기능 참조

namespace ProjectEta.Tests.EditMode // 기능 영역
{ // 범위 시작
    public sealed class Day96BalanceTests // 검증 동작 구성
    { // 범위 시작
        private PieceDatabase _database; // 검증 보조 구성
        private readonly List<UnityEngine.Object> _temporary = new List<UnityEngine.Object>(); // 검증 보조 구성
        [SetUp] // 테스트 지정
        public void SetUp() // 동작 검증
        { // 범위 시작
            _database = AssetDatabase.LoadAssetAtPath<PieceDatabase>("Assets/ProjectEta/Data/PieceDatabase.asset"); // 검증 동작 구성
            RunEconomyService.ResetForTests(); // 검증 동작 구성
        } // 범위 종료
        [TearDown] // 테스트 지정
        public void TearDown() // 동작 검증
        { // 범위 시작
            foreach (var item in _temporary) // 대상 순회
            { // 범위 시작
                UnityEngine.Object.DestroyImmediate(item); // 검증 동작 구성
            } // 범위 종료
            _temporary.Clear(); // 검증 동작 구성
            RunEconomyService.ResetForTests(); // 검증 동작 구성
        } // 범위 종료
        [Test] // 테스트 지정
        public void Victory_일반승리Gold15지급후중복완료차단() // 동작 검증
        { // 범위 시작
            var run = new RunState(3); // 검증 동작 구성
            Assert.That(RunStageFlowService.CompleteBattle(run, BattleOutcome.Victory), Is.True); // 결과 검증
            Assert.That(RunEconomyService.GetOrCreate(run).Currency, Is.EqualTo(115)); // 결과 검증
            Assert.That(RunStageFlowService.CompleteBattle(run, BattleOutcome.Victory), Is.False); // 결과 검증
            Assert.That(RunEconomyService.GetOrCreate(run).Currency, Is.EqualTo(115)); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Defeat_패배에는Gold보상없음() // 동작 검증
        { // 범위 시작
            var run = new RunState(0); // 검증 동작 구성
            RunStageFlowService.CompleteBattle(run, BattleOutcome.Defeat); // 검증 동작 구성
            Assert.That(RunEconomyService.GetOrCreate(run).Currency, Is.EqualTo(100)); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Victory_저장복원후동일노드재지급차단() // 동작 검증
        { // 범위 시작
            var run = new RunState(3); // 검증 동작 구성
            RunStageFlowService.CompleteBattle(run, BattleOutcome.Victory); // 검증 동작 구성
            var copy = RunState.FromSaveData(JsonUtility.FromJson<RunSaveData>(JsonUtility.ToJson(run.ToSaveData())), _database); // 검증 동작 구성
            copy.Flow.EnterBattle(); // 검증 동작 구성
            RunStageFlowService.CompleteBattle(copy, BattleOutcome.Victory); // 검증 동작 구성
            Assert.That(RunEconomyService.GetOrCreate(copy).Currency, Is.EqualTo(115)); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Victory_다음페이즈진입전4페이즈보스보상55기록() // 동작 검증
        { // 범위 시작
            var run = new RunState(3); // 검증 동작 구성
            run.CurrentRound = 10; // 검증 동작 구성
            run.RouteMap.Configure(10, new StageNode("phase_4_stage_10_midboss", Vector2Int.zero, 10, "stage_10_midboss"), null); // 검증 동작 구성
            RunStageFlowService.CompleteBattle(run, BattleOutcome.Victory); // 검증 동작 구성
            Assert.That(RunEconomyService.GetOrCreate(run).Currency, Is.EqualTo(155)); // 결과 검증
            var entry = Entries(run).Cast<object>().First(x => Field<string>(x, "kind") == "Gold"); // 검증 동작 구성
            Assert.That(Field<int>(entry, "phase"), Is.EqualTo(4)); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Weighted_재료별가중치가실제후보분포에반영() // 동작 검증
        { // 범위 시작
            var config = Profile("{\"_materialWeights\":[{\"pieceId\":\"pawn\",\"weight\":2000}]}"); // 검증 동작 구성
            int pawnCount = 0; // 검증 동작 구성
            for (int seed = 0; seed < 1000; seed++) // 대상 순회
            { // 범위 시작
                if (Balanced(seed, config).Single().PieceId == "pawn") // 조건 확인
                { // 범위 시작
                    pawnCount++; // 검증 동작 구성
                } // 범위 종료
            } // 범위 종료
            Assert.That(pawnCount, Is.GreaterThan(800)); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Weighted_같은Seed와입력은같은후보순서() // 동작 검증
        { // 범위 시작
            var config = Profile("{\"_materialWeights\":[{\"pieceId\":\"rook\",\"weight\":700}]}"); // 검증 동작 구성
            var first = Balanced(17, config, count: 3).Select(x => x.PieceId).ToArray(); // 검증 동작 구성
            var second = Balanced(17, config, count: 3).Select(x => x.PieceId).ToArray(); // 검증 동작 구성
            Assert.That(second, Is.EqualTo(first)); // 결과 검증
            Assert.That(first.Distinct().Count(), Is.EqualTo(3)); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Weighted_큰가중치도정상사망합계보유상한우회불가() // 동작 검증
        { // 범위 시작
            var pawn = _database.FindById("pawn"); // 검증 동작 구성
            var config = Profile("{\"_materialWeights\":[{\"pieceId\":\"pawn\",\"weight\":2000}]}"); // 검증 동작 구성
            var result = Balanced(1, config, new[] { pawn, pawn }, new[] { pawn }, 3); // 검증 동작 구성
            Assert.That(result.Any(x => x.PieceId == "pawn"), Is.False); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Weighted_왕합성결과미해금기물후보제외() // 동작 검증
        { // 범위 시작
            var config = Profile("{}"); // 검증 동작 구성
            var pool = new[] { _database.FindById("king"), _database.FindById("grand_sage"), _database.FindById("shield_guard"), _database.FindById("rook") }; // 검증 동작 구성
            var result = Balanced(1, config, count: 4, pool: pool); // 검증 동작 구성
            Assert.That(result.Select(x => x.PieceId), Is.EqualTo(new[] { "rook" })); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Weighted_음수가중치는재료를삭제하지않고기본값적용() // 동작 검증
        { // 범위 시작
            var config = Profile("{\"_materialWeights\":[{\"pieceId\":\"pawn\",\"weight\":-1}]}"); // 검증 동작 구성
            int count = 0; // 검증 동작 구성
            for (int seed = 0; seed < 100; seed++) // 대상 순회
            { // 범위 시작
                if (Balanced(seed, config).Single().PieceId == "pawn") // 조건 확인
                { // 범위 시작
                    count++; // 검증 동작 구성
                } // 범위 종료
            } // 범위 종료
            Assert.That(count, Is.GreaterThan(10)); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Price_프로필변경을진행도가산가격에반영() // 동작 검증
        { // 범위 시작
            var config = Profile("{\"oneStarPrice\":17,\"purchasePhaseStep\":2,\"purchaseStageStep\":3}"); // 검증 동작 구성
            var method = typeof(ShopPriceRules).GetMethod("GetCardPurchasePrice", new[] { typeof(PieceGrade), typeof(int), typeof(int), config.GetType() }); // 검증 동작 구성
            Assert.That(method, Is.Not.Null); // 결과 검증
            Assert.That(method.Invoke(null, new object[] { PieceGrade.OneStar, 3, 10, config }), Is.EqualTo(30)); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Shop_성공구매만카드와지출기록() // 동작 검증
        { // 범위 시작
            var run = new RunState(3); // 검증 동작 구성
            var economy = RunEconomyService.GetOrCreate(run); // 검증 동작 구성
            var offer = new ShopOffer(_database.FindById("rook"), 25); // 검증 동작 구성
            Assert.That(ShopService.TryPurchaseCard(run, economy, offer, out _), Is.True); // 결과 검증
            Assert.That(economy.Currency, Is.EqualTo(75)); // 결과 검증
            Assert.That(Entries(run).Cast<object>().Count(x => Field<string>(x, "kind") == "Acquired"), Is.EqualTo(1)); // 결과 검증
            Assert.That(ShopService.TryPurchaseCard(run, economy, offer, out _), Is.False); // 결과 검증
            Assert.That(Entries(run).Cast<object>().Count(x => Field<string>(x, "kind") == "Acquired"), Is.EqualTo(1)); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Shop_Gold부족은획득없이실패지출기록() // 동작 검증
        { // 범위 시작
            var run = new RunState(3); // 검증 동작 구성
            var economy = RunEconomyService.GetOrCreate(run); // 검증 동작 구성
            economy.RestoreCurrency(0); // 검증 동작 구성
            Assert.That(ShopService.TryPurchaseCard(run, economy, new ShopOffer(_database.FindById("rook"), 25), out _), Is.False); // 결과 검증
            Assert.That(run.Deck.OwnedCardPool, Is.Empty); // 결과 검증
            var entry = Entries(run).Cast<object>().Single(x => Field<string>(x, "kind") == "Gold"); // 검증 동작 구성
            Assert.That(Field<bool>(entry, "successful"), Is.False); // 결과 검증
            Assert.That(Field<int>(entry, "afterGold"), Is.EqualTo(0)); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Event_무료카드도실제획득기록() // 동작 검증
        { // 범위 시작
            var run = new RunState(3); // 검증 동작 구성
            var choice = new StageEventChoice("take", "카드", "", StageEventChoiceEffectType.CardReward); // 검증 동작 구성
            Assert.That(StageEventService.TryTakeEventCard(run, RunEconomyService.GetOrCreate(run), _database.FindById("rook"), choice, out _, RunContentUnlockSnapshot.Capture(run.RunId, null)), Is.True); // 결과 검증
            Assert.That(Entries(run).Cast<object>().Count(x => Field<string>(x, "kind") == "Acquired"), Is.EqualTo(1)); // 결과 검증
        } // 범위 종료
        [TestCase("king")] // 테스트 지정
        [TestCase("grand_sage")] // 테스트 지정
        [TestCase("shield_guard")] // 테스트 지정
        public void Event_왕합성결과미해금카드직접지급차단(string id) // 동작 검증
        { // 범위 시작
            var run = new RunState(3); // 검증 동작 구성
            var economy = RunEconomyService.GetOrCreate(run); // 검증 동작 구성
            var choice = new StageEventChoice("buy", "구매", "", StageEventChoiceEffectType.PurchaseCard); // 검증 동작 구성
            Assert.That(StageEventService.TryTakeEventCard(run, economy, _database.FindById(id), choice, out _, RunContentUnlockSnapshot.Capture(run.RunId, null)), Is.False); // 결과 검증
            Assert.That(economy.Currency, Is.EqualTo(100)); // 결과 검증
            Assert.That(run.Deck.OwnedCardPool, Is.Empty); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Fusion_실제손패합성완료를기록() // 동작 검증
        { // 범위 시작
            var run = new RunState(3); // 검증 동작 구성
            var recipes = AssetDatabase.LoadAssetAtPath<FusionRecipeDatabase>("Assets/ProjectEta/Data/FusionRecipeDatabase.asset"); // 검증 동작 구성
            var recipe = recipes.Recipes.First(x => x.Result.Grade == PieceGrade.TwoStar); // 검증 동작 구성
            var root = new GameObject("Day96Fusion"); // 검증 동작 구성
            root.SetActive(false); // 검증 동작 구성
            _temporary.Add(root); // 검증 동작 구성
            var view = root.AddComponent<BoardView>(); // 검증 동작 구성
            var input = root.AddComponent<BoardInputController>(); // 검증 동작 구성
            typeof(BoardInputController).GetField("_fusionRecipeDatabase", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(input, recipes); // 검증 동작 구성
            view.Bind(run.Board); // 검증 동작 구성
            input.Bind(run, view, new TurnManager(), new BattleHooks()); // 검증 동작 구성
            run.Hand.TryAddCard(recipe.MaterialA); // 검증 동작 구성
            run.Hand.TryAddCard(recipe.MaterialB); // 검증 동작 구성
            run.Deck.AddToOwnedPool(recipe.MaterialA); // 검증 동작 구성
            run.Deck.AddToOwnedPool(recipe.MaterialB); // 검증 동작 구성
            Assert.That(input.TryFuseCards(recipe.MaterialA, recipe.MaterialB), Is.True); // 결과 검증
            var entry = Entries(run).Cast<object>().Single(x => Field<string>(x, "kind") == "Fusion"); // 검증 동작 구성
            Assert.That(Field<string>(entry, "recipeId"), Is.EqualTo(recipe.RecipeId)); // 결과 검증
            Assert.That(Field<int>(entry, "grade"), Is.EqualTo(2)); // 결과 검증
            Assert.That(input.TryFuseCards(recipe.MaterialA, recipe.MaterialB), Is.False); // 결과 검증
            Assert.That(Entries(run).Cast<object>().Count(x => Field<string>(x, "kind") == "Fusion"), Is.EqualTo(1)); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Save_기록복원과저장스냅샷분리() // 동작 검증
        { // 범위 시작
            var run = new RunState(3); // 검증 동작 구성
            var economy = RunEconomyService.GetOrCreate(run); // 검증 동작 구성
            ShopService.TryPurchaseCard(run, economy, new ShopOffer(_database.FindById("rook"), 25), out _); // 검증 동작 구성
            var data = run.ToSaveData(); // 검증 동작 구성
            ShopService.TryPurchaseCard(run, economy, new ShopOffer(_database.FindById("knight"), 25), out _); // 검증 동작 구성
            var copy = RunState.FromSaveData(JsonUtility.FromJson<RunSaveData>(JsonUtility.ToJson(data)), _database); // 검증 동작 구성
            Assert.That(RunEconomyService.GetOrCreate(copy).Currency, Is.EqualTo(75)); // 결과 검증
            Assert.That(Entries(copy).Cast<object>().Count(x => Field<string>(x, "kind") == "Acquired"), Is.EqualTo(1)); // 결과 검증
            Assert.That(Entries(copy).Cast<object>().Count(x => Field<string>(x, "kind") == "Gold"), Is.EqualTo(1)); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Save_구버전측정필드없어도정상진행() // 동작 검증
        { // 범위 시작
            var data = JsonUtility.FromJson<RunSaveData>("{\"saveVersion\":2,\"kingHp\":3,\"currentRound\":1,\"runCurrency\":37}"); // 검증 동작 구성
            var copy = RunState.FromSaveData(data, _database); // 검증 동작 구성
            Assert.That(Entries(copy), Is.Empty); // 결과 검증
            Assert.That(RunEconomyService.GetOrCreate(copy).Currency, Is.EqualTo(37)); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Offers_후보재표시와저장복원중복기록차단() // 동작 검증
        { // 범위 시작
            var run = new RunState(3); // 검증 동작 구성
            var cards = new[] { _database.FindById("rook") }; // 검증 동작 구성
            Record("RecordOffers", run, "Shop", 12, cards, 1, 1); // 검증 동작 구성
            Record("RecordOffers", run, "Shop", 12, cards, 1, 1); // 검증 동작 구성
            var copy = RunState.FromSaveData(JsonUtility.FromJson<RunSaveData>(JsonUtility.ToJson(run.ToSaveData())), _database); // 검증 동작 구성
            Record("RecordOffers", copy, "Shop", 12, cards, 1, 1); // 검증 동작 구성
            Assert.That(Entries(copy).Cast<object>().Count(x => Field<string>(x, "kind") == "Offer"), Is.EqualTo(1)); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Report_환불을새수입으로세지않고순지출0계산() // 동작 검증
        { // 범위 시작
            var run = new RunState(3); // 검증 동작 구성
            var economy = RunEconomyService.GetOrCreate(run); // 검증 동작 구성
            var spend = typeof(RunEconomyState).GetMethod("TrySpend", new[] { typeof(int), typeof(string) }); // 검증 동작 구성
            var add = typeof(RunEconomyState).GetMethod("Add", new[] { typeof(int), typeof(string) }); // 검증 동작 구성
            Assert.That(spend, Is.Not.Null); // 결과 검증
            spend.Invoke(economy, new object[] { 30, "ShopPurchase" }); // 검증 동작 구성
            add.Invoke(economy, new object[] { 30, "Refund" }); // 검증 동작 구성
            var type = typeof(RunState).Assembly.GetType("ProjectEta.Run.RunBalanceReport"); // 검증 동작 구성
            Assert.That(type, Is.Not.Null); // 결과 검증
            var summary = type.GetMethod("Calculate").Invoke(null, new object[] { run }); // 검증 동작 구성
            Assert.That(summary.GetType().GetProperty("GoldIncome").GetValue(summary), Is.EqualTo(0)); // 결과 검증
            Assert.That(summary.GetType().GetProperty("GoldSpent").GetValue(summary), Is.EqualTo(0)); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Export_JSON과CSV에현재런기록저장() // 동작 검증
        { // 범위 시작
            var run = new RunState(3); // 검증 동작 구성
            ShopService.TryPurchaseCard(run, RunEconomyService.GetOrCreate(run), new ShopOffer(_database.FindById("rook"), 25), out _); // 검증 동작 구성
            var type = typeof(RunState).Assembly.GetType("ProjectEta.Run.RunBalanceReport"); // 검증 동작 구성
            Assert.That(type, Is.Not.Null); // 결과 검증
            string directory = Path.Combine(Path.GetTempPath(), "ProjectEta96_" + Guid.NewGuid().ToString("N")); // 검증 동작 구성
            try // 검증 동작 구성
            { // 범위 시작
                type.GetMethod("Export").Invoke(null, new object[] { run, directory }); // 검증 동작 구성
                Assert.That(Directory.GetFiles(directory, "*.json").Length, Is.EqualTo(1)); // 결과 검증
                Assert.That(Directory.GetFiles(directory, "*.csv").Length, Is.EqualTo(1)); // 결과 검증
                Assert.That(File.ReadAllText(Directory.GetFiles(directory, "*.csv").Single()), Does.Contain("ShopPurchase")); // 결과 검증
            } // 범위 종료
            finally // 검증 동작 구성
            { // 범위 시작
                if (Directory.Exists(directory)) // 조건 확인
                { // 범위 시작
                    Directory.Delete(directory, true); // 검증 동작 구성
                } // 범위 종료
            } // 범위 종료
        } // 범위 종료
[Test] // 구성 저장
        public void Victory_승리확정전Gold직접지급차단() // 구성 저장
        { // 범위 시작
            var run = new RunState(3); // 구성 저장
            Assert.That(RunBattleGoldRewardService.TryGrant(run), Is.False); // 구성 저장
            Assert.That(RunEconomyService.GetOrCreate(run).Currency, Is.EqualTo(100)); // 구성 저장
        } // 범위 종료
        [Test] // 구성 저장
        public void Offers_페이즈전환후에도완료노드와페이즈보존() // 구성 저장
        { // 범위 시작
            var run = new RunState(3); // 구성 저장
            RunBalanceTelemetry.RecordOffersAt(run, "Reward:MidBossVictory", 12, new[] { _database.FindById("rook") }, 4, 10, "phase_4_stage_10_midboss"); // 구성 저장
            RunBalanceTelemetry.RecordCardAt(run, _database.FindById("rook"), "Reward:MidBossVictory", 4, 10, "phase_4_stage_10_midboss"); // 구성 저장
            Assert.That(run.BalanceData.entries.All(x => x.phase == 4 && x.stage == 10 && x.nodeId == "phase_4_stage_10_midboss"), Is.True); // 구성 저장
        } // 범위 종료
        [Test] // 구성 저장
        public void Telemetry_상한이후첫등급과지급이력보존() // 구성 저장
        { // 범위 시작
            var run = new RunState(3); // 구성 저장
            for (int index = 0; index < RunBalanceTelemetry.MaximumEntries; index++) // 구성 저장
            { // 범위 시작
                RunBalanceTelemetry.RecordGold(run, 100, 99, -1, true, "Test"); // 구성 저장
            } // 범위 종료
            var recipes = AssetDatabase.LoadAssetAtPath<FusionRecipeDatabase>("Assets/ProjectEta/Data/FusionRecipeDatabase.asset"); // 구성 저장
            RunBalanceTelemetry.RecordFusion(run, recipes.Recipes.First(x => x.Result.Grade == PieceGrade.TwoStar), 2); // 구성 저장
            RunStageFlowService.CompleteBattle(run, BattleOutcome.Victory); // 구성 저장
            var copy = RunState.FromSaveData(JsonUtility.FromJson<RunSaveData>(JsonUtility.ToJson(run.ToSaveData())), _database); // 구성 저장
            Assert.That(copy.BalanceData.entries.Count, Is.EqualTo(2048)); // 구성 저장
            Assert.That(copy.BalanceData.droppedEntries, Is.EqualTo(2)); // 구성 저장
            Assert.That(copy.BalanceData.firstFusions.Single().grade, Is.EqualTo(2)); // 구성 저장
            Assert.That(copy.BalanceData.goldRewardClaims, Has.Count.EqualTo(1)); // 구성 저장
        } // 범위 종료
        [Test] // 구성 저장
        public void Report_구매실패와부분환불을순지출에반영() // 구성 저장
        { // 범위 시작
            var run = new RunState(3); // 구성 저장
            var economy = RunEconomyService.GetOrCreate(run); // 구성 저장
            economy.TrySpend(30, "ShopPurchase"); // 구성 저장
            economy.Add(10, "Refund"); // 구성 저장
            economy.TrySpend(999, "ShopPurchase"); // 구성 저장
            var summary = RunBalanceReport.Calculate(run); // 구성 저장
            Assert.That(summary.GoldIncome, Is.Zero); // 구성 저장
            Assert.That(summary.GoldSpent, Is.EqualTo(20)); // 구성 저장
            Assert.That(summary.FailedSpends, Is.EqualTo(1)); // 구성 저장
        } // 범위 종료
        [Test] // 구성 저장
        public void RewardSeed_지도와페이즈변경에따른시드분리() // 구성 저장
        { // 범위 시작
            var first = new RunState(3); // 구성 저장
            var second = new RunState(3); // 구성 저장
            first.RouteMap.Configure(1, new StageNode("phase_1_stage_1_battle", Vector2Int.zero, 1, "stage_1_battle"), null); // 구성 저장
            second.RouteMap.Configure(1, new StageNode("phase_2_stage_1_battle", Vector2Int.zero, 1, "stage_1_battle"), null); // 구성 저장
            var go = new GameObject("Day96RewardTest"); // 구성 저장
            go.SetActive(false); // 구성 저장
            try // 구성 저장
            { // 범위 시작
                var controller = go.AddComponent<CardRewardController>(); // 구성 저장
                var field = typeof(CardRewardController).GetField("_runState", BindingFlags.Instance | BindingFlags.NonPublic); // 구성 저장
                var phase = typeof(CardRewardController).GetField("_rewardPhase", BindingFlags.Instance | BindingFlags.NonPublic); // 구성 저장
                var node = typeof(CardRewardController).GetField("_rewardNode", BindingFlags.Instance | BindingFlags.NonPublic); // 구성 저장
                var method = typeof(CardRewardController).GetMethod("CreateRewardSeed", BindingFlags.Instance | BindingFlags.NonPublic); // 구성 저장
                field.SetValue(controller, first); // 구성 저장
                node.SetValue(controller, first.RouteMap.CurrentNodeId); // 구성 저장
                int seed1 = (int)method.Invoke(controller, new object[] { CardRewardSource.BattleVictory, 1 }); // 구성 저장
                field.SetValue(controller, second); // 구성 저장
                phase.SetValue(controller, 2); // 구성 저장
                node.SetValue(controller, second.RouteMap.CurrentNodeId); // 구성 저장
                int seed2 = (int)method.Invoke(controller, new object[] { CardRewardSource.BattleVictory, 1 }); // 구성 저장
                Assert.That(seed2, Is.Not.EqualTo(seed1)); // 구성 저장
            } // 범위 종료
            finally // 구성 저장
            { // 범위 시작
                UnityEngine.Object.DestroyImmediate(go); // 구성 저장
            } // 범위 종료
        } // 범위 종료
[Test] // 구성 저장
        public void Telemetry_상한이후수입과행동총계저장보존() // 구성 저장
        { // 범위 시작
            var run = new RunState(3); // 구성 저장
            var economy = RunEconomyService.GetOrCreate(run); // 구성 저장
            for (int index = 0; index < RunBalanceTelemetry.MaximumEntries; index++) // 구성 저장
            { // 범위 시작
                economy.TrySpend(999, "ShopPurchase"); // 구성 저장
            } // 범위 종료
            economy.Add(15, "BattleReward:Battle"); // 구성 저장
            RunBalanceTelemetry.RecordCard(run, _database.FindById("rook"), "Reward"); // 구성 저장
            var recipes = AssetDatabase.LoadAssetAtPath<FusionRecipeDatabase>("Assets/ProjectEta/Data/FusionRecipeDatabase.asset"); // 구성 저장
            RunBalanceTelemetry.RecordFusion(run, recipes.Recipes.First(x => x.Result.Grade == PieceGrade.TwoStar), 2); // 구성 저장
            var copy = RunState.FromSaveData(JsonUtility.FromJson<RunSaveData>(JsonUtility.ToJson(run.ToSaveData())), _database); // 구성 저장
            var summary = RunBalanceReport.Calculate(copy); // 구성 저장
            Assert.That(summary.GoldIncome, Is.EqualTo(15)); // 구성 저장
            Assert.That(summary.FailedSpends, Is.EqualTo(2048)); // 구성 저장
            Assert.That(summary.AcquiredCount, Is.EqualTo(1)); // 구성 저장
            Assert.That(summary.FusionCount, Is.EqualTo(1)); // 구성 저장
            Assert.That(copy.BalanceData.droppedEntries, Is.EqualTo(3)); // 구성 저장
        } // 범위 종료
[Test] // 구성 저장
        public void Save_누적총계없는이전측정기록복원후새수입합산() // 구성 저장
        { // 범위 시작
            var data = JsonUtility.FromJson<RunSaveData>("{\"saveVersion\":2,\"kingHp\":3,\"currentRound\":1,\"runCurrency\":115,\"balanceData\":{\"initialGold\":100,\"entries\":[{\"kind\":\"Gold\",\"source\":\"BattleReward:Battle\",\"beforeGold\":100,\"afterGold\":115,\"successful\":true}]}}"); // 구성 저장
            var run = RunState.FromSaveData(data, _database); // 구성 저장
            Assert.That(RunBalanceReport.Calculate(run).GoldIncome, Is.EqualTo(15)); // 구성 저장
            RunEconomyService.GetOrCreate(run).Add(5, "EventGold"); // 구성 저장
            Assert.That(RunBalanceReport.Calculate(run).GoldIncome, Is.EqualTo(20)); // 구성 저장
        } // 범위 종료
[Test] // 구성 저장
        public void Profile_음수가격과오버플로및비전투보상보정() // 구성 저장
        { // 범위 시작
            var profile = (RunBalanceProfile)Profile("{\"oneStarPrice\":2147483647,\"purchasePhaseStep\":2147483647,\"purchaseStageStep\":2147483647}"); // 구성 저장
            Assert.That(ShopPriceRules.GetCardPurchasePrice(PieceGrade.OneStar, 5, 10, profile), Is.EqualTo(100000)); // 구성 저장
            profile.oneStarPrice = -25; // 구성 저장
            profile.purchasePhaseStep = -5; // 구성 저장
            profile.purchaseStageStep = -3; // 구성 저장
            Assert.That(ShopPriceRules.GetCardPurchasePrice(PieceGrade.OneStar, 5, 10, profile), Is.EqualTo(1)); // 구성 저장
            Assert.That(profile.GetBattleGold(StageType.Event, 5), Is.Zero); // 구성 저장
        } // 범위 종료
        private ScriptableObject Profile(string json) // 검증 보조 구성
        { // 범위 시작
            var type = typeof(RunState).Assembly.GetType("ProjectEta.Run.RunBalanceProfile"); // 검증 동작 구성
            Assert.That(type, Is.Not.Null, "재료 가중치 설정 누락"); // 결과 검증
            var asset = ScriptableObject.CreateInstance(type); // 검증 동작 구성
            JsonUtility.FromJsonOverwrite(json, asset); // 검증 동작 구성
            _temporary.Add(asset); // 검증 동작 구성
            return asset; // 결과 반환
        } // 범위 종료
        private IReadOnlyList<PieceDefinition> Balanced(int seed, ScriptableObject config, IReadOnlyList<PieceDefinition> owned = null, IReadOnlyList<PieceDefinition> dead = null, int count = 1, IReadOnlyList<PieceDefinition> pool = null) // 검증 보조 구성
        { // 범위 시작
            var method = typeof(CardRewardGenerator).GetMethod("GenerateBalanced"); // 검증 동작 구성
            Assert.That(method, Is.Not.Null, "재료별 가중치 후보 생성 누락"); // 결과 검증
            pool = pool ?? new[] { _database.FindById("pawn"), _database.FindById("rook"), _database.FindById("knight") }; // 검증 동작 구성
            var reward = new CardRewardProfile(CardRewardQuality.Basic, CardRewardSource.RewardNode, 1, 100, 0, 0); // 검증 동작 구성
            return (IReadOnlyList<PieceDefinition>)method.Invoke(null, new object[] { pool, owned, dead, count, seed, null, reward, config }); // 결과 반환
        } // 범위 종료
        private static IList Entries(RunState run) // 검증 보조 구성
        { // 범위 시작
            var property = typeof(RunState).GetProperty("BalanceData"); // 검증 동작 구성
            Assert.That(property, Is.Not.Null, "런 경제 측정 데이터 누락"); // 결과 검증
            var data = property.GetValue(run); // 검증 동작 구성
            return (IList)data.GetType().GetField("entries").GetValue(data); // 결과 반환
        } // 범위 종료
        private static T Field<T>(object value, string name) // 검증 보조 구성
        { // 범위 시작
            return (T)value.GetType().GetField(name).GetValue(value); // 결과 반환
        } // 범위 종료
        private static void Record(string method, params object[] args) // 검증 보조 구성
        { // 범위 시작
            var type = typeof(RunState).Assembly.GetType("ProjectEta.Run.RunBalanceTelemetry"); // 검증 동작 구성
            Assert.That(type, Is.Not.Null); // 결과 검증
            type.GetMethod(method).Invoke(null, args); // 검증 동작 구성
        } // 범위 종료
    } // 범위 종료
} // 범위 종료
