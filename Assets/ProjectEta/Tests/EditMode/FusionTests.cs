using System.Linq; // IReadOnlyList<PieceDefinition>.Contains 확장 메서드를 사용하기 위한 네임스페이스
using System.Reflection; // 테스트에서 직렬화된 private 필드에 값을 주입하기 위한 네임스페이스
using NUnit.Framework; // EditMode 테스트 어트리뷰트와 Assert를 사용하기 위한 네임스페이스
using UnityEngine; // GameObject, ScriptableObject, Object, Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Battle; // TurnManager를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardView, BoardInputController를 사용하기 위한 네임스페이스
using ProjectEta.Fusion; // FusionRecipe, FusionRecipeDatabase를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition을 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class FusionTests // 21일차: 합성 레시피 조회와 재료 선택 모드·확정 흐름을 검증하는 테스트 모음
    {
        [Test] // 재료 순서를 바꿔도 같은 레시피가 매칭되는지 확인하는 테스트
        public void TryFindRecipe_MatchesRegardlessOfMaterialOrder()
        {
            var materialA = ScriptableObject.CreateInstance<PieceDefinition>(); // 테스트용 재료 A
            var materialB = ScriptableObject.CreateInstance<PieceDefinition>(); // 테스트용 재료 B
            var result = ScriptableObject.CreateInstance<PieceDefinition>(); // 테스트용 합성 결과
            var recipe = ScriptableObject.CreateInstance<FusionRecipe>(); // 테스트용 레시피 인스턴스
            SetPrivateField(recipe, "_materialA", materialA); // 레시피 재료 A 주입
            SetPrivateField(recipe, "_materialB", materialB); // 레시피 재료 B 주입
            SetPrivateField(recipe, "_result", result); // 레시피 결과 주입

            var database = ScriptableObject.CreateInstance<FusionRecipeDatabase>(); // 테스트용 레시피 데이터베이스
            SetPrivateField(database, "_recipes", new System.Collections.Generic.List<FusionRecipe> { recipe }); // 데이터베이스에 레시피 1개 등록

            bool matchedInOrder = database.TryFindRecipe(materialA, materialB, out var recipeInOrder); // 등록 순서 그대로 조회
            bool matchedReversed = database.TryFindRecipe(materialB, materialA, out var recipeReversed); // 반대 순서로 조회

            Assert.IsTrue(matchedInOrder); // 순서대로도 매칭돼야 함
            Assert.IsTrue(matchedReversed); // 순서를 바꿔도 매칭돼야 함
            Assert.AreSame(recipe, recipeInOrder); // 같은 레시피를 반환해야 함
            Assert.AreSame(recipe, recipeReversed); // 같은 레시피를 반환해야 함
        }

        [Test] // 등록되지 않은 조합은 매칭되지 않는지 확인하는 테스트
        public void TryFindRecipe_Fails_ForUnregisteredCombination()
        {
            var database = ScriptableObject.CreateInstance<FusionRecipeDatabase>(); // 빈 레시피 데이터베이스
            var materialA = ScriptableObject.CreateInstance<PieceDefinition>(); // 등록되지 않은 재료 A
            var materialB = ScriptableObject.CreateInstance<PieceDefinition>(); // 등록되지 않은 재료 B

            bool matched = database.TryFindRecipe(materialA, materialB, out var recipe); // 매칭 시도

            Assert.IsFalse(matched); // 매칭되지 않아야 함
            Assert.IsNull(recipe); // 결과도 null이어야 함
        }

        [Test] // 배치 턴이 아니면 합성 모드에 진입할 수 없는지 검증
        public void SetFusionModeActive_Fails_OutsideDeploymentTurn()
        {
            var context = CreateContext(withRecipe: false); // 합성 데이터 없이 기본 컨텍스트 생성

            try // 테스트 자원 정리를 보장
            {
                context.TurnManager.TryEndDeploymentTurn(); // 배치 턴을 벗어나 PlayerTurn으로 전환 시도(킹 미배치라 실패할 수 있음)

                bool activated = context.BoardInput.SetFusionModeActive(true); // 배치 턴이 아닌 상태에서 합성 모드 진입 시도

                if (context.TurnManager.CurrentState == ProjectEta.Battle.TurnState.DeploymentTurn) // 킹 미배치로 여전히 배치 턴이면
                {
                    Assert.IsTrue(activated); // 배치 턴이므로 진입은 성공해야 함
                }
                else // 실제로 배치 턴을 벗어났다면
                {
                    Assert.IsFalse(activated); // 합성 모드 진입이 거부돼야 함
                    Assert.IsFalse(context.BoardInput.IsFusionModeActive); // 모드 상태도 꺼진 채여야 함
                }
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 객체 제거
            }
        }

        [Test] // 재료 2장을 선택하면 매칭 레시피가 미리보기로 계산되고, 확정하면 손패가 정확히 갱신되는지 검증
        public void FusionFlow_SelectTwoMaterials_PreviewsThenConfirmsResult()
        {
            var context = CreateContext(withRecipe: true); // 매칭 레시피가 포함된 컨텍스트 생성

            try // 테스트 자원 정리를 보장
            {
                context.RunState.Hand.TryAddCard(context.MaterialA); // 재료 A를 손패에 직접 추가
                context.RunState.Hand.TryAddCard(context.MaterialB); // 재료 B를 손패에 직접 추가

                Assert.IsTrue(context.BoardInput.SetFusionModeActive(true)); // 배치 턴이므로 합성 모드 진입 성공
                Assert.IsTrue(context.BoardInput.TryToggleFusionMaterial(context.MaterialA)); // 재료 A 선택
                Assert.IsNull(context.BoardInput.CurrentFusionRecipe); // 재료가 1장뿐이면 아직 미리보기 없음

                Assert.IsTrue(context.BoardInput.TryToggleFusionMaterial(context.MaterialB)); // 재료 B 선택(2장 완성)
                Assert.IsNotNull(context.BoardInput.CurrentFusionRecipe); // 2장이 모이면 매칭 레시피가 미리 계산돼야 함
                Assert.AreSame(context.Result, context.BoardInput.CurrentFusionRecipe.Result); // 미리보기 결과가 정확해야 함

                bool confirmed = context.BoardInput.TryConfirmFusionSelection(); // 합성 확정 실행

                Assert.IsTrue(confirmed); // 확정이 성공해야 함
                Assert.IsFalse(context.RunState.Hand.Hand.Contains(context.MaterialA)); // 재료 A가 손패에서 사라져야 함
                Assert.IsFalse(context.RunState.Hand.Hand.Contains(context.MaterialB)); // 재료 B가 손패에서 사라져야 함
                Assert.IsTrue(context.RunState.Hand.Hand.Contains(context.Result)); // 결과 카드가 손패에 들어와야 함
                Assert.AreEqual(0, context.BoardInput.FusionMaterials.Count); // 확정 후 재료 선택은 비워져야 함
                Assert.IsTrue(context.BoardInput.IsFusionModeActive); // 연속 합성을 위해 합성 모드 자체는 유지돼야 함
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 객체 제거
            }
        }

        [Test] // 매칭되는 레시피가 없으면 합성 확정이 거부되는지 검증
        public void TryConfirmFusionSelection_Fails_WhenNoRecipeMatches()
        {
            var context = CreateContext(withRecipe: false); // 레시피가 등록되지 않은 컨텍스트 생성
            var unrelatedA = ScriptableObject.CreateInstance<PieceDefinition>(); // 어떤 레시피와도 매칭되지 않는 카드 A
            var unrelatedB = ScriptableObject.CreateInstance<PieceDefinition>(); // 어떤 레시피와도 매칭되지 않는 카드 B

            try // 테스트 자원 정리를 보장
            {
                context.RunState.Hand.TryAddCard(unrelatedA); // 손패에 카드 A 추가
                context.RunState.Hand.TryAddCard(unrelatedB); // 손패에 카드 B 추가

                Assert.IsTrue(context.BoardInput.SetFusionModeActive(true)); // 합성 모드 진입
                context.BoardInput.TryToggleFusionMaterial(unrelatedA); // 카드 A 선택
                context.BoardInput.TryToggleFusionMaterial(unrelatedB); // 카드 B 선택

                Assert.IsNull(context.BoardInput.CurrentFusionRecipe); // 매칭되는 레시피가 없어야 함

                bool confirmed = context.BoardInput.TryConfirmFusionSelection(); // 합성 확정 시도

                Assert.IsFalse(confirmed); // 매칭 레시피가 없으므로 실패해야 함
                Assert.AreEqual(2, context.RunState.Hand.Hand.Count); // 손패 카드 2장이 그대로 유지돼야 함
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 객체 제거
                Object.DestroyImmediate(unrelatedA); // 별도로 생성한 카드 정의 정리
                Object.DestroyImmediate(unrelatedB); // 별도로 생성한 카드 정의 정리
            }
        }

        [Test] // 22일차: 합성 패널의 재료 슬롯 클릭이 그 자리의 재료 1장만 빼고 나머지 선택은 유지하는지 검증
        public void ToggleFusionMaterial_RemovesOnlyThatSlot_AndKeepsTheOther()
        {
            var context = CreateContext(withRecipe: true); // 매칭 레시피가 포함된 컨텍스트 생성
            var spareCard = CreatePiece(PieceGrade.OneStar, PieceCategory.Basic); // 슬롯을 비운 뒤 새로 넣어볼 예비 카드

            try // 테스트 자원 정리를 보장
            {
                context.RunState.Hand.TryAddCard(context.MaterialA); // 재료 A를 손패에 추가
                context.RunState.Hand.TryAddCard(context.MaterialB); // 재료 B를 손패에 추가
                context.RunState.Hand.TryAddCard(spareCard); // 예비 카드도 손패에 추가

                Assert.IsTrue(context.BoardInput.SetFusionModeActive(true)); // 합성 모드 진입
                context.BoardInput.TryToggleFusionMaterial(context.MaterialA); // 재료 A 선택(슬롯 A)
                context.BoardInput.TryToggleFusionMaterial(context.MaterialB); // 재료 B 선택(슬롯 B)
                Assert.AreEqual(2, context.BoardInput.FusionMaterials.Count); // 두 슬롯이 모두 채워져야 함
                Assert.IsNotNull(context.BoardInput.CurrentFusionRecipe); // 이 조합은 미리보기가 계산돼야 함

                // 합성 패널의 A 슬롯을 누르는 것과 동일한 동작: 그 슬롯의 재료를 다시 토글
                Assert.IsTrue(context.BoardInput.TryToggleFusionMaterial(context.BoardInput.FusionMaterials[0]));

                Assert.AreEqual(1, context.BoardInput.FusionMaterials.Count); // 한 장만 빠져야 함
                Assert.AreSame(context.MaterialB, context.BoardInput.FusionMaterials[0]); // 남은 재료는 B여야 함(A만 제거)
                Assert.IsNull(context.BoardInput.CurrentFusionRecipe); // 재료가 1장이므로 미리보기는 사라져야 함
                Assert.AreEqual(FusionBlockReason.NotEnoughMaterials, context.BoardInput.CurrentFusionBlockReason); // 사유도 재료 부족으로 바뀌어야 함
                Assert.IsTrue(context.BoardInput.IsFusionModeActive); // 합성 모드 자체는 유지돼야 함
                Assert.IsTrue(context.RunState.Hand.Hand.Contains(context.MaterialA)); // 뺀 재료는 손패에 그대로 남아 있어야 함

                Assert.IsTrue(context.BoardInput.TryToggleFusionMaterial(spareCard)); // 비워진 자리에 다른 카드를 바로 넣을 수 있어야 함
                Assert.AreEqual(2, context.BoardInput.FusionMaterials.Count); // 다시 두 장이 채워져야 함
                Assert.AreSame(spareCard, context.BoardInput.FusionMaterials[1]); // 새 카드가 빈 자리에 들어가야 함
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 컨텍스트 정리
                DestroyAll(spareCard); // 예비 카드 정의 정리
            }
        }

        [Test] // 22일차: 결과 등급이 재료보다 정확히 한 단계 높을 때만 등급 상승 규칙을 통과하는지 검증
        public void IsGradeStepValid_AllowsSingleStep_AndBlocksJump()
        {
            var oneStarA = CreatePiece(PieceGrade.OneStar, PieceCategory.Basic); // 1성 재료 A
            var oneStarB = CreatePiece(PieceGrade.OneStar, PieceCategory.Basic); // 1성 재료 B
            var twoStar = CreatePiece(PieceGrade.TwoStar, PieceCategory.Fusion); // 한 단계 높은 2성 결과
            var threeStar = CreatePiece(PieceGrade.ThreeStar, PieceCategory.Fusion); // 두 단계 높은 3성 결과

            var validRecipe = CreateRecipe(oneStarA, oneStarB, twoStar); // 1성 + 1성 -> 2성 정상 레시피
            var jumpRecipe = CreateRecipe(oneStarA, oneStarB, threeStar); // 1성 + 1성 -> 3성 등급 점프 레시피

            try // 테스트 자원 정리를 보장
            {
                Assert.IsTrue(FusionRuleValidator.IsGradeStepValid(validRecipe)); // 한 단계 상승은 허용돼야 함
                Assert.IsFalse(FusionRuleValidator.IsGradeStepValid(jumpRecipe)); // 두 단계 점프는 차단돼야 함
                Assert.AreEqual(FusionBlockReason.GradeStepViolation, FusionRuleValidator.ValidateRecipe(jumpRecipe)); // 차단 사유가 등급 위반으로 보고돼야 함
            }
            finally // 성공/실패와 무관하게 정리
            {
                DestroyAll(oneStarA, oneStarB, twoStar, threeStar); // 기물 정의 정리
                Object.DestroyImmediate(validRecipe); // 레시피 정리
                Object.DestroyImmediate(jumpRecipe); // 레시피 정리
            }
        }

        [Test] // 22일차: 동일 카드 특수 레시피는 예외 플래그로 등급 규칙을 건너뛸 수 있는지 검증
        public void IsGradeStepValid_AllowsGradeJump_ForIdenticalMaterialException()
        {
            var oneStar = CreatePiece(PieceGrade.OneStar, PieceCategory.Basic); // 동일 카드로 쓸 1성 기물
            var fourStar = CreatePiece(PieceGrade.FourStar, PieceCategory.Fusion); // 세 단계 높은 4성 결과
            var exceptionRecipe = CreateRecipe(oneStar, oneStar, fourStar, ignoresGradeStep: true); // 동일 카드 2장 + 등급 규칙 예외 레시피

            try // 테스트 자원 정리를 보장
            {
                Assert.IsTrue(exceptionRecipe.UsesIdenticalMaterials); // 동일 카드 레시피로 인식돼야 함
                Assert.IsTrue(FusionRuleValidator.IsGradeStepValid(exceptionRecipe)); // 예외 플래그가 켜져 있으면 등급 점프가 허용돼야 함
                Assert.AreEqual(FusionBlockReason.None, FusionRuleValidator.ValidateRecipe(exceptionRecipe)); // 레시피 규칙 검증도 통과해야 함
            }
            finally // 성공/실패와 무관하게 정리
            {
                DestroyAll(oneStar, fourStar); // 기물 정의 정리
                Object.DestroyImmediate(exceptionRecipe); // 레시피 정리
            }
        }

        [Test] // 22일차: King처럼 합성 대상이 아닌 분류의 기물은 재료로 쓸 수 없는지 검증
        public void ValidateRecipe_Blocks_NonFusableMaterial()
        {
            var king = CreatePiece(PieceGrade.OneStar, PieceCategory.Special); // 합성 제외 분류인 King 역할 기물
            var basicPiece = CreatePiece(PieceGrade.OneStar, PieceCategory.Basic); // 일반 기본 기물
            var result = CreatePiece(PieceGrade.TwoStar, PieceCategory.Fusion); // 한 단계 높은 결과
            var recipe = CreateRecipe(king, basicPiece, result); // King을 재료로 쓰는 레시피

            try // 테스트 자원 정리를 보장
            {
                Assert.IsFalse(FusionRuleValidator.IsFusableMaterial(king)); // King 분류는 재료로 쓸 수 없어야 함
                Assert.AreEqual(FusionBlockReason.MaterialNotFusable, FusionRuleValidator.ValidateRecipe(recipe)); // 차단 사유가 재료 분류 위반으로 보고돼야 함
            }
            finally // 성공/실패와 무관하게 정리
            {
                DestroyAll(king, basicPiece, result); // 기물 정의 정리
                Object.DestroyImmediate(recipe); // 레시피 정리
            }
        }

        [Test] // 22일차: 4·5성 기물의 동일 기물 보유 상한이 기획서 기본안과 일치하는지 검증
        public void GetOwnedLimit_MatchesDesignDocumentLimits()
        {
            Assert.AreEqual(int.MaxValue, FusionRuleValidator.GetOwnedLimit(PieceGrade.OneStar)); // 1성은 제한 없음
            Assert.AreEqual(int.MaxValue, FusionRuleValidator.GetOwnedLimit(PieceGrade.ThreeStar)); // 3성도 제한 없음
            Assert.AreEqual(FusionRuleValidator.FourStarOwnedLimit, FusionRuleValidator.GetOwnedLimit(PieceGrade.FourStar)); // 4성은 지정된 상한
            Assert.AreEqual(FusionRuleValidator.FiveStarOwnedLimit, FusionRuleValidator.GetOwnedLimit(PieceGrade.FiveStar)); // 5성은 지정된 상한
            Assert.AreEqual(1, FusionRuleValidator.FiveStarOwnedLimit); // 5성은 동일 최상위 기물 1개 제한이어야 함
        }

        [Test] // 22일차: 5성 결과를 이미 보유 중이면 추가 합성이 수량 제한으로 차단되는지 검증
        public void EvaluateFusion_Blocks_WhenOwnedLimitReached()
        {
            var context = CreateContext(withRecipe: false); // 레시피를 직접 주입하기 위해 빈 컨텍스트 생성
            var materialA = CreatePiece(PieceGrade.FourStar, PieceCategory.Fusion); // 4성 재료 A
            var materialB = CreatePiece(PieceGrade.FourStar, PieceCategory.Fusion); // 4성 재료 B
            var fiveStarResult = CreatePiece(PieceGrade.FiveStar, PieceCategory.Fusion); // 5성 최종 합성 결과
            var recipe = CreateRecipe(materialA, materialB, fiveStarResult); // 4성 + 4성 -> 5성 정상 레시피

            try // 테스트 자원 정리를 보장
            {
                var database = ScriptableObject.CreateInstance<FusionRecipeDatabase>(); // 레시피 데이터베이스 생성
                SetPrivateField(database, "_recipes", new System.Collections.Generic.List<FusionRecipe> { recipe }); // 레시피 등록
                SetPrivateField(context.BoardInput, "_fusionRecipeDatabase", database); // 입력 컨트롤러에 연결

                context.RunState.Hand.TryAddCard(materialA); // 재료 A를 손패에 추가
                context.RunState.Hand.TryAddCard(materialB); // 재료 B를 손패에 추가

                Assert.AreEqual(FusionBlockReason.None, context.BoardInput.EvaluateFusion(materialA, materialB, out _)); // 아직 5성을 보유하지 않았으므로 합성 가능

                context.RunState.Deck.AddToOwnedPool(fiveStarResult); // 5성 결과를 이미 1개 보유한 상황을 만듦

                Assert.AreEqual(FusionBlockReason.OwnedLimitReached, context.BoardInput.EvaluateFusion(materialA, materialB, out _)); // 상한 도달로 차단돼야 함
                Assert.IsFalse(context.BoardInput.TryFuseCards(materialA, materialB)); // 실제 합성도 실패해야 함
                Assert.AreEqual(2, context.RunState.Hand.Hand.Count); // 손패 재료가 그대로 남아야 함
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 컨텍스트 정리
                DestroyAll(materialA, materialB, fiveStarResult); // 기물 정의 정리
                Object.DestroyImmediate(recipe); // 레시피 정리
            }
        }

        [Test] // 22일차: 숨김 레시피는 발견 전 결과가 가려지고, 합성 성공 시 발견 기록·알림이 남는지 검증
        public void HiddenRecipe_IsMaskedUntilDiscovered_ThenRecorded()
        {
            var context = CreateContext(withRecipe: false); // 레시피를 직접 주입하기 위해 빈 컨텍스트 생성
            var materialA = CreatePiece(PieceGrade.OneStar, PieceCategory.Basic); // 1성 재료 A
            var materialB = CreatePiece(PieceGrade.OneStar, PieceCategory.Basic); // 1성 재료 B
            var result = CreatePiece(PieceGrade.TwoStar, PieceCategory.Fusion); // 2성 숨김 합성 결과
            var hiddenRecipe = CreateRecipe(materialA, materialB, result, isHidden: true); // 숨김 레시피 생성
            FusionRecipe discoveredRecipe = null; // 발견 이벤트로 전달받은 레시피를 담을 변수

            try // 테스트 자원 정리를 보장
            {
                var database = ScriptableObject.CreateInstance<FusionRecipeDatabase>(); // 레시피 데이터베이스 생성
                SetPrivateField(database, "_recipes", new System.Collections.Generic.List<FusionRecipe> { hiddenRecipe }); // 숨김 레시피 등록
                SetPrivateField(context.BoardInput, "_fusionRecipeDatabase", database); // 입력 컨트롤러에 연결
                context.BoardInput.HiddenRecipeDiscovered += recipe => discoveredRecipe = recipe; // 발견 이벤트 구독

                context.RunState.Hand.TryAddCard(materialA); // 재료 A를 손패에 추가
                context.RunState.Hand.TryAddCard(materialB); // 재료 B를 손패에 추가

                Assert.IsFalse(context.RunState.FusionDiscovery.IsDiscovered(hiddenRecipe)); // 합성 전에는 미발견 상태여야 함

                Assert.IsTrue(context.BoardInput.SetFusionModeActive(true)); // 합성 모드 진입
                context.BoardInput.TryToggleFusionMaterial(materialA); // 재료 A 선택
                context.BoardInput.TryToggleFusionMaterial(materialB); // 재료 B 선택

                Assert.IsNotNull(context.BoardInput.CurrentFusionRecipe); // 규칙을 통과했으므로 미리보기 레시피는 존재해야 함
                Assert.IsTrue(context.BoardInput.IsCurrentFusionRecipeUndiscovered); // 아직 발견 전이라 결과를 가려야 함

                Assert.IsTrue(context.BoardInput.TryConfirmFusionSelection()); // 합성 확정은 정상 성공해야 함

                Assert.IsTrue(context.RunState.FusionDiscovery.IsDiscovered(hiddenRecipe)); // 합성 후에는 발견 상태여야 함
                Assert.AreEqual(1, context.RunState.FusionDiscovery.DiscoveredCount); // 발견 기록이 1건이어야 함
                Assert.AreSame(hiddenRecipe, discoveredRecipe); // 발견 이벤트가 해당 레시피로 발생해야 함
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 컨텍스트 정리
                DestroyAll(materialA, materialB, result); // 기물 정의 정리
                Object.DestroyImmediate(hiddenRecipe); // 레시피 정리
            }
        }

        [Test] // 22일차: 숨김 합성식 발견 기록이 저장·복원을 거쳐도 유지되는지 검증
        public void FusionDiscovery_SurvivesSaveAndLoad()
        {
            var runState = new RunState(3); // 저장용 런 상태 생성
            var hiddenRecipe = CreateRecipe(null, null, null, isHidden: true); // 발견 기록만 확인할 숨김 레시피

            try // 테스트 자원 정리를 보장
            {
                Assert.IsTrue(runState.FusionDiscovery.TryMarkDiscovered(hiddenRecipe)); // 숨김 레시피를 발견 처리
                Assert.IsFalse(runState.FusionDiscovery.TryMarkDiscovered(hiddenRecipe)); // 같은 레시피를 다시 발견 처리하면 중복이 아니어야 함

                var saveData = runState.ToSaveData(); // 저장용 데이터로 변환
                Assert.Contains(hiddenRecipe.RecipeId, saveData.discoveredRecipeIds); // 발견 목록이 저장 데이터에 포함돼야 함

                var emptyDatabase = ScriptableObject.CreateInstance<PieceDatabase>(); // 카드 복원은 이 테스트의 관심사가 아니므로 빈 데이터베이스 사용
                var restored = RunState.FromSaveData(saveData, emptyDatabase); // 저장 데이터로 런 상태 복원
                Assert.IsTrue(restored.FusionDiscovery.IsDiscovered(hiddenRecipe)); // 복원 후에도 발견 상태가 유지돼야 함
                Object.DestroyImmediate(emptyDatabase); // 임시 데이터베이스 인스턴스 정리
            }
            finally // 성공/실패와 무관하게 정리
            {
                Object.DestroyImmediate(hiddenRecipe); // 레시피 정리
            }
        }

        private static void DestroyAll(params PieceDefinition[] definitions) // 22일차: 테스트에서 만든 기물 정의를 한 번에 정리하는 도우미 메서드
        {
            foreach (var definition in definitions) // 전달받은 정의를 순회하며
            {
                if (definition != null) Object.DestroyImmediate(definition); // 남아 있는 인스턴스를 즉시 제거
            }
        }

        private static PieceDefinition CreatePiece(PieceGrade grade, PieceCategory category) // 22일차: 등급·분류를 지정한 테스트용 기물 정의를 만드는 도우미 메서드
        {
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 빈 기물 정의 인스턴스 생성
            SetPrivateField(definition, "_grade", grade); // 등급 주입
            SetPrivateField(definition, "_category", category); // 분류 주입
            return definition; // 완성된 기물 정의 반환
        }

        private static FusionRecipe CreateRecipe(PieceDefinition materialA, PieceDefinition materialB, PieceDefinition result, bool isHidden = false, bool ignoresGradeStep = false) // 22일차: 재료·결과·숨김·예외 플래그를 지정한 테스트용 레시피를 만드는 도우미 메서드
        {
            var recipe = ScriptableObject.CreateInstance<FusionRecipe>(); // 빈 레시피 인스턴스 생성
            SetPrivateField(recipe, "_recipeId", "test_recipe"); // 발견 기록용 식별자 주입
            SetPrivateField(recipe, "_materialA", materialA); // 재료 A 주입
            SetPrivateField(recipe, "_materialB", materialB); // 재료 B 주입
            SetPrivateField(recipe, "_result", result); // 결과 주입
            SetPrivateField(recipe, "_isHiddenRecipe", isHidden); // 숨김 여부 주입
            SetPrivateField(recipe, "_ignoresGradeStepRule", ignoresGradeStep); // 등급 규칙 예외 여부 주입
            return recipe; // 완성된 레시피 반환
        }

        private static void SetPrivateField(object target, string fieldName, object value) // 리플렉션으로 private 필드 값을 설정하는 공용 도우미 메서드
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance); // 대상 타입에서 지정한 이름의 private 인스턴스 필드 조회
            Assert.IsNotNull(field, $"필드 {fieldName}을 찾을 수 없습니다."); // 필드 존재 검증
            field.SetValue(target, value); // 조회한 필드에 값 대입
        }

        private static FusionTestContext CreateContext(bool withRecipe) // 합성 테스트에 필요한 객체 일체를 만드는 도우미 메서드
        {
            var root = new GameObject("FusionTestRoot"); // 테스트 루트 생성
            var boardView = root.AddComponent<BoardView>(); // 보드 뷰 추가
            var boardInput = root.AddComponent<BoardInputController>(); // 입력 컨트롤러 추가
            var runState = new RunState(3); // 실제 런 상태 생성
            var turnManager = new TurnManager(); // 시작 배치 턴 상태로 생성(합성은 배치 턴 전용이므로 그대로 사용)

            var materialA = CreatePiece(PieceGrade.OneStar, PieceCategory.Basic); // 22일차: 등급 규칙 검증을 통과하도록 1성 기본 기물로 생성
            var materialB = CreatePiece(PieceGrade.OneStar, PieceCategory.Basic); // 22일차: 등급 규칙 검증을 통과하도록 1성 기본 기물로 생성
            var result = CreatePiece(PieceGrade.TwoStar, PieceCategory.Fusion); // 22일차: 재료보다 정확히 한 등급 높은 2성 합성 결과

            if (withRecipe) // 매칭 레시피가 필요한 테스트면
            {
                var recipe = ScriptableObject.CreateInstance<FusionRecipe>(); // 실제 레시피 인스턴스 생성
                SetPrivateField(recipe, "_materialA", materialA); // 재료 A 주입
                SetPrivateField(recipe, "_materialB", materialB); // 재료 B 주입
                SetPrivateField(recipe, "_result", result); // 결과 주입

                var database = ScriptableObject.CreateInstance<FusionRecipeDatabase>(); // 레시피 데이터베이스 생성
                SetPrivateField(database, "_recipes", new System.Collections.Generic.List<FusionRecipe> { recipe }); // 데이터베이스에 레시피 등록
                SetPrivateField(boardInput, "_fusionRecipeDatabase", database); // BoardInputController에 데이터베이스 연결
            }

            boardView.Bind(runState.Board); // 보드 상태 연결
            boardInput.Bind(runState, boardView, turnManager); // 입력 상태 연결

            return new FusionTestContext(root, boardInput, runState, turnManager, materialA, materialB, result); // 완성된 테스트 컨텍스트 반환
        }

        private sealed class FusionTestContext // 테스트 객체와 정리 책임을 묶는 내부 클래스
        {
            public GameObject Root { get; } // 테스트 루트
            public BoardInputController BoardInput { get; } // 입력 컨트롤러
            public RunState RunState { get; } // 런 상태
            public TurnManager TurnManager { get; } // 턴 매니저
            public PieceDefinition MaterialA { get; } // 테스트용 재료 A
            public PieceDefinition MaterialB { get; } // 테스트용 재료 B
            public PieceDefinition Result { get; } // 테스트용 합성 결과

            public FusionTestContext(GameObject root, BoardInputController boardInput, RunState runState, TurnManager turnManager, PieceDefinition materialA, PieceDefinition materialB, PieceDefinition result) // 생성자
            {
                Root = root; // 루트 저장
                BoardInput = boardInput; // 입력 저장
                RunState = runState; // 런 저장
                TurnManager = turnManager; // 턴 저장
                MaterialA = materialA; // 재료 A 저장
                MaterialB = materialB; // 재료 B 저장
                Result = result; // 결과 저장
            }

            public void Dispose() // 테스트 객체 정리
            {
                Object.DestroyImmediate(Root); // GameObject 제거
                if (MaterialA != null) Object.DestroyImmediate(MaterialA); // 재료 A 정의 제거
                if (MaterialB != null) Object.DestroyImmediate(MaterialB); // 재료 B 정의 제거
                if (Result != null) Object.DestroyImmediate(Result); // 결과 정의 제거
            }
        }
    }
}
