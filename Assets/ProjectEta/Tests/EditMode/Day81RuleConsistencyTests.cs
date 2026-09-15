using System; // 합성 완료 처리자 사용
using System.Collections.Generic; // 업적 대기 집합 사용
using System.Reflection; // 테스트 데이터 필드 주입 사용
using NUnit.Framework; // EditMode 테스트 도구 사용
using UnityEngine; // Unity 테스트 객체 사용
using ProjectEta.Battle; // 턴 상태 사용
using ProjectEta.Board; // 보드 입력 사용
using ProjectEta.Cards; // 카드 보유 상태 사용
using ProjectEta.Fusion; // 합성 레시피 사용
using ProjectEta.Pieces; // 기물 데이터 사용
using ProjectEta.Run; // 카드 획득 규칙 사용
using ProjectEta.Steam; // Steam 통합 상태 사용

namespace ProjectEta.Tests.EditMode // EditMode 테스트 네임스페이스
{ // 네임스페이스 범위 시작
    public sealed class Day81RuleConsistencyTests // 81일차 규칙 일관성 테스트
    { // 테스트 클래스 범위 시작
        [TestCase(PieceGrade.OneStar, 3)] // 1성 보유 상한 검증
        [TestCase(PieceGrade.TwoStar, 3)] // 2성 보유 상한 검증
        [TestCase(PieceGrade.ThreeStar, 3)] // 3성 보유 상한 검증
        [TestCase(PieceGrade.FourStar, 2)] // 4성 보유 상한 검증
        [TestCase(PieceGrade.FiveStar, 1)] // 5성 보유 상한 검증
        public void OwnershipRules_ReturnExpectedLimit(PieceGrade grade, int expectedLimit) // 등급별 단일 규칙 검증
        { // 테스트 범위 시작
            Assert.That(CardOwnershipRules.GetOwnedLimit(grade), Is.EqualTo(expectedLimit)); // 설계 상한 일치 검증
        } // 테스트 범위 종료

        [Test] // 등급별 보유 상한 검증
        public void RewardAcquisition_UsesGradeOwnedLimits() // 모든 카드 추가 경로의 등급 상한 검증
        { // 테스트 범위 시작
            var deck = new DeckState(); // 테스트 카드 보유 상태 생성
            PieceDefinition fourStar = CreatePiece("four_star", PieceGrade.FourStar, PieceCategory.Fusion); // 4성 테스트 카드 생성
            PieceDefinition fiveStar = CreatePiece("five_star", PieceGrade.FiveStar, PieceCategory.Fusion); // 5성 테스트 카드 생성

            try // 테스트 자원 정리 보장
            { // 보호 범위 시작
                Assert.That(CardRewardRules.TryAddOwnedCard(deck, fourStar), Is.True); // 첫 4성 추가 허용 검증
                Assert.That(CardRewardRules.TryAddOwnedCard(deck, fourStar), Is.True); // 두 번째 4성 추가 허용 검증
                Assert.That(CardRewardRules.TryAddOwnedCard(deck, fourStar), Is.False); // 세 번째 4성 추가 차단 검증
                Assert.That(CardRewardRules.TryAddOwnedCard(deck, fiveStar), Is.True); // 첫 5성 추가 허용 검증
                Assert.That(CardRewardRules.TryAddOwnedCard(deck, fiveStar), Is.False); // 두 번째 5성 추가 차단 검증
            } // 보호 범위 종료
            finally // 테스트 자원 정리 단계
            { // 정리 범위 시작
                UnityEngine.Object.DestroyImmediate(fourStar); // 4성 테스트 카드 제거
                UnityEngine.Object.DestroyImmediate(fiveStar); // 5성 테스트 카드 제거
            } // 정리 범위 종료
        } // 테스트 범위 종료

        [TestCase(PieceGrade.OneStar)] // 1성 실제 획득 상한 검증
        [TestCase(PieceGrade.TwoStar)] // 2성 실제 획득 상한 검증
        [TestCase(PieceGrade.ThreeStar)] // 3성 실제 획득 상한 검증
        public void RewardAcquisition_OneToThreeStarAllowsThreeCopies(PieceGrade grade) // 저등급 실제 카드 추가 경계 검증
        { // 테스트 범위 시작
            DeckState deck = new DeckState(); // 테스트 카드 보유 상태 생성
            PieceDefinition definition = CreatePiece($"grade_{grade}", grade, PieceCategory.Basic); // 등급별 테스트 카드 생성

            try // 테스트 자원 정리 보장
            { // 보호 범위 시작
                Assert.That(CardRewardRules.TryAddOwnedCard(deck, definition), Is.True); // 첫 카드 획득 허용 검증
                Assert.That(CardRewardRules.TryAddOwnedCard(deck, definition), Is.True); // 둘째 카드 획득 허용 검증
                Assert.That(CardRewardRules.TryAddOwnedCard(deck, definition), Is.True); // 셋째 카드 획득 허용 검증
                Assert.That(CardRewardRules.TryAddOwnedCard(deck, definition), Is.False); // 넷째 카드 획득 차단 검증
            } // 보호 범위 종료
            finally // 테스트 자원 정리 단계
            { // 정리 범위 시작
                UnityEngine.Object.DestroyImmediate(definition); // 등급별 테스트 카드 제거
            } // 정리 범위 종료
        } // 테스트 범위 종료

        [Test] // 사망 카드 포함 획득 상한 검증
        public void RewardAcquisition_BlocksWhenSameCardLimitIsInDeadPile() // 사망한 동일 카드의 소유권 반영 검증
        { // 테스트 범위 시작
            DeckState deck = new DeckState(); // 테스트 카드 보유 상태 생성
            PieceDefinition dead = CreatePiece("dead_basic_card", PieceGrade.OneStar, PieceCategory.Basic); // 사망 보유 카드 생성
            PieceDefinition candidate = CreatePiece("dead_basic_card", PieceGrade.OneStar, PieceCategory.Basic); // 획득 가능 후보 카드 생성
            int acquiredCount = 0; // 획득 이벤트 횟수 초기화

            try // 테스트 자원 정리 보장
            { // 보호 범위 시작
                for (int i = 0; i < CardOwnershipRules.DefaultOwnedLimit; i++) // 기본 상한까지 카드 준비
                { // 준비 반복 범위 시작
                    deck.AddToOwnedPool(dead); // 사망 전 카드 보유 등록
                    deck.MoveToDeadPile(dead); // 사망 카드 더미 이동
                } // 준비 반복 범위 종료

                deck.CardAcquired += card => // 획득 이벤트 처리자 연결
                { // 처리자 범위 시작
                    acquiredCount++; // 획득 이벤트 횟수 증가
                }; // 처리자 범위 종료

                bool canOffer = CardRewardRules.CanOffer(candidate, deck.OwnedCardPool, deck.DeadCardPile); // 전체 소유 기준 후보 가능 여부 확인
                bool added = CardRewardRules.TryAddOwnedCard(deck, candidate); // 전체 소유 기준 카드 획득 실행

                Assert.That(canOffer, Is.False); // 사망 카드 상한 후보 차단 검증
                Assert.That(added, Is.False); // 사망 카드 상한 획득 차단 검증
                Assert.That(acquiredCount, Is.Zero); // 실패 획득 이벤트 미발생 검증
            } // 보호 범위 종료
            finally // 테스트 자원 정리 단계
            { // 정리 범위 시작
                UnityEngine.Object.DestroyImmediate(dead); // 사망 보유 카드 제거
                UnityEngine.Object.DestroyImmediate(candidate); // 획득 후보 카드 제거
            } // 정리 범위 종료
        } // 테스트 범위 종료

        [Test] // 프로필 없는 후보 생성 검증
        public void RewardGenerator_NullProfileHonorsDeadPileLimit() // 균등 후보 생성의 사망 카드 상한 반영 검증
        { // 테스트 범위 시작
            DeckState deck = new DeckState(); // 테스트 카드 보유 상태 생성
            PieceDefinition dead = CreatePiece("dead_uniform_card", PieceGrade.OneStar, PieceCategory.Basic); // 사망 보유 카드 생성
            PieceDefinition candidate = CreatePiece("dead_uniform_card", PieceGrade.OneStar, PieceCategory.Basic); // 획득 가능 후보 카드 생성

            try // 테스트 자원 정리 보장
            { // 보호 범위 시작
                for (int i = 0; i < CardOwnershipRules.DefaultOwnedLimit; i++) // 기본 상한까지 카드 준비
                { // 준비 반복 범위 시작
                    deck.AddToOwnedPool(dead); // 사망 전 카드 보유 등록
                    deck.MoveToDeadPile(dead); // 사망 카드 더미 이동
                } // 준비 반복 범위 종료

                IReadOnlyList<PieceDefinition> candidates = CardRewardGenerator.Generate(new[] { candidate }, deck.OwnedCardPool, deck.DeadCardPile, 1, 81, null); // 프로필 없는 균등 후보 생성

                Assert.That(candidates, Is.Empty); // 사망 카드 상한 후보 제외 검증
            } // 보호 범위 종료
            finally // 테스트 자원 정리 단계
            { // 정리 범위 시작
                UnityEngine.Object.DestroyImmediate(dead); // 사망 보유 카드 제거
                UnityEngine.Object.DestroyImmediate(candidate); // 획득 후보 카드 제거
            } // 정리 범위 종료
        } // 테스트 범위 종료

        [Test] // 복원 사망 카드 업적 동기화 검증
        public void SteamIntegration_QueuesFiveStarFromDeadPile() // 사망 더미 5성의 복원 업적 연결 검증
        { // 테스트 범위 시작
            GameObject root = new GameObject("Day81SteamIntegrationRoot"); // Steam 통합 테스트 루트 생성
            SteamGameIntegrationController controller = root.AddComponent<SteamGameIntegrationController>(); // Steam 통합 Controller 생성
            DeckState deck = new DeckState(); // 테스트 카드 보유 상태 생성
            PieceDefinition dead = CreatePiece("restored_dead_five_star", PieceGrade.FiveStar, PieceCategory.Fusion); // 복원 사망 5성 생성

            try // 테스트 자원 정리 보장
            { // 보호 범위 시작
                deck.AddToOwnedPool(dead); // 사망 전 카드 보유 등록
                deck.MoveToDeadPile(dead); // 사망 카드 더미 이동
                InvokePrivateMethod(controller, "QueueExistingCardAchievements", deck); // 복원 카드 업적 동기화 실행
                HashSet<string> pending = GetPrivateField<HashSet<string>>(controller, "pendingAchievementIds"); // 대기 업적 집합 조회

                Assert.That(pending, Does.Contain(SteamAchievementIds.FirstFiveStar)); // 복원 사망 5성 업적 등록 검증
            } // 보호 범위 종료
            finally // 테스트 자원 정리 단계
            { // 정리 범위 시작
                UnityEngine.Object.DestroyImmediate(root); // Steam 통합 테스트 루트 제거
                UnityEngine.Object.DestroyImmediate(dead); // 복원 사망 5성 제거
            } // 정리 범위 종료
        } // 테스트 범위 종료

        [Test] // 식별자 기준 보유 수 검증
        public void CountOwnedCopies_UsesPieceIdAcrossDistinctDefinitions() // 별도 정의 인스턴스의 동일 카드 판정 검증
        { // 테스트 범위 시작
            RunState runState = new RunState(3); // 테스트 런 상태 생성
            PieceDefinition owned = CreatePiece("shared_piece", PieceGrade.FiveStar, PieceCategory.Fusion); // 정상 보유 카드 생성
            PieceDefinition dead = CreatePiece("shared_piece", PieceGrade.FiveStar, PieceCategory.Fusion); // 사망 보유 카드 생성
            PieceDefinition query = CreatePiece("shared_piece", PieceGrade.FiveStar, PieceCategory.Fusion); // 조회 기준 카드 생성

            try // 테스트 자원 정리 보장
            { // 보호 범위 시작
                runState.Deck.AddToOwnedPool(owned); // 정상 보유 카드 추가
                runState.Deck.AddToOwnedPool(dead); // 사망 전 보유 카드 추가
                runState.Deck.MoveToDeadPile(dead); // 사망 카드 더미 이동

                Assert.That(runState.CountOwnedCopies(query), Is.EqualTo(2)); // 동일 식별자 전체 소유 수 검증
            } // 보호 범위 종료
            finally // 테스트 자원 정리 단계
            { // 정리 범위 시작
                UnityEngine.Object.DestroyImmediate(owned); // 정상 보유 카드 제거
                UnityEngine.Object.DestroyImmediate(dead); // 사망 보유 카드 제거
                UnityEngine.Object.DestroyImmediate(query); // 조회 기준 카드 제거
            } // 정리 범위 종료
        } // 테스트 범위 종료

        [Test] // 명시적 카드 획득 이벤트 검증
        public void RewardAcquisition_RaisesCardAcquiredOnce() // 성공한 외부 카드 획득 이벤트 계약 검증
        { // 테스트 범위 시작
            DeckState deck = new DeckState(); // 테스트 카드 보유 상태 생성
            PieceDefinition acquired = CreatePiece("acquired_piece", PieceGrade.FiveStar, PieceCategory.Fusion); // 획득 카드 생성
            int acquiredCount = 0; // 획득 이벤트 횟수 초기화
            PieceDefinition acquiredCard = null; // 획득 이벤트 카드 초기화

            try // 테스트 자원 정리 보장
            { // 보호 범위 시작
                deck.CardAcquired += card => // 획득 이벤트 처리자 연결
                { // 처리자 범위 시작
                    acquiredCount++; // 획득 이벤트 횟수 증가
                    acquiredCard = card; // 실제 획득 카드 저장
                }; // 처리자 범위 종료

                bool added = CardRewardRules.TryAddOwnedCard(deck, acquired); // 실제 카드 획득 실행
                bool rejected = CardRewardRules.TryAddOwnedCard(deck, acquired); // 상한 초과 획득 실행

                Assert.That(added, Is.True); // 카드 획득 성공 검증
                Assert.That(rejected, Is.False); // 상한 초과 획득 차단 검증
                Assert.That(acquiredCount, Is.EqualTo(1)); // 획득 이벤트 1회 검증
                Assert.That(acquiredCard, Is.SameAs(acquired)); // 실제 획득 카드 전달 검증
            } // 보호 범위 종료
            finally // 테스트 자원 정리 단계
            { // 정리 범위 시작
                UnityEngine.Object.DestroyImmediate(acquired); // 획득 카드 제거
            } // 정리 범위 종료
        } // 테스트 범위 종료

        [Test] // 명시적 합성 완료 이벤트 검증
        public void TryFuseCards_RaisesFusionCompletedOnceWithActualRecipe() // 실제 합성 성공 이벤트 계약 검증
        { // 테스트 범위 시작
            GameObject root = new GameObject("Day81FusionRoot"); // 테스트 루트 생성
            BoardView boardView = root.AddComponent<BoardView>(); // 보드 뷰 생성
            BoardInputController boardInput = root.AddComponent<BoardInputController>(); // 보드 입력 생성
            RunState runState = new RunState(3); // 테스트 런 상태 생성
            TurnManager turnManager = new TurnManager(); // 배치 턴 상태 생성
            PieceDefinition materialA = CreatePiece("material_a", PieceGrade.OneStar, PieceCategory.Basic); // 첫 합성 재료 생성
            PieceDefinition materialB = CreatePiece("material_b", PieceGrade.OneStar, PieceCategory.Basic); // 둘째 합성 재료 생성
            PieceDefinition result = CreatePiece("result", PieceGrade.TwoStar, PieceCategory.Fusion); // 합성 결과 생성
            FusionRecipe recipe = CreateRecipe(materialA, materialB, result); // 테스트 합성식 생성
            FusionRecipeDatabase database = ScriptableObject.CreateInstance<FusionRecipeDatabase>(); // 합성식 데이터베이스 생성
            EventInfo completedEvent = typeof(BoardInputController).GetEvent("FusionCompleted"); // 합성 완료 이벤트 조회
            int completedCount = 0; // 완료 이벤트 발생 횟수 초기화
            FusionRecipe completedRecipe = null; // 완료 이벤트 결과 초기화

            try // 테스트 자원 정리 보장
            { // 보호 범위 시작
                Assert.That(completedEvent, Is.Not.Null, "BoardInputController에 FusionCompleted 이벤트가 필요합니다."); // 명시적 이벤트 존재 검증
                Action<FusionRecipe> handler = completed => // 완료 이벤트 처리자 생성
                { // 처리자 범위 시작
                    completedCount++; // 완료 이벤트 횟수 증가
                    completedRecipe = completed; // 실제 완료 합성식 저장
                }; // 처리자 범위 종료
                completedEvent.AddEventHandler(boardInput, handler); // 완료 이벤트 처리자 연결
                SetPrivateField(database, "_recipes", new System.Collections.Generic.List<FusionRecipe> { recipe }); // 테스트 합성식 등록
                SetPrivateField(boardInput, "_fusionRecipeDatabase", database); // 입력에 합성식 데이터베이스 연결
                boardView.Bind(runState.Board); // 보드 상태 연결
                boardInput.Bind(runState, boardView, turnManager); // 런과 턴 상태 연결
                runState.Hand.TryAddCard(materialA); // 첫 재료를 손패에 추가
                runState.Hand.TryAddCard(materialB); // 둘째 재료를 손패에 추가

                bool fused = boardInput.TryFuseCards(materialA, materialB); // 실제 합성 실행

                Assert.That(fused, Is.True); // 합성 성공 검증
                Assert.That(completedCount, Is.EqualTo(1)); // 완료 이벤트 1회 검증
                Assert.That(completedRecipe, Is.SameAs(recipe)); // 실제 합성식 전달 검증
            } // 보호 범위 종료
            finally // 테스트 자원 정리 단계
            { // 정리 범위 시작
                UnityEngine.Object.DestroyImmediate(root); // 테스트 루트 제거
                UnityEngine.Object.DestroyImmediate(materialA); // 첫 재료 제거
                UnityEngine.Object.DestroyImmediate(materialB); // 둘째 재료 제거
                UnityEngine.Object.DestroyImmediate(result); // 합성 결과 제거
                UnityEngine.Object.DestroyImmediate(recipe); // 테스트 합성식 제거
                UnityEngine.Object.DestroyImmediate(database); // 합성식 데이터베이스 제거
            } // 정리 범위 종료
        } // 테스트 범위 종료

        private static PieceDefinition CreatePiece(string pieceId, PieceGrade grade, PieceCategory category) // 테스트 기물 생성 도우미
        { // 도우미 범위 시작
            PieceDefinition definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 빈 기물 데이터 생성
            SetPrivateField(definition, "_pieceId", pieceId); // 기물 식별자 설정
            SetPrivateField(definition, "_displayName", pieceId); // 기물 표시 이름 설정
            SetPrivateField(definition, "_grade", grade); // 기물 등급 설정
            SetPrivateField(definition, "_category", category); // 기물 분류 설정
            return definition; // 완성된 기물 반환
        } // 도우미 범위 종료

        private static FusionRecipe CreateRecipe(PieceDefinition materialA, PieceDefinition materialB, PieceDefinition result) // 테스트 합성식 생성 도우미
        { // 도우미 범위 시작
            FusionRecipe recipe = ScriptableObject.CreateInstance<FusionRecipe>(); // 빈 합성식 생성
            SetPrivateField(recipe, "_recipeId", "day81_recipe"); // 합성식 식별자 설정
            SetPrivateField(recipe, "_materialA", materialA); // 첫 합성 재료 설정
            SetPrivateField(recipe, "_materialB", materialB); // 둘째 합성 재료 설정
            SetPrivateField(recipe, "_result", result); // 합성 결과 설정
            return recipe; // 완성된 합성식 반환
        } // 도우미 범위 종료

        private static void SetPrivateField(object target, string fieldName, object value) // 비공개 필드 설정 도우미
        { // 도우미 범위 시작
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance); // 대상 필드 조회
            Assert.That(field, Is.Not.Null, $"필드 {fieldName}을 찾을 수 없습니다."); // 대상 필드 존재 검증
            field.SetValue(target, value); // 대상 필드 값 설정
        } // 도우미 범위 종료

        private static T GetPrivateField<T>(object target, string fieldName) // 비공개 필드 조회 도우미
        { // 도우미 범위 시작
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance); // 대상 필드 조회
            Assert.That(field, Is.Not.Null, $"필드 {fieldName}을 찾을 수 없습니다."); // 대상 필드 존재 검증
            return (T)field.GetValue(target); // 대상 필드 값 반환
        } // 도우미 범위 종료

        private static void InvokePrivateMethod(object target, string methodName, object argument) // 비공개 메서드 실행 도우미
        { // 도우미 범위 시작
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance); // 대상 메서드 조회
            Assert.That(method, Is.Not.Null, $"메서드 {methodName}을 찾을 수 없습니다."); // 대상 메서드 존재 검증
            method.Invoke(target, new[] { argument }); // 대상 메서드 실행
        } // 도우미 범위 종료
    } // 테스트 클래스 범위 종료
} // 네임스페이스 범위 종료
