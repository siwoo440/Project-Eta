using System.Collections.Generic; // 현재 화면에 생성된 CardView 목록을 관리하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour, GameObject, Vector2Int 등을 사용하기 위한 네임스페이스
using UnityEngine.EventSystems; // EventSystem을 런타임 생성하기 위한 네임스페이스
using UnityEngine.InputSystem.UI; // 새 Input System 기반 UI 포인터 입력 모듈을 사용하기 위한 네임스페이스
using UnityEngine.UI; // Canvas, CanvasScaler, GraphicRaycaster, HorizontalLayoutGroup 등을 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardInputController를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition과 PieceMovementType을 사용하기 위한 네임스페이스

namespace ProjectEta.UI // 프로젝트 η 런타임 UI 타입을 모아두는 네임스페이스
{
    public class HandUI : MonoBehaviour // 화면 하단 중앙에 실제 손패 카드를 만들고 드래그 Drop을 보드 소환으로 연결하는 컴포넌트
    {
        public int CardCount => _cardViews.Count; // 현재 화면에 표시 중인 손패 카드 UI 수
        public Transform DragLayer => _canvasTransform; // CardView가 드래그 중 사용할 Canvas 최상위 레이어
        public bool IsHandLowered => _isHandLowered; // 카드가 보드 쪽으로 올라가 손패가 화면 아래로 내려간 상태인지 여부
        public Vector2 HandAnchoredPosition => _handRoot != null ? _handRoot.anchoredPosition : Vector2.zero; // 테스트와 디버그에서 현재 손패 위치를 확인하는 프로퍼티

        private BoardInputController _boardInput; // 실제 HandState와 카드 소환 규칙을 제공하는 입력 컨트롤러
        private Canvas _canvas; // 화면 하단 손패 전용 Screen Space Overlay Canvas
        private Transform _canvasTransform; // Canvas Transform 캐시
        private RectTransform _handRoot; // 화면 하단 중앙 카드 정렬 컨테이너
        private HorizontalLayoutGroup _layoutGroup; // 카드들을 가로로 겹쳐 정렬하는 레이아웃
        private readonly List<CardView> _cardViews = new List<CardView>(); // 현재 생성된 손패 CardView 목록
        private EventSystem _createdEventSystem; // 이 HandUI가 직접 만든 EventSystem 참조
        private bool _refreshQueued; // 드래그 이벤트 도중 손패가 바뀌어도 안전하게 다음 프레임에 UI를 재구성하기 위한 플래그
        private bool _isHandLowered; // 카드가 손패 영역을 벗어나 위로 올라갔을 때 손패를 화면 아래로 숨긴 상태
        private readonly Vector2 _normalHandPosition = new Vector2(0f, 14f); // 평상시 손패의 화면 하단 위치
        private readonly Vector2 _loweredHandPosition = new Vector2(0f, -245f); // 드래그 중 보드를 가리지 않도록 손패 대부분을 화면 아래로 내리는 위치

        public void Bind(BoardInputController boardInput) // 실제 전투 입력·손패 상태를 카드 UI에 연결하는 메서드
        {
            if (_boardInput != null) // 이전 BoardInputController가 연결돼 있었다면
            {
                _boardInput.HandChanged -= HandleHandChanged; // 이전 손패 변경 이벤트 구독 해제
                if (_boardInput.TurnManager != null) _boardInput.TurnManager.TurnChanged -= HandleTurnChanged; // 이전 턴 이벤트 구독 해제
            }

            _boardInput = boardInput; // 새 실제 입력 컨트롤러 저장
            EnsureCanvas(); // 하단 손패 Canvas와 EventSystem을 런타임 생성

            if (_boardInput != null) // 정상 입력 컨트롤러가 전달됐다면
            {
                _boardInput.HandChanged += HandleHandChanged; // Draw·소환 등 실제 손패 변화 이벤트 구독
                if (_boardInput.TurnManager != null) _boardInput.TurnManager.TurnChanged += HandleTurnChanged; // 턴 상태 변화 이벤트 구독
            }

            Refresh(); // 현재 HandState를 즉시 카드 UI로 재구성
        }

        public bool CanDragCard(PieceDefinition card) // CardView가 현재 카드 드래그 가능 여부를 묻는 메서드
        {
            return _boardInput != null && _boardInput.CanSummonCard(card); // 실제 턴·킹 필수·손패 검증 결과 사용
        }

        public void BeginCardDrag(CardView cardView, Vector2 screenPosition) // 카드 드래그 시작 시 손패·Drop 미리보기를 준비하는 메서드
        {
            if (cardView == null || _boardInput == null) return; // 연결이 누락됐으면 종료
            UpdateHandVisibilityForDrag(screenPosition); // 마우스가 손패 위쪽으로 올라갔는지 확인해 손패 숨김 상태 갱신
            _boardInput.PreviewCardDrop(cardView.Definition, screenPosition); // 현재 마우스 아래 보드 셀에 기물 고스트 프리뷰 표시
        }

        public void UpdateCardDrag(CardView cardView, Vector2 screenPosition) // 카드가 마우스를 따라 이동할 때 손패 숨김과 기물 고스트를 갱신하는 메서드
        {
            if (cardView == null || _boardInput == null) return; // 연결이 누락됐으면 종료
            UpdateHandVisibilityForDrag(screenPosition); // 카드를 충분히 위로 끌면 손패가 아래로 내려가 시야를 확보
            _boardInput.PreviewCardDrop(cardView.Definition, screenPosition); // 현재 화면 좌표의 3D 기물 고스트 위치 갱신
        }

        public void EndCardDrag() // 좌클릭을 놓아 카드 드래그가 끝났을 때 손패와 보드 프리뷰를 정리하는 메서드
        {
            RestoreHandAfterDrag(); // 손패를 원래 화면 하단 위치로 되돌림
            _boardInput?.ClearCardDropPreview(); // 기물 고스트와 보드 셀 강조를 정리
        }

        public void LowerHandForDrag() // 카드가 위로 올라갔을 때 손패를 화면 아래로 내려 보드를 가리지 않게 하는 메서드
        {
            if (_handRoot == null || _isHandLowered) return; // 손패가 없거나 이미 내려가 있으면 중복 처리하지 않음
            _isHandLowered = true; // 내려간 상태 기록
            _handRoot.anchoredPosition = _loweredHandPosition; // 손패 전체를 화면 아래쪽으로 이동
        }

        public void RestoreHandAfterDrag() // 드래그가 끝나면 손패를 다시 기본 위치로 복원하는 메서드
        {
            if (_handRoot == null) return; // 손패가 아직 생성되지 않았으면 종료
            _isHandLowered = false; // 내려간 상태 해제
            _handRoot.anchoredPosition = _normalHandPosition; // 평상시 하단 중앙 위치로 복원
        }

        private void UpdateHandVisibilityForDrag(Vector2 screenPosition) // 현재 마우스가 손패 위쪽으로 충분히 벗어났는지 판정하는 메서드
        {
            if (_handRoot == null || _isHandLowered) return; // 이미 내려간 뒤에는 드래그 종료 전까지 계속 숨긴 상태 유지
            var corners = new Vector3[4]; // 손패 RectTransform의 네 모서리를 받을 배열 생성
            _handRoot.GetWorldCorners(corners); // 현재 화면에 보이는 손패 영역의 월드 좌표 계산
            float handTopScreenY = RectTransformUtility.WorldToScreenPoint(null, corners[1]).y; // 손패 좌상단의 화면 Y 좌표 계산
            if (screenPosition.y > handTopScreenY + 18f) LowerHandForDrag(); // 카드가 손패 상단보다 충분히 위로 올라가면 손패를 아래로 숨김
        }

        public bool TryDropCardFromScreen(CardView cardView, Vector2 screenPosition) // 실제 좌클릭 Release 화면 좌표를 보드 셀 소환으로 변환하는 메서드
        {
            if (cardView == null || _boardInput == null) return false; // 카드 또는 전투 연결이 없으면 실패
            if (!_boardInput.TryGetBoardCellFromScreenPoint(screenPosition, out var cell)) return false; // 보드 밖 Release면 실패
            return TryDropCardAtCellInternal(cardView, cell, deferRefresh: true); // 드래그 콜백이 끝나기 전 CardView를 파괴하지 않도록 UI 갱신을 다음 프레임으로 미룸
        }

        public bool TryDropCardAtCell(CardView cardView, Vector2Int cell) // 테스트와 디버그에서 카드 UI를 특정 보드 셀에 직접 Drop하는 진입점
        {
            return TryDropCardAtCellInternal(cardView, cell, deferRefresh: false); // 직접 호출은 기존처럼 즉시 카드 수를 확인할 수 있게 갱신
        }

        private bool TryDropCardAtCellInternal(CardView cardView, Vector2Int cell, bool deferRefresh) // 실제 카드 소환과 손패 UI 갱신 시점을 분리하는 공통 메서드
        {
            if (cardView == null || _boardInput == null || cardView.Definition == null) return false; // 필수 정보가 없으면 실패
            bool result = _boardInput.TrySummonCardFromUI(cardView.Definition, cell); // 실제 턴·영역·점유 규칙을 거쳐 소환 시도
            if (result) // 소환에 성공했다면
            {
                if (deferRefresh) _refreshQueued = true; // OnEndDrag가 고스트 정리를 마친 뒤 LateUpdate에서 안전하게 카드 UI를 다시 그림
                else
                {
                    _refreshQueued = false; // 직접 Drop 테스트에서는 예약 갱신을 제거
                    Refresh(); // 즉시 최신 HandState 기준으로 카드 UI 재구성
                }
            }
            else RefreshInteractableStates(); // 실패 시 카드 수는 유지하고 현재 활성 상태만 다시 계산
            return result; // 실제 소환 결과 반환
        }

        public CardView FindCardView(PieceDefinition definition) // 테스트와 디버그에서 특정 카드 UI를 찾는 메서드
        {
            foreach (var view in _cardViews) // 현재 카드 목록 순회
            {
                if (view != null && view.Definition == definition) return view; // 같은 PieceDefinition을 표시하는 CardView 반환
            }
            return null; // 해당 카드가 없으면 null 반환
        }

        public CardView FindFirstNonKingCardView() // 테스트와 디버그에서 첫 비킹 카드 UI를 찾는 메서드
        {
            foreach (var view in _cardViews) // 현재 카드 목록 순회
            {
                if (view != null && view.Definition != null && view.Definition.MovementType != PieceMovementType.King) return view; // 첫 비킹 카드 반환
            }
            return null; // 비킹 카드가 없으면 null 반환
        }

        public void Refresh() // 실제 HandState의 카드 목록을 화면 하단 카드 UI로 완전히 다시 그리는 메서드
        {
            EnsureCanvas(); // Canvas가 없다면 먼저 생성
            ClearCardViews(); // 이전 손패 카드 UI 제거
            if (_boardInput == null || _boardInput.HandState == null) return; // 실제 손패가 없으면 빈 UI 유지

            var hand = _boardInput.HandState.Hand; // 실제 RunState.Hand 목록 참조
            for (int i = 0; i < hand.Count; i++) // 현재 손패 순서대로 카드 생성
            {
                var cardObject = new GameObject($"CardView_{i + 1}_{hand[i].name}", typeof(RectTransform)); // 카드 UI GameObject 생성
                cardObject.transform.SetParent(_handRoot, false); // 하단 가로 레이아웃에 연결
                var cardView = cardObject.AddComponent<CardView>(); // 카드 시각화·드래그 컴포넌트 추가
                cardView.Bind(hand[i], i, this); // 실제 카드 데이터와 현재 슬롯 번호 연결
                _cardViews.Add(cardView); // 화면 카드 목록에 등록
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_handRoot); // 카드 수 변경 직후 레이아웃 즉시 갱신
        }

        private void RefreshInteractableStates() // 카드 수는 유지하고 현재 턴에 따른 활성/잠금만 갱신하는 메서드
        {
            foreach (var cardView in _cardViews) // 모든 카드 UI 순회
            {
                if (cardView != null) cardView.SetInteractable(CanDragCard(cardView.Definition)); // 현재 턴 규칙 반영
            }
        }

        private void HandleHandChanged() // 실제 HandState 변경 이벤트 처리 메서드
        {
            _refreshQueued = true; // CardView 드래그 콜스택 중 즉시 파괴하지 않도록 다음 프레임 갱신 예약
        }

        private void LateUpdate() // 입력 이벤트 처리가 끝난 뒤 예약된 손패 UI 갱신을 안전하게 수행하는 메서드
        {
            if (!_refreshQueued) return; // 예약된 갱신이 없으면 종료
            _refreshQueued = false; // 이번 프레임에서 예약을 소비
            Refresh(); // 자동 드로우·숫자키 소환 등 외부 손패 변경을 실제 카드 UI에 반영
        }

        private void HandleTurnChanged(ProjectEta.Battle.TurnState state, int turnNumber) // 턴 상태 변경 이벤트 처리 메서드
        {
            RefreshInteractableStates(); // 현재 턴에서 드래그 가능한 카드만 갱신
        }

        private void EnsureCanvas() // 하단 손패 UI에 필요한 Canvas·레이아웃·EventSystem을 한 번만 생성하는 메서드
        {
            if (_canvas != null) return; // 이미 Canvas가 있으면 중복 생성하지 않음
            EnsureEventSystem(); // 포인터·드래그 이벤트를 처리할 EventSystem 보장

            var canvasObject = new GameObject("HandCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 손패 전용 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // BattleController 또는 테스트 호스트의 자식으로 연결
            _canvasTransform = canvasObject.transform; // 드래그 레이어로 사용할 Transform 저장
            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 확보
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 위에 직접 표시
            _canvas.sortingOrder = 90; // 상단 TurnStatusUI보다 아래, 월드보다 위에 표시

            var scaler = canvasObject.GetComponent<CanvasScaler>(); // 해상도 대응용 CanvasScaler 확보
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기에 맞춰 UI 보정
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 카드 UI 기준 해상도
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 다양한 화면 비율 대응
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 보정

            var handRootObject = new GameObject("HandRoot", typeof(RectTransform), typeof(HorizontalLayoutGroup)); // 카드 가로 정렬 컨테이너 생성
            handRootObject.transform.SetParent(canvasObject.transform, false); // Canvas 자식으로 연결
            _handRoot = handRootObject.GetComponent<RectTransform>(); // RectTransform 확보
            _handRoot.anchorMin = new Vector2(0.5f, 0f); // 화면 하단 중앙 앵커
            _handRoot.anchorMax = new Vector2(0.5f, 0f); // 고정 하단 중앙 앵커
            _handRoot.pivot = new Vector2(0.5f, 0f); // 하단 중앙 피벗
            _handRoot.anchoredPosition = _normalHandPosition; // 화면 아래에서 약간 띄운 기본 손패 위치 적용
            _handRoot.sizeDelta = new Vector2(1540f, 270f); // 최대 10장 카드 영역 확보

            _layoutGroup = handRootObject.GetComponent<HorizontalLayoutGroup>(); // 가로 레이아웃 확보
            _layoutGroup.childAlignment = TextAnchor.LowerCenter; // 카드들을 하단 중앙 정렬
            _layoutGroup.spacing = -24f; // 카드가 살짝 겹쳐 10장도 화면에 들어오게 설정
            _layoutGroup.padding = new RectOffset(8, 8, 5, 5); // 손패 내부 여백 적용
            _layoutGroup.childControlWidth = false; // CardView 고정 너비 사용
            _layoutGroup.childControlHeight = false; // CardView 고정 높이 사용
            _layoutGroup.childForceExpandWidth = false; // 자동 가로 확장 금지
            _layoutGroup.childForceExpandHeight = false; // 자동 세로 확장 금지
        }

        private void EnsureEventSystem() // 카드 포인터·드래그 입력을 처리할 EventSystem을 보장하는 메서드
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return; // 기존 EventSystem이 있으면 그대로 사용
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 새 Input System용 EventSystem 생성
            _createdEventSystem = eventSystemObject.GetComponent<EventSystem>(); // 이 HandUI가 만든 EventSystem 참조 저장
        }

        private void ClearCardViews() // 현재 생성된 손패 CardView를 모두 제거하는 메서드
        {
            foreach (var cardView in _cardViews) // 기존 카드 목록 순회
            {
                if (cardView == null) continue; // 이미 제거된 카드면 건너뜀
                if (Application.isPlaying) Destroy(cardView.gameObject); // Play Mode에서는 프레임 끝에 제거
                else DestroyImmediate(cardView.gameObject); // EditMode 테스트에서는 즉시 제거
            }
            _cardViews.Clear(); // 내부 카드 목록도 비움
        }

        private void OnDestroy() // HandUI 호스트 제거 시 이벤트와 직접 생성한 EventSystem을 정리하는 메서드
        {
            if (_boardInput != null) // 입력 컨트롤러가 연결돼 있으면
            {
                _boardInput.HandChanged -= HandleHandChanged; // 손패 변경 이벤트 해제
                if (_boardInput.TurnManager != null) _boardInput.TurnManager.TurnChanged -= HandleTurnChanged; // 턴 이벤트 해제
            }

            if (_createdEventSystem != null) // 이 HandUI가 만든 EventSystem이 남아 있으면
            {
                if (Application.isPlaying) Destroy(_createdEventSystem.gameObject); // Play Mode에서는 안전하게 제거
                else DestroyImmediate(_createdEventSystem.gameObject); // EditMode에서는 즉시 제거
            }
        }
    }
}
