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

            var materialA = ScriptableObject.CreateInstance<PieceDefinition>(); // 테스트용 재료 A
            var materialB = ScriptableObject.CreateInstance<PieceDefinition>(); // 테스트용 재료 B
            var result = ScriptableObject.CreateInstance<PieceDefinition>(); // 테스트용 합성 결과

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
