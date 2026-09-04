using System; // Random을 사용하기 위한 네임스페이스
using System.Collections; // IEnumerator 코루틴을 사용하기 위한 네임스페이스
using System.Collections.Generic; // HashSet<T>와 List<T>를 사용하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour, GameObject, Debug, Resources 등을 사용하기 위한 네임스페이스
using UnityEngine.SceneManagement; // 현재 씬이 Battle인지 확인하기 위한 네임스페이스
using ProjectEta.Battle; // BattleController를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition을 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스

namespace ProjectEta.Cards // 카드·덱 관련 타입을 모아두는 네임스페이스
{
    public sealed class PrototypePlayerDeck26Bootstrap : MonoBehaviour // 기존 기본 6종 프로토타입 덱을 26종 한 장씩으로 확장하는 런타임 부트스트랩
    {
        private const string CatalogResourcePath = "PlayerStartingDeck26"; // Resources 폴더에서 시작 덱 카탈로그를 찾을 경로
        private static readonly HashSet<string> LegacyPrototypeIds = new HashSet<string> // 기존 프로토타입 시작 덱으로 인정할 기본 6종 id 집합
        {
            "king", // 킹
            "pawn", // 폰
            "knight", // 나이트
            "bishop", // 비숍
            "rook", // 룩
            "queen" // 퀸
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Battle 씬이 로드될 때 자동으로 부트스트랩을 준비
        private static void AutoCreateForBattleScene() // 씬에 수동 컴포넌트 배치 없이 자동 실행하기 위한 진입점
        {
            if (SceneManager.GetActiveScene().name != "Battle") return; // Battle 씬이 아니면 아무 작업도 하지 않음
            if (UnityEngine.Object.FindFirstObjectByType<PrototypePlayerDeck26Bootstrap>() != null) return; // 이미 존재하면 중복 생성하지 않음

            var bootstrapObject = new GameObject("PrototypePlayerDeck26Bootstrap"); // 26종 덱 확장을 실행할 임시 오브젝트 생성
            bootstrapObject.AddComponent<PrototypePlayerDeck26Bootstrap>(); // Start 코루틴이 실행되도록 컴포넌트 추가
        }

        private IEnumerator Start() // BattleController와 기존 6종 시작 손패 구성이 끝난 뒤 실행하기 위한 코루틴
        {
            yield return null; // BattleController.Awake와 EnsurePrototypeStartingHand가 먼저 끝나도록 한 프레임 대기

            var battleController = UnityEngine.Object.FindFirstObjectByType<BattleController>(); // 현재 Battle 씬의 실제 전투 컨트롤러 탐색
            if (battleController == null || battleController.RunState == null) // 전투 상태가 준비되지 않았으면
            {
                Destroy(gameObject); // 더 이상 할 일이 없으므로 부트스트랩 오브젝트 제거
                yield break; // 실행 종료
            }

            var catalog = Resources.Load<PlayerStartingDeckCatalog>(CatalogResourcePath); // 26종 PieceDefinition이 등록된 Resources 카탈로그 로드
            if (catalog == null) // 카탈로그를 찾지 못하면
            {
                Debug.LogError("26종 플레이어 시작 덱 카탈로그를 찾지 못했습니다: Resources/PlayerStartingDeck26.asset"); // 설정 오류를 명확히 출력
                Destroy(gameObject); // 임시 오브젝트 제거
                yield break; // 실행 종료
            }

            bool changed = TryExpandPrototypeDeck(battleController.RunState, catalog.Cards); // 현재 기본 6종 프로토타입 덱을 26종으로 확장 시도
            if (changed) // 실제 카드가 추가됐다면
            {
                Debug.Log($"플레이어 시작 덱을 26종 한 장씩으로 확장했습니다. Owned={battleController.RunState.Deck.OwnedCardPool.Count}, Hand={battleController.RunState.Hand.Hand.Count}, Draw={battleController.RunState.Deck.DrawPile.Count}"); // 최종 덱 상태 출력
            }

            Destroy(gameObject); // 한 번만 실행하면 되므로 부트스트랩 오브젝트 제거
        }

        public static bool TryExpandPrototypeDeck(RunState runState, IReadOnlyList<PieceDefinition> catalogCards, int? shuffleSeed = null) // 테스트와 런타임이 함께 사용하는 실제 덱 확장 로직
        {
            if (runState == null || catalogCards == null || catalogCards.Count != 26) return false; // 필수 상태와 26종 카탈로그가 아니면 변경하지 않음
            if (runState.CurrentRound != 1 || runState.Deck.DeadCardPile.Count > 0) return false; // 진행 중인 런이나 이미 사망 카드가 있는 런은 자동 변경하지 않음

            var canonicalById = new Dictionary<string, PieceDefinition>(); // 카탈로그의 고유 id와 실제 정의를 연결할 사전 생성
            foreach (var card in catalogCards) // 26종 카탈로그 순회
            {
                if (card == null || string.IsNullOrWhiteSpace(card.PieceId)) return false; // null 또는 id 없는 카드가 있으면 잘못된 카탈로그로 간주
                if (canonicalById.ContainsKey(card.PieceId)) return false; // 같은 기물이 두 번 들어 있으면 한 장씩 조건을 만족하지 못하므로 중단
                canonicalById.Add(card.PieceId, card); // 정상 고유 카드 등록
            }

            if (canonicalById.Count != 26) return false; // 최종 고유 기물 수도 반드시 26종이어야 함

            if (HasCompleteDeck(runState, canonicalById)) return false; // 이미 26종 한 장씩 보유 중이면 중복 추가하지 않음
            if (!IsLegacyPrototypeState(runState)) return false; // 기존 기본 6종 프로토타입 상태가 아니면 사용자 커스텀/세이브 덱을 보호

            var ownedIds = new HashSet<string>(); // 현재 OwnedCardPool의 id 집합 생성
            foreach (var card in runState.Deck.OwnedCardPool) // 현재 보유 카드 순회
            {
                if (card != null) ownedIds.Add(card.PieceId); // null이 아닌 카드 id 기록
            }

            var handIds = new HashSet<string>(); // 현재 손패 id 집합 생성
            foreach (var card in runState.Hand.Hand) // 현재 손패 순회
            {
                if (card != null) handIds.Add(card.PieceId); // 손패 카드 id 기록
            }

            var drawIds = new HashSet<string>(); // 현재 DrawPile id 집합 생성
            foreach (var card in runState.Deck.DrawPile) // 현재 드로우 더미 순회
            {
                if (card != null) drawIds.Add(card.PieceId); // 드로우 카드 id 기록
            }

            var missingCards = new List<PieceDefinition>(); // 기존 6종에 없는 20종을 담을 목록 생성
            foreach (var pair in canonicalById) // 카탈로그 26종 전체 순회
            {
                if (!ownedIds.Contains(pair.Key)) missingCards.Add(pair.Value); // OwnedCardPool에 없는 기물만 추가 대상으로 수집
            }

            if (missingCards.Count == 0) return false; // 실제 누락 카드가 없으면 변경하지 않음

            Shuffle(missingCards, shuffleSeed); // 새로 추가되는 카드들의 드로우 순서를 무작위화

            foreach (var card in missingCards) // 누락된 신규 기물을 한 장씩 순회
            {
                runState.Deck.AddToOwnedPool(card); // 영구 보유 풀에 정확히 한 장 추가

                if (!handIds.Contains(card.PieceId) && !drawIds.Contains(card.PieceId)) // 이미 손패나 드로우 더미에 같은 카드가 없다면
                {
                    runState.Deck.AddToDrawPile(card); // 현재 라운드에서도 바로 뽑을 수 있도록 드로우 더미에 한 장 추가
                    drawIds.Add(card.PieceId); // 이후 중복 삽입 방지를 위해 id 기록
                }
            }

            return true; // 실제 덱 확장이 수행됐음을 반환
        }

        private static bool IsLegacyPrototypeState(RunState runState) // 자동 확장을 허용할 기존 기본 6종 테스트 덱인지 판별
        {
            if (runState.Deck.OwnedCardPool.Count > 6) return false; // 기본 6종보다 많은 카드가 이미 있으면 커스텀 또는 확장된 덱으로 간주

            foreach (var card in runState.Deck.OwnedCardPool) // OwnedCardPool 검사
            {
                if (card == null || !LegacyPrototypeIds.Contains(card.PieceId)) return false; // 기본 6종 외 카드가 있으면 자동 변경 금지
            }

            foreach (var card in runState.Hand.Hand) // 손패 검사
            {
                if (card == null || !LegacyPrototypeIds.Contains(card.PieceId)) return false; // 기본 6종 외 카드가 있으면 자동 변경 금지
            }

            foreach (var card in runState.Deck.DrawPile) // 드로우 더미 검사
            {
                if (card == null || !LegacyPrototypeIds.Contains(card.PieceId)) return false; // 기본 6종 외 카드가 있으면 자동 변경 금지
            }

            return true; // 현재 상태가 기존 기본 6종 프로토타입 범위 안에 있으므로 확장 허용
        }

        private static bool HasCompleteDeck(RunState runState, Dictionary<string, PieceDefinition> canonicalById) // 이미 26종 한 장씩인지 검사
        {
            if (runState.Deck.OwnedCardPool.Count != 26) return false; // 총 보유 장수가 26이 아니면 완성 상태가 아님

            var ids = new HashSet<string>(); // 중복 id 검사 집합 생성
            foreach (var card in runState.Deck.OwnedCardPool) // 보유 카드 전체 순회
            {
                if (card == null || !canonicalById.ContainsKey(card.PieceId)) return false; // 카탈로그 외 카드가 있으면 완성된 테스트 덱이 아님
                if (!ids.Add(card.PieceId)) return false; // 같은 기물이 두 장이면 한 장씩 조건 위반
            }

            return ids.Count == 26; // 정확히 26종 고유 카드가 있으면 완성 상태
        }

        private static void Shuffle(List<PieceDefinition> cards, int? shuffleSeed) // 신규 20종의 드로우 순서를 섞는 도우미
        {
            var random = shuffleSeed.HasValue ? new System.Random(shuffleSeed.Value) : new System.Random(); // 테스트에서는 고정 시드, 실제 플레이에서는 새 난수 사용

            for (int i = cards.Count - 1; i > 0; i--) // Fisher-Yates 방식으로 뒤에서 앞으로 순회
            {
                int swapIndex = random.Next(i + 1); // 0부터 현재 위치까지 무작위 교환 인덱스 선택
                var temporary = cards[i]; // 현재 카드 임시 보관
                cards[i] = cards[swapIndex]; // 선택 카드와 현재 카드 교환
                cards[swapIndex] = temporary; // 임시 카드 복원
            }
        }
    }
}
