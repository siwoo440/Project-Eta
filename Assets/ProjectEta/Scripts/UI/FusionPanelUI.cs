using UnityEngine; // MonoBehaviour, GameObject, Vector2, Color 등을 사용하기 위한 네임스페이스
using UnityEngine.EventSystems; // EventSystem을 런타임 생성하기 위한 네임스페이스
using UnityEngine.InputSystem.UI; // 새 Input System 기반 UI 포인터 입력 모듈을 사용하기 위한 네임스페이스
using UnityEngine.UI; // Canvas, Button, Image, Text 등을 사용하기 위한 네임스페이스
using ProjectEta.Battle; // TurnState를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardInputController를 사용하기 위한 네임스페이스
using ProjectEta.Fusion; // 22일차: FusionRecipe, FusionBlockReason, FusionRuleValidator를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition, PieceMovementType을 사용하기 위한 네임스페이스

namespace ProjectEta.UI // 프로젝트 η 런타임 UI 타입을 모아두는 네임스페이스
{
    public class FusionPanelUI : MonoBehaviour // 21일차: "합성" 버튼과 재료 2장·결과 미리보기 패널을 관리하는 컴포넌트
    {
        public bool IsPanelVisible => _panelRoot != null && _panelRoot.activeSelf; // 테스트와 디버그에서 패널이 열려 있는지 확인하는 프로퍼티

        private BoardInputController _boardInput; // 실제 합성 상태·규칙을 제공하는 입력 컨트롤러
        private Canvas _canvas; // 이 UI 전용 Screen Space Overlay Canvas
        private Button _toggleButton; // 화면 하단의 "합성" 진입/종료 버튼
        private Text _toggleButtonText; // 토글 버튼 표시 문구
        private GameObject _panelRoot; // 재료·결과 미리보기 패널 루트(토글에 따라 켜고 끔, 전체 화면을 가리지 않음)
        private Image _materialSlotAImage; // 재료 A 슬롯 초상화 이미지
        private Text _materialSlotAText; // 재료 A 슬롯 이름 텍스트
        private Button _materialSlotAButton; // 22일차: 재료 A 슬롯을 눌러 해당 재료만 빼기 위한 버튼
        private Text _materialSlotAHintText; // 22일차: 재료 A 슬롯 하단의 "클릭하여 제외" 안내 텍스트
        private Image _materialSlotBImage; // 재료 B 슬롯 초상화 이미지
        private Text _materialSlotBText; // 재료 B 슬롯 이름 텍스트
        private Button _materialSlotBButton; // 22일차: 재료 B 슬롯을 눌러 해당 재료만 빼기 위한 버튼
        private Text _materialSlotBHintText; // 22일차: 재료 B 슬롯 하단의 "클릭하여 제외" 안내 텍스트
        private Image _resultSlotImage; // 결과 슬롯 초상화 이미지
        private Text _resultNameText; // 결과 슬롯 이름 텍스트
        private Text _resultStatsText; // 결과 슬롯 등급·ATK·HP 텍스트
        private Text _resultDescriptionText; // 결과 슬롯 설명 텍스트
        private Text _discoveryNoticeText; // 22일차: 숨김 합성식을 처음 발견했을 때 잠깐 뜨는 알림 텍스트
        private float _discoveryNoticeRemainingSeconds; // 22일차: 발견 알림이 화면에 남아 있을 시간(초)
        private Button _confirmButton; // "합성" 확정 버튼
        private Text _confirmButtonText; // 확정 버튼 문구(재료 부족/합성 가능/불가 안내 겸용)
        private EventSystem _createdEventSystem; // 이 컴포넌트가 직접 만든 EventSystem 참조
        private static Font _runtimeFont; // 한글 표시용 런타임 폰트 캐시
        private const float DiscoveryNoticeDurationSeconds = 3f; // 22일차: 숨김 합성식 발견 알림을 유지할 시간(초)

        public void Bind(BoardInputController boardInput) // 실제 BoardInputController와 합성 상태를 이 UI에 연결하는 메서드
        {
            if (_boardInput != null) // 이전에 연결된 입력 컨트롤러가 있었다면
            {
                _boardInput.FusionSelectionChanged -= HandleFusionSelectionChanged; // 이전 합성 선택 이벤트 구독 해제
                _boardInput.HiddenRecipeDiscovered -= HandleHiddenRecipeDiscovered; // 22일차: 이전 숨김 레시피 발견 이벤트 구독 해제
                if (_boardInput.TurnManager != null) _boardInput.TurnManager.TurnChanged -= HandleTurnChanged; // 이전 턴 이벤트 구독 해제
            }

            _boardInput = boardInput; // 새 실제 입력 컨트롤러 저장
            EnsureUI(); // 버튼·패널 UI를 런타임 생성(최초 1회)

            if (_boardInput != null) // 정상 입력 컨트롤러가 전달됐다면
            {
                _boardInput.FusionSelectionChanged += HandleFusionSelectionChanged; // 합성 모드·재료 선택 변화 이벤트 구독
                _boardInput.HiddenRecipeDiscovered += HandleHiddenRecipeDiscovered; // 22일차: 숨김 합성식을 처음 발견했을 때 알림을 띄우기 위해 구독
                if (_boardInput.TurnManager != null) _boardInput.TurnManager.TurnChanged += HandleTurnChanged; // 배치 턴 진입/이탈에 따라 버튼 상태 갱신
            }

            RefreshToggleButtonInteractable(); // 현재 턴 기준으로 버튼 사용 가능 여부 즉시 반영
            RefreshPanel(); // 현재 합성 상태로 패널 내용 즉시 반영
        }

        private void HandleFusionSelectionChanged() // 합성 모드 On/Off, 재료 선택, 결과 미리보기가 바뀔 때 호출되는 이벤트 처리 메서드
        {
            RefreshPanel(); // 패널 표시 상태와 내용을 실제 상태에 맞춰 다시 그림
        }

        private void HandleHiddenRecipeDiscovered(FusionRecipe recipe) // 22일차: 숨김 합성식을 이번 합성으로 처음 발견했을 때 호출되는 이벤트 처리 메서드
        {
            if (recipe == null || recipe.Result == null || _discoveryNoticeText == null) return; // 표시할 정보나 텍스트가 없으면 종료

            string resultName = string.IsNullOrEmpty(recipe.Result.DisplayName) ? recipe.Result.name : recipe.Result.DisplayName; // 결과 기물 표시 이름 결정
            _discoveryNoticeText.text = $"숨김 합성식 발견! {resultName}"; // 발견 알림 문구 표시
            _discoveryNoticeRemainingSeconds = DiscoveryNoticeDurationSeconds; // 일정 시간 뒤 자동으로 사라지도록 남은 시간 설정
            _discoveryNoticeText.gameObject.SetActive(true); // 알림 텍스트를 화면에 표시
        }

        private void Update() // 22일차: 숨김 합성식 발견 알림을 일정 시간 뒤 자동으로 지우기 위한 매 프레임 처리
        {
            if (_discoveryNoticeRemainingSeconds <= 0f) return; // 표시 중인 알림이 없으면 처리하지 않음

            _discoveryNoticeRemainingSeconds -= Time.unscaledDeltaTime; // 타임스케일과 무관하게 남은 표시 시간을 감소
            if (_discoveryNoticeRemainingSeconds > 0f) return; // 아직 표시 시간이 남아 있으면 유지

            _discoveryNoticeRemainingSeconds = 0f; // 남은 시간을 0으로 고정
            if (_discoveryNoticeText != null) _discoveryNoticeText.gameObject.SetActive(false); // 알림 텍스트를 숨김
        }

        private void HandleTurnChanged(TurnState state, int turnNumber) // 턴 상태가 바뀔 때 호출되는 이벤트 처리 메서드
        {
            RefreshToggleButtonInteractable(); // 배치 턴 진입/이탈에 따라 "합성" 버튼 사용 가능 여부 갱신
        }

        private void OnToggleButtonClicked() // "합성" 버튼을 눌렀을 때 호출되는 메서드
        {
            if (_boardInput == null) return; // 연결이 없으면 처리하지 않음
            bool nextActive = !_boardInput.IsFusionModeActive; // 현재 상태의 반대로 전환 시도
            _boardInput.SetFusionModeActive(nextActive); // 실제 합성 모드 전환 요청(배치 턴이 아니면 내부에서 거부됨)
            RefreshPanel(); // 전환 결과를 즉시 화면에 반영
        }

        private void OnMaterialSlotClicked(int slotIndex) // 22일차: 재료 슬롯을 눌러 그 자리의 재료 1장만 선택 해제하는 메서드
        {
            if (_boardInput == null) return; // 연결이 없으면 처리하지 않음

            var materials = _boardInput.FusionMaterials; // 현재 선택된 재료 목록 참조
            if (slotIndex < 0 || slotIndex >= materials.Count) return; // 빈 슬롯을 눌렀으면 아무 것도 하지 않음

            _boardInput.TryToggleFusionMaterial(materials[slotIndex]); // 이미 선택된 재료이므로 같은 토글 진입점이 선택 해제로 동작(손패 카드 강조도 같은 이벤트로 함께 갱신됨)
        }

        private void OnConfirmButtonClicked() // "합성" 확정 버튼을 눌렀을 때 호출되는 메서드
        {
            _boardInput?.TryConfirmFusionSelection(); // 실제 합성 실행 시도(결과 반영은 FusionSelectionChanged 이벤트로 처리)
        }

        private void RefreshToggleButtonInteractable() // 현재 배치 턴 여부에 맞춰 "합성" 버튼 사용 가능 상태를 갱신하는 메서드
        {
            if (_toggleButton == null) return; // 버튼이 아직 없으면 종료
            bool canUse = _boardInput != null && _boardInput.CanUseFusionInput; // 합성이 가능한 턴인지 확인
            _toggleButton.interactable = canUse; // 배치 턴이 아니면 버튼 자체를 비활성화
        }

        private void RefreshPanel() // 현재 합성 모드·재료·미리보기 상태를 화면에 전부 반영하는 메서드
        {
            bool active = _boardInput != null && _boardInput.IsFusionModeActive; // 현재 합성 모드 여부 확인
            if (_panelRoot != null) _panelRoot.SetActive(active); // 모드가 꺼져 있으면 패널 전체를 숨김
            if (_toggleButtonText != null) _toggleButtonText.text = active ? "합성 종료" : "합성"; // 버튼 문구를 현재 모드에 맞춰 갱신

            if (!active) return; // 패널이 숨겨진 상태면 나머지 내용은 갱신할 필요 없음

            var materials = _boardInput.FusionMaterials; // 현재 선택된 재료 목록 참조
            RefreshMaterialSlot(_materialSlotAImage, _materialSlotAText, materials.Count > 0 ? materials[0] : null); // 재료 A 슬롯 갱신
            RefreshMaterialSlot(_materialSlotBImage, _materialSlotBText, materials.Count > 1 ? materials[1] : null); // 재료 B 슬롯 갱신
            RefreshSlotRemoveAffordance(_materialSlotAButton, _materialSlotAHintText, materials.Count > 0); // 22일차: 재료 A 슬롯의 클릭 제외 가능 여부 갱신
            RefreshSlotRemoveAffordance(_materialSlotBButton, _materialSlotBHintText, materials.Count > 1); // 22일차: 재료 B 슬롯의 클릭 제외 가능 여부 갱신

            var recipe = _boardInput.CurrentFusionRecipe; // 현재 매칭된 합성 결과 레시피 확인
            if (recipe == null || recipe.Result == null) // 규칙 위반이나 조합 없음으로 결과가 없으면
            {
                RefreshMaterialSlot(_resultSlotImage, null, null); // 결과 슬롯을 빈 상태로 표시
                _resultNameText.text = FusionRuleValidator.DescribeBlockReason(_boardInput.CurrentFusionBlockReason); // 22일차: 등급 위반·수량 제한 등 구체적인 차단 사유를 그대로 안내
                _resultStatsText.text = ""; // 스탯 요약은 비움
                _resultDescriptionText.text = ""; // 설명도 비움
                _confirmButton.interactable = false; // 합성 확정 불가
                _confirmButtonText.text = "합성"; // 버튼 문구는 유지(비활성화로 안내)
                return; // 결과 표시를 끝냄
            }

            if (_boardInput.IsCurrentFusionRecipeUndiscovered) // 22일차: 아직 발견하지 못한 숨김 합성식이면
            {
                RefreshMaterialSlot(_resultSlotImage, null, null); // 결과 초상화를 가림
                _resultNameText.text = "???"; // 결과 이름을 가림
                _resultStatsText.text = "숨김 합성식"; // 숨김 합성식임을 안내
                _resultDescriptionText.text = "합성해야 결과를 확인할 수 있습니다."; // 발견 전 안내 문구
                _confirmButton.interactable = true; // 결과는 가리되 합성 자체는 가능
                _confirmButtonText.text = "합성"; // 정상 확정 문구
                return; // 결과 상세는 표시하지 않고 끝냄
            }

            RefreshMaterialSlot(_resultSlotImage, null, recipe.Result); // 결과 슬롯에 실제 결과 초상화 표시
            _resultNameText.text = string.IsNullOrEmpty(recipe.Result.DisplayName) ? recipe.Result.name : recipe.Result.DisplayName; // 결과 이름 표시
            _resultStatsText.text = BuildResultComparisonText(recipe, materials); // 22일차: 재료 대비 등급·ATK·HP 증감을 함께 표시
            _resultDescriptionText.text = recipe.Result.Description; // 결과 설명 표시
            _confirmButton.interactable = true; // 합성 확정 가능
            _confirmButtonText.text = "합성"; // 정상 확정 문구
        }

        private static string BuildResultComparisonText(FusionRecipe recipe, System.Collections.Generic.IReadOnlyList<PieceDefinition> materials) // 22일차: 결과 스탯을 재료 최고치와 비교해 증감까지 보여주는 문구를 만드는 메서드
        {
            var result = recipe.Result; // 비교 기준이 되는 합성 결과
            int resultGrade = Mathf.Max(1, (int)result.Grade); // 결과 등급(최소 1성으로 보정)

            int bestMaterialGrade = 0; // 재료 중 가장 높은 등급
            int bestMaterialAtk = 0; // 재료 중 가장 높은 공격력
            int bestMaterialHp = 0; // 재료 중 가장 높은 체력

            for (int i = 0; i < materials.Count; i++) // 선택된 재료를 순회하며
            {
                var material = materials[i]; // 이번 순회의 재료
                if (material == null) continue; // 빈 슬롯은 건너뜀

                int materialGrade = Mathf.Max(1, (int)material.Grade); // 재료 등급(최소 1성으로 보정)
                if (materialGrade > bestMaterialGrade) bestMaterialGrade = materialGrade; // 최고 등급 갱신
                if (material.BaseAtk > bestMaterialAtk) bestMaterialAtk = material.BaseAtk; // 최고 공격력 갱신
                if (material.BaseHp > bestMaterialHp) bestMaterialHp = material.BaseHp; // 최고 체력 갱신
            }

            if (bestMaterialGrade <= 0) // 비교할 재료 정보가 없으면
            {
                return $"{resultGrade}성 · ATK {result.BaseAtk} · HP {result.BaseHp}"; // 결과 스탯만 그대로 표시
            }

            string gradeText = bestMaterialGrade == resultGrade ? $"{resultGrade}성" : $"{bestMaterialGrade}성 → {resultGrade}성"; // 등급 상승을 화살표로 표기
            return $"{gradeText} · ATK {result.BaseAtk}{FormatDelta(result.BaseAtk - bestMaterialAtk)} · HP {result.BaseHp}{FormatDelta(result.BaseHp - bestMaterialHp)}"; // 등급·공격력·체력 증감을 한 줄로 표시
        }

        private static string FormatDelta(int delta) // 22일차: 재료 대비 증감을 (+2)/(-1) 형태로 표기하는 메서드
        {
            if (delta == 0) return ""; // 변화가 없으면 표기하지 않음
            return delta > 0 ? $" (+{delta})" : $" ({delta})"; // 증가는 +, 감소는 그대로 표기
        }

        private static void RefreshSlotRemoveAffordance(Button slotButton, Text hintText, bool hasMaterial) // 22일차: 재료가 들어 있는 슬롯만 클릭으로 뺄 수 있도록 버튼·안내 상태를 갱신하는 메서드
        {
            if (slotButton != null) slotButton.interactable = hasMaterial; // 빈 슬롯은 클릭해도 반응하지 않도록 비활성화
            if (hintText != null) hintText.gameObject.SetActive(hasMaterial); // 재료가 있을 때만 "클릭하여 제외" 안내 표시
        }

        private void RefreshMaterialSlot(Image slotImage, Text slotText, PieceDefinition definition) // 재료·결과 슬롯 하나의 초상화와 이름을 갱신하는 공통 메서드
        {
            if (slotImage != null) // 슬롯 이미지가 존재하면
            {
                bool hasArtwork = definition != null && definition.CardArtwork != null; // 실제 Artwork 연결 여부 확인
                slotImage.enabled = hasArtwork; // Artwork가 있을 때만 이미지 표시
                slotImage.sprite = hasArtwork ? definition.CardArtwork : null; // Artwork 적용(없으면 비움)
            }

            if (slotText != null) // 슬롯 텍스트가 존재하면
            {
                slotText.text = definition != null // 카드가 있으면 이름 또는 약칭 표시
                    ? (string.IsNullOrEmpty(definition.DisplayName) ? definition.name : definition.DisplayName)
                    : "빈 슬롯"; // 카드가 없으면 빈 슬롯 안내
            }
        }

        private void EnsureUI() // 버튼·패널·Canvas를 한 번만 만드는 메서드
        {
            if (_canvas != null) return; // 이미 생성됐으면 중복 생성하지 않음
            EnsureEventSystem(); // 버튼 클릭 입력을 처리할 EventSystem 보장

            var canvasObject = new GameObject("FusionPanelCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 전용 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // BattleController 또는 테스트 호스트의 자식으로 연결
            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 확보
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 위에 직접 표시
            _canvas.sortingOrder = 96; // 손패(90)·덱 패널(95)보다 위, 턴 상태(100)보다 아래에 표시

            var scaler = canvasObject.GetComponent<CanvasScaler>(); // 해상도 대응용 CanvasScaler 확보
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기에 맞춰 UI 보정
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 다양한 화면 비율 대응
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 보정

            BuildToggleButton(canvasObject.transform); // 하단 중앙 "합성" 버튼 생성
            BuildPanel(canvasObject.transform); // 재료·결과 미리보기 패널(초기 비활성) 생성
        }

        private void BuildToggleButton(Transform parent) // 화면 하단 중앙, 손패 위쪽에 배치되는 "합성" 버튼을 만드는 메서드
        {
            var buttonObject = new GameObject("FusionToggleButton", typeof(RectTransform), typeof(Image), typeof(Button)); // 버튼 GameObject 생성
            buttonObject.transform.SetParent(parent, false); // Canvas 자식으로 연결
            var rect = buttonObject.GetComponent<RectTransform>(); // RectTransform 확보
            rect.anchorMin = new Vector2(1f, 0f); // 32일차: 우하단 앵커로 변경(죽은 카드 버튼과 같은 기준)
            rect.anchorMax = new Vector2(1f, 0f); // 우하단 앵커
            rect.pivot = new Vector2(0.5f, 0f); // 죽은 카드 버튼 중심에 맞춰 가로 중앙 정렬되도록 중앙 피벗 사용
            rect.anchoredPosition = new Vector2(-99f, 106f); // 32일차: 죽은 카드 버튼(우하단, 폭 150, 중심 -99) 바로 위에 배치
            rect.sizeDelta = new Vector2(225f, 56f); // 32일차: 기존 150 대비 가로 1.5배로 확장

            var image = buttonObject.GetComponent<Image>(); // 버튼 배경 Image 확보
            image.color = new Color(0.5f, 0.32f, 0.12f, 0.92f); // 합성을 연상시키는 청동색 배경 적용

            _toggleButton = buttonObject.GetComponent<Button>(); // Button 컴포넌트 확보
            _toggleButton.onClick.AddListener(OnToggleButtonClicked); // 클릭 시 합성 모드 토글 실행

            _toggleButtonText = CreateText("FusionToggleText", buttonObject.transform, 17, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white); // 버튼 문구 텍스트 생성
            Stretch(_toggleButtonText.rectTransform, 4f, 4f, 4f, 4f); // 버튼 내부 여백 적용
            _toggleButtonText.text = "합성"; // 기본 문구
        }

        private void BuildPanel(Transform parent) // 재료 2장·결과 미리보기·확정 버튼을 담는 패널을 만드는 메서드
        {
            var body = CreatePanel("FusionPanelBody", parent, new Color(0.09f, 0.1f, 0.13f, 0.96f)); // 패널 배경 생성
            _panelRoot = body.gameObject; // 패널 전체 루트로 저장(토글에 따라 켜고 끔)
            SetRect(body.rect, new Vector2(0.5f, 0f), new Vector2(0f, 366f), new Vector2(780f, 240f)); // 합성 버튼 위쪽, 화면 하단 중앙에 고정 배치
            body.image.raycastTarget = true; // 패널 내부 클릭이 아래 보드로 전달되지 않도록 자체적으로 Raycast 소비
            var bodyBlocker = body.gameObject.AddComponent<Button>(); // 패널 배경 클릭이 카드/보드 쪽으로 새지 않도록 빈 Button으로 이벤트 소비
            bodyBlocker.transition = Selectable.Transition.None; // 시각적 변화 없이 클릭만 차단하는 용도
            AddOutline(body.gameObject, new Color(0.55f, 0.42f, 0.22f, 0.9f), new Vector2(2f, -2f)); // 청동색 외곽선으로 손패 카드 프레임과 톤 맞춤

            var materialA = BuildSlot(body.rect, new Vector2(-290f, 30f), out _materialSlotAImage, out _materialSlotAText, out _materialSlotAButton, out _materialSlotAHintText); // 재료 A 슬롯 생성
            _materialSlotAButton.onClick.AddListener(() => OnMaterialSlotClicked(0)); // 22일차: A 슬롯 클릭 시 첫 번째 재료만 선택 해제
            CreateText("PlusSign", body.rect, 26, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white).rectTransform.anchoredPosition = new Vector2(-190f, 30f); // 재료 A/B 사이 "+" 표시
            var plusText = materialA.transform.parent.Find("PlusSign")?.GetComponent<Text>(); // 방금 생성한 "+" 텍스트 재조회(위치만 지정했으므로 문구를 직접 채움)
            if (plusText != null) plusText.text = "+"; // "+" 문구 적용

            var materialB = BuildSlot(body.rect, new Vector2(-90f, 30f), out _materialSlotBImage, out _materialSlotBText, out _materialSlotBButton, out _materialSlotBHintText); // 재료 B 슬롯 생성
            _materialSlotBButton.onClick.AddListener(() => OnMaterialSlotClicked(1)); // 22일차: B 슬롯 클릭 시 두 번째 재료만 선택 해제
            var arrowText = CreateText("ArrowSign", body.rect, 26, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white); // 재료→결과 화살표 텍스트 생성
            arrowText.rectTransform.anchoredPosition = new Vector2(10f, 30f); // 재료 B와 결과 슬롯 사이에 배치
            arrowText.text = "→"; // 화살표 문구 적용

            BuildResultSlot(body.rect, new Vector2(190f, 20f)); // 결과 미리보기 슬롯(이름·스탯·설명 포함) 생성

            var confirmObject = new GameObject("FusionConfirmButton", typeof(RectTransform), typeof(Image), typeof(Button)); // 합성 확정 버튼 생성
            confirmObject.transform.SetParent(body.rect, false); // 패널 자식으로 연결
            SetRect(confirmObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(-190f, -78f), new Vector2(130f, 44f)); // 재료 슬롯 아래쪽에 배치
            confirmObject.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.22f, 1f); // 확정 버튼 배경(초록 계열)
            _confirmButton = confirmObject.GetComponent<Button>(); // Button 컴포넌트 확보
            _confirmButton.onClick.AddListener(OnConfirmButtonClicked); // 클릭 시 실제 합성 확정 실행
            _confirmButtonText = CreateText("FusionConfirmText", confirmObject.transform, 16, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white); // 확정 버튼 문구 텍스트 생성
            Stretch(_confirmButtonText.rectTransform, 2f, 2f, 2f, 2f); // 버튼 내부 여백 적용
            _confirmButtonText.text = "합성"; // 기본 문구

            var cancelObject = new GameObject("FusionCancelButton", typeof(RectTransform), typeof(Image), typeof(Button)); // 합성 모드 취소 버튼 생성
            cancelObject.transform.SetParent(body.rect, false); // 패널 자식으로 연결
            SetRect(cancelObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(-40f, -78f), new Vector2(100f, 44f)); // 확정 버튼 옆에 배치

            _discoveryNoticeText = CreateText("DiscoveryNotice", body.rect, 18, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.35f, 1f)); // 22일차: 숨김 합성식 발견 알림 텍스트 생성
            SetRect(_discoveryNoticeText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 26f), new Vector2(760f, 30f)); // 패널 위쪽에 한 줄로 배치
            _discoveryNoticeText.gameObject.SetActive(false); // 발견 전에는 숨겨둠
            cancelObject.GetComponent<Image>().color = new Color(0.4f, 0.15f, 0.15f, 1f); // 취소 버튼 배경(붉은 계열)
            var cancelButton = cancelObject.GetComponent<Button>(); // Button 컴포넌트 확보
            cancelButton.onClick.AddListener(() => _boardInput?.SetFusionModeActive(false)); // 클릭 시 합성 모드 전체 종료
            var cancelText = CreateText("FusionCancelText", cancelObject.transform, 16, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white); // 취소 버튼 문구 텍스트 생성
            Stretch(cancelText.rectTransform, 2f, 2f, 2f, 2f); // 버튼 내부 여백 적용
            cancelText.text = "취소"; // 취소 문구

            body.gameObject.SetActive(false); // 처음에는 합성 모드가 꺼져 있으므로 패널을 숨긴 채 시작
        }

        private RectTransform BuildSlot(Transform parent, Vector2 position, out Image slotImage, out Text slotText, out Button slotButton, out Text slotHintText) // 재료 슬롯 1개(초상화 배경 + 이름 텍스트 + 클릭 제외 버튼)를 만드는 공통 메서드
        {
            var slot = CreatePanel("MaterialSlot", parent, new Color(0.18f, 0.2f, 0.25f, 1f)); // 슬롯 배경 패널 생성
            SetRect(slot.rect, new Vector2(0.5f, 0.5f), position, new Vector2(150f, 120f)); // 지정한 위치·크기로 배치
            AddOutline(slot.gameObject, new Color(0.5f, 0.5f, 0.55f, 0.8f), new Vector2(1.5f, -1.5f)); // 옅은 외곽선으로 슬롯 경계 표시

            slot.image.raycastTarget = true; // 22일차: 슬롯 배경이 클릭을 직접 받도록 Raycast 대상 유지
            slotButton = slot.gameObject.AddComponent<Button>(); // 22일차: 슬롯 자체를 눌러 해당 재료만 빼는 버튼으로 사용
            slotButton.targetGraphic = slot.image; // 슬롯 배경 이미지에 색상 전환 적용
            var slotColors = slotButton.colors; // 기본 색상 전환 설정 복사
            slotColors.normalColor = Color.white; // 평상시에는 배경 색을 그대로 사용
            slotColors.highlightedColor = new Color(1f, 0.92f, 0.72f, 1f); // 마우스를 올리면 "뺄 수 있다"는 뜻으로 밝게 표시
            slotColors.pressedColor = new Color(0.85f, 0.7f, 0.45f, 1f); // 누르는 동안 더 진하게 표시
            slotColors.disabledColor = Color.white; // 빈 슬롯은 색 변화 없이 그대로 표시
            slotButton.colors = slotColors; // 변경한 색상 전환 설정 적용
            slotButton.interactable = false; // 재료가 들어오기 전까지는 클릭 반응 없음

            var artObject = new GameObject("SlotArt", typeof(RectTransform), typeof(Image)); // 슬롯 초상화 이미지 생성
            artObject.transform.SetParent(slot.rect, false); // 슬롯 배경 자식으로 연결
            slotImage = artObject.GetComponent<Image>(); // 초상화 Image 참조 저장
            slotImage.raycastTarget = false; // 미리보기 슬롯은 클릭 대상이 아니므로 Raycast 비활성화
            SetRect(slotImage.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(90f, 70f)); // 슬롯 상단에 정사각형에 가까운 초상화 영역 배치
            slotImage.enabled = false; // Artwork가 없을 때는 숨김 상태로 시작

            slotText = CreateText("SlotName", slot.rect, 13, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white); // 슬롯 이름 텍스트 생성
            SetRect(slotText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(140f, 30f)); // 슬롯 하단에 배치(안내 문구 자리를 위해 살짝 올림)
            slotText.text = "빈 슬롯"; // 기본 문구
            slotText.raycastTarget = false; // 22일차: 이름 텍스트가 슬롯 클릭을 가로채지 않도록 Raycast 비활성화

            slotHintText = CreateText("SlotRemoveHint", slot.rect, 10, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.8f, 0.78f, 0.7f, 1f)); // 22일차: 클릭 제외 안내 텍스트 생성
            SetRect(slotHintText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 7f), new Vector2(140f, 16f)); // 이름 텍스트 아래에 배치
            slotHintText.text = "클릭하여 제외"; // 안내 문구
            slotHintText.raycastTarget = false; // 안내 텍스트도 슬롯 클릭을 가로채지 않도록 Raycast 비활성화
            slotHintText.gameObject.SetActive(false); // 재료가 들어오기 전까지는 숨김

            return slot.rect; // 슬롯 RectTransform 반환(위치 재사용 용도)
        }

        private void BuildResultSlot(Transform parent, Vector2 position) // 결과 미리보기 슬롯(초상화 + 이름 + 스탯 + 설명)을 만드는 메서드
        {
            var slot = CreatePanel("ResultSlot", parent, new Color(0.2f, 0.18f, 0.1f, 1f)); // 결과 슬롯은 재료 슬롯과 다른 색으로 구분
            SetRect(slot.rect, new Vector2(0.5f, 0.5f), position, new Vector2(220f, 190f)); // 재료 슬롯보다 크게 배치
            AddOutline(slot.gameObject, new Color(0.75f, 0.6f, 0.3f, 0.9f), new Vector2(2f, -2f)); // 금색 계열 외곽선으로 결과임을 강조

            var artObject = new GameObject("ResultArt", typeof(RectTransform), typeof(Image)); // 결과 초상화 이미지 생성
            artObject.transform.SetParent(slot.rect, false); // 결과 슬롯 자식으로 연결
            _resultSlotImage = artObject.GetComponent<Image>(); // 초상화 Image 참조 저장
            _resultSlotImage.raycastTarget = false; // 미리보기 슬롯은 클릭 대상이 아니므로 Raycast 비활성화
            SetRect(_resultSlotImage.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(80f, 62f)); // 슬롯 상단에 초상화 영역 배치
            _resultSlotImage.enabled = false; // Artwork가 없을 때는 숨김 상태로 시작

            _resultNameText = CreateText("ResultName", slot.rect, 15, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white); // 결과 이름 텍스트 생성
            SetRect(_resultNameText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -74f), new Vector2(210f, 24f)); // 초상화 아래 배치
            _resultNameText.text = "재료를 선택하세요"; // 기본 안내 문구

            _resultStatsText = CreateText("ResultStats", slot.rect, 12, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.9f, 0.85f, 0.6f, 1f)); // 등급·ATK·HP 요약 텍스트 생성
            SetRect(_resultStatsText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -98f), new Vector2(210f, 20f)); // 이름 아래 배치

            _resultDescriptionText = CreateText("ResultDescription", slot.rect, 11, FontStyle.Normal, TextAnchor.UpperCenter, new Color(0.85f, 0.85f, 0.85f, 1f)); // 결과 설명 텍스트 생성
            SetRect(_resultDescriptionText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(200f, 60f)); // 슬롯 하단에 여러 줄 설명 배치
        }

        private void EnsureEventSystem() // 버튼 클릭 입력을 처리할 EventSystem을 보장하는 메서드
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return; // 기존 EventSystem이 있으면 그대로 사용(HandUI·DeckPanelUI 등과 공유)
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 새 Input System용 EventSystem 생성
            _createdEventSystem = eventSystemObject.GetComponent<EventSystem>(); // 이 컴포넌트가 만든 EventSystem 참조 저장
        }

        private static (GameObject gameObject, RectTransform rect, Image image) CreatePanel(string name, Transform parent, Color color) // 단색 UI 패널 생성 보조 메서드
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image)); // 패널 GameObject 생성
            panel.transform.SetParent(parent, false); // 부모에 연결
            var rect = panel.GetComponent<RectTransform>(); // RectTransform 확보
            var image = panel.GetComponent<Image>(); // Image 확보
            image.color = color; // 배경색 적용
            image.raycastTarget = false; // 기본값은 입력을 받지 않음(필요한 곳에서 개별적으로 켬)
            return (panel, rect, image); // 구성 요소 반환
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle style, TextAnchor alignment, Color color) // 공통 런타임 Text 생성 보조 메서드
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // UI Text GameObject 생성
            textObject.transform.SetParent(parent, false); // 부모에 연결
            var text = textObject.GetComponent<Text>(); // Text 확보
            text.font = GetRuntimeFont(); // 한글 시스템 폰트 적용
            text.fontSize = fontSize; // 글자 크기 적용
            text.fontStyle = style; // 글자 스타일 적용
            text.alignment = alignment; // 정렬 적용
            text.color = color; // 글자색 적용
            text.horizontalOverflow = HorizontalWrapMode.Wrap; // 폭을 넘으면 줄바꿈
            text.verticalOverflow = VerticalWrapMode.Truncate; // 영역을 넘으면 잘라 표시
            text.raycastTarget = false; // 텍스트는 입력을 받지 않음
            return text; // 구성된 Text 반환
        }

        private static void AddOutline(GameObject target, Color color, Vector2 distance) // Image/Text에 간단한 외곽선 효과를 추가하는 보조 메서드
        {
            var outline = target.AddComponent<Outline>(); // Outline 추가
            outline.effectColor = color; // 외곽선 색 적용
            outline.effectDistance = distance; // 외곽선 두께 적용
            outline.useGraphicAlpha = true; // 원본 알파 반영
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size) // 고정 크기 UI 요소 위치 설정 보조 메서드
        {
            rect.anchorMin = anchor; // 최소 앵커 설정
            rect.anchorMax = anchor; // 최대 앵커 동일 설정
            rect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 사용
            rect.anchoredPosition = position; // 앵커 기준 위치 적용
            rect.sizeDelta = size; // 고정 크기 적용
        }

        private static void Stretch(RectTransform rect, float left, float bottom, float right, float top) // 부모 영역 안쪽으로 Stretch하는 보조 메서드
        {
            rect.anchorMin = Vector2.zero; // 좌하단 앵커
            rect.anchorMax = Vector2.one; // 우상단 앵커
            rect.offsetMin = new Vector2(left, bottom); // 좌·하단 여백 적용
            rect.offsetMax = new Vector2(-right, -top); // 우·상단 여백 적용
        }

        private static Font GetRuntimeFont() // 한글 텍스트를 표시할 런타임 폰트를 만드는 메서드
        {
            if (_runtimeFont != null) return _runtimeFont; // 이미 생성됐으면 캐시 재사용
            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 24); // 한글 시스템 폰트 우선 생성
            if (_runtimeFont == null) _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 실패 시 Unity 기본 폰트 사용
            return _runtimeFont; // 최종 폰트 반환
        }

        private void OnDestroy() // 컴포넌트가 파괴될 때 이벤트와 직접 생성한 EventSystem을 정리하는 메서드
        {
            if (_boardInput != null) // 입력 컨트롤러가 연결돼 있으면
            {
                _boardInput.FusionSelectionChanged -= HandleFusionSelectionChanged; // 합성 선택 이벤트 구독 해제
                _boardInput.HiddenRecipeDiscovered -= HandleHiddenRecipeDiscovered; // 22일차: 숨김 레시피 발견 이벤트 구독 해제
                if (_boardInput.TurnManager != null) _boardInput.TurnManager.TurnChanged -= HandleTurnChanged; // 턴 이벤트 구독 해제
            }

            if (_createdEventSystem != null) // 이 컴포넌트가 만든 EventSystem이 남아 있으면
            {
                if (Application.isPlaying) Destroy(_createdEventSystem.gameObject); // Play Mode에서는 안전하게 제거
                else DestroyImmediate(_createdEventSystem.gameObject); // EditMode에서는 즉시 제거
            }
        }
    }
}
