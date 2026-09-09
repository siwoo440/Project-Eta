using System.Collections.Generic; // CardView·Offset 목록 사용
using System.Linq; // FusionMaterials.Contains 사용
using UnityEngine; // MonoBehaviour·RectTransformUtility 사용
using UnityEngine.InputSystem; // Mouse 입력 사용
using UnityEngine.SceneManagement; // Battle 씬 자동 생성
using UnityEngine.UI; // HorizontalLayoutGroup 사용
using ProjectEta.Board; // BoardInputController 사용

namespace ProjectEta.UI
{
    [DefaultExecutionOrder(1300)]
    public sealed class Day66HandCardMotionController : MonoBehaviour
    {
        private const float ScanInterval = 0.12f; // 손패 재생성 탐색 주기
        private const float LiftSpeed = 420f; // 카드 상하 이동 속도

        private readonly List<CardView> _cards = new List<CardView>(); // 현재 손패 카드 목록
        private readonly Dictionary<int, float> _offsetByInstanceId = new Dictionary<int, float>(); // 카드별 현재 이동 거리
        private BoardInputController _boardInput; // Fusion 선택 상태 제공 입력 컨트롤러
        private float _nextScanTime; // 다음 손패 탐색 시간

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreateForBattleScene()
        {
            if (SceneManager.GetActiveScene().name != "Battle") return; // Battle 씬 외 생성 차단
            if (Object.FindFirstObjectByType<Day66HandCardMotionController>() != null) return; // 중복 생성 차단

            GameObject host = new GameObject("Day66HandCardMotionController"); // 66일차 카드 모션 호스트 생성
            host.AddComponent<Day66HandCardMotionController>(); // 손패 카드 상하 모션 추가
        }

        private void Update()
        {
            if (_boardInput == null) _boardInput = Object.FindFirstObjectByType<BoardInputController>(); // 전투 입력 지연 탐색
            if (Time.unscaledTime >= _nextScanTime) RefreshCardList(); // 손패 카드 목록 주기 갱신
        }

        private void LateUpdate()
        {
            if (_cards.Count == 0) return; // 손패 카드 없음 처리

            Vector2 pointerPosition = Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(-10000f, -10000f); // 현재 마우스 화면 좌표 조회
            bool leftPressed = Mouse.current != null && Mouse.current.leftButton.isPressed; // 좌클릭 선택 입력 확인

            for (int i = 0; i < _cards.Count; i++)
            {
                CardView card = _cards[i]; // 현재 카드 조회
                if (card == null || card.RectTransform == null) continue; // 파괴된 카드 제외

                RectTransform rect = card.RectTransform; // 카드 RectTransform 조회
                HorizontalLayoutGroup layout = rect.parent != null ? rect.parent.GetComponent<HorizontalLayoutGroup>() : null; // 손패 레이아웃 조회
                float baseY = ResolveLayoutBaseY(rect, layout); // 레이아웃 기본 Y 계산
                bool hovered = RectTransformUtility.RectangleContainsScreenPoint(rect, pointerPosition, null); // 현재 포인터 카드 Hover 확인
                bool fusionSelected = IsFusionSelected(card); // Fusion 재료 선택 여부 확인
                bool pointerHeld = card.IsInteractable && hovered && leftPressed; // 현재 카드 선택 입력 유지 확인
                float targetOffset = Day66CardLiftState.ResolveTargetOffset(card.IsInteractable, hovered, fusionSelected, pointerHeld); // 카드 목표 상하 위치 계산
                int instanceId = card.GetInstanceID(); // 카드 런타임 식별자 조회

                if (!_offsetByInstanceId.TryGetValue(instanceId, out float currentOffset)) currentOffset = Day66CardLiftState.IdleOffset; // 신규 카드 기본 하강 위치 지정
                currentOffset = Mathf.MoveTowards(currentOffset, targetOffset, LiftSpeed * Time.unscaledDeltaTime); // 부드러운 카드 상하 이동
                _offsetByInstanceId[instanceId] = currentOffset; // 현재 카드 이동 거리 저장

                Vector2 anchored = rect.anchoredPosition; // 현재 레이아웃 X 위치 보존
                anchored.y = baseY + currentOffset; // 대기·선택 상태 Y 위치 적용
                rect.anchoredPosition = anchored; // 카드 위치 반영
            }
        }

        private void RefreshCardList()
        {
            _nextScanTime = Time.unscaledTime + ScanInterval; // 다음 탐색 시간 예약
            CardView[] found = Object.FindObjectsByType<CardView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None); // 활성 손패 카드 전체 조회
            _cards.Clear(); // 이전 카드 목록 초기화

            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != null) _cards.Add(found[i]); // 유효 카드 목록 등록
            }

            CleanupOffsetCache(); // 제거된 카드 이동 상태 정리
        }

        private void CleanupOffsetCache()
        {
            var activeIds = new HashSet<int>(); // 현재 카드 식별자 집합 생성

            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] != null) activeIds.Add(_cards[i].GetInstanceID()); // 활성 카드 식별자 등록
            }

            var removeIds = new List<int>(); // 제거할 이전 카드 식별자 목록 생성

            foreach (KeyValuePair<int, float> pair in _offsetByInstanceId)
            {
                if (!activeIds.Contains(pair.Key)) removeIds.Add(pair.Key); // 현재 손패에 없는 카드 기록
            }

            for (int i = 0; i < removeIds.Count; i++) _offsetByInstanceId.Remove(removeIds[i]); // 제거된 카드 상태 삭제
        }

        private bool IsFusionSelected(CardView card)
        {
            if (card == null || card.Definition == null || _boardInput == null) return false; // 필수 상태 누락 방어
            if (!_boardInput.IsFusionModeActive) return false; // Fusion 모드 외 선택 처리 제외
            return _boardInput.FusionMaterials.Contains(card.Definition); // 현재 Fusion 재료 포함 여부 반환
        }

        private static float ResolveLayoutBaseY(RectTransform rect, HorizontalLayoutGroup layout)
        {
            if (rect == null) return 0f; // 카드 RectTransform 누락 방어
            if (layout == null) return rect.anchoredPosition.y; // 레이아웃 없는 카드 현재 위치 사용

            float height = rect.rect.height > 0f ? rect.rect.height : rect.sizeDelta.y; // 카드 실제 높이 계산
            return layout.padding.bottom + height * rect.pivot.y; // LowerCenter 손패 기본 Y 계산
        }
    }
}
