using System; // Action 사용
using System.Collections.Generic; // List<T>·Dictionary<T> 사용
using UnityEngine; // MonoBehaviour·GameObject·Color·Vector2 사용
using UnityEngine.UI; // Button·Image·Text 사용

namespace ProjectEta.Meta
{
    public sealed class MetaProgressPanelController : MonoBehaviour
    {
        private static readonly Color PanelColor = new Color(0.055f, 0.065f, 0.085f, 0.99f); // 영구 성장 패널 배경
        private static readonly Color SecondaryColor = new Color(0.035f, 0.043f, 0.058f, 0.98f); // 보조 영역 배경
        private static readonly Color AccentColor = new Color(0.38f, 0.68f, 0.88f, 1f); // 해금 가능 강조
        private static readonly Color UnlockedColor = new Color(0.22f, 0.52f, 0.36f, 1f); // 해금 완료 강조
        private static readonly Color WarningColor = new Color(0.68f, 0.46f, 0.2f, 1f); // 토큰 부족 강조
        private static readonly Color SoftTextColor = new Color(0.68f, 0.72f, 0.79f, 1f); // 보조 텍스트
        private static Font _runtimeFont; // 한글 런타임 폰트 캐시

        private readonly MetaProgressPanelState _panelState = new MetaProgressPanelState(); // 영구 성장 화면 상태
        private readonly Dictionary<MetaProgressCategory, Button> _categoryButtons = new Dictionary<MetaProgressCategory, Button>(); // 카테고리 버튼 목록
        private readonly List<GameObject> _itemObjects = new List<GameObject>(); // 동적 해금 카드 오브젝트 목록
        private Action _closeRequested; // 외부 닫기 콜백
        private MetaProgressState _progress; // 현재 영구 진행 상태
        private Transform _itemRoot; // 해금 카드 목록 부모
        private Text _tokenText; // 보유 메타 토큰 문구
        private Text _summaryText; // 영구 해금 요약 문구
        private Text _detailTypeText; // 상세 타입 문구
        private Text _detailTitleText; // 상세 이름 문구
        private Text _detailDescriptionText; // 상세 설명 문구
        private Text _detailCostText; // 상세 비용 문구
        private Text _detailStatusText; // 상세 상태 문구
        private Text _feedbackText; // 해금 결과 안내
        private Button _unlockButton; // 상세 해금 버튼
        private Text _unlockButtonText; // 상세 해금 버튼 문구
        private GameObject _confirmRoot; // 해금 확인 팝업 루트
        private Text _confirmTitleText; // 해금 확인 제목
        private Text _confirmBodyText; // 해금 확인 본문
        private Button _confirmButton; // 해금 확인 버튼
        private MetaUnlockDefinition _pendingUnlock; // 현재 확인 대기 해금 정의
        private bool _initialized; // 패널 초기화 완료 여부

        public void Initialize(Action closeRequested)
        {
            if (_initialized) return; // 중복 UI 생성 차단

            _closeRequested = closeRequested; // 외부 닫기 콜백 저장
            BuildUI(); // 영구 성장 정식 UI 생성
            _initialized = true; // 초기화 완료 기록

            if (gameObject.activeInHierarchy) RefreshPanel(); // 활성 상태 초기화 시 즉시 표시 갱신
        }

        private void OnEnable()
        {
            if (!_initialized) return; // Initialize 전 활성 이벤트 차단
            RefreshPanel(); // 패널 진입 시 저장 상태 재조회
        }

        public void RefreshPanel()
        {
            _progress = MetaProgressService.Current; // 최신 영구 진행 상태 조회
            RefreshHeader(); // 토큰·해금 요약 갱신
            RefreshCategoryButtons(); // 카테고리 선택 상태 갱신
            RebuildItems(); // 현재 카테고리 카드 목록 재구성
            RefreshDetails(); // 선택 상세 정보 갱신
        }

        private void BuildUI()
        {
            GameObject backdrop = CreateImage("MetaBackdrop_Day58", transform, Vector2.zero, new Vector2(1920f, 1080f), new Color(0f, 0f, 0f, 0.72f)); // 전체 화면 입력 차단 배경 생성
            Stretch(backdrop.GetComponent<RectTransform>(), 0f); // 전체 화면 배경 배치

            GameObject panel = CreateImage("MetaProgressPanel_Day58", transform, Vector2.zero, new Vector2(1420f, 920f), PanelColor); // 영구 성장 중앙 패널 생성

            Text eyebrow = CreateText("Eyebrow", panel.transform, 18, FontStyle.Bold, TextAnchor.MiddleLeft); // 영구 성장 분류 문구 생성
            eyebrow.text = "PERMANENT PROGRESSION"; // 분류 문구 적용
            eyebrow.color = AccentColor; // 강조 색상 적용
            SetRect(eyebrow.rectTransform, new Vector2(-575f, 390f), new Vector2(230f, 36f)); // 분류 문구 배치

            Text title = CreateText("Title", panel.transform, 44, FontStyle.Bold, TextAnchor.MiddleLeft); // 영구 성장 제목 생성
            title.text = "영구 성장"; // 영구 성장 제목 적용
            SetRect(title.rectTransform, new Vector2(-520f, 338f), new Vector2(340f, 60f)); // 영구 성장 제목 배치

            _tokenText = CreateText("Token", panel.transform, 28, FontStyle.Bold, TextAnchor.MiddleRight); // 메타 토큰 잔액 문구 생성
            _tokenText.color = AccentColor; // 토큰 강조 색상 적용
            SetRect(_tokenText.rectTransform, new Vector2(475f, 355f), new Vector2(360f, 50f)); // 토큰 문구 배치

            _summaryText = CreateText("Summary", panel.transform, 17, FontStyle.Normal, TextAnchor.MiddleRight); // 해금 수 요약 문구 생성
            _summaryText.color = SoftTextColor; // 요약 보조 색상 적용
            SetRect(_summaryText.rectTransform, new Vector2(470f, 315f), new Vector2(460f, 40f)); // 요약 문구 배치

            GameObject navPanel = CreateImage("CategoryNavigation", panel.transform, new Vector2(-555f, -25f), new Vector2(250f, 650f), SecondaryColor); // 카테고리 내비게이션 영역 생성
            BuildCategoryButtons(navPanel.transform); // 카테고리 버튼 생성

            GameObject listPanel = CreateImage("UnlockListPanel", panel.transform, new Vector2(-145f, -25f), new Vector2(530f, 650f), SecondaryColor); // 해금 카드 목록 영역 생성
            Text listTitle = CreateText("ListTitle", listPanel.transform, 22, FontStyle.Bold, TextAnchor.MiddleLeft); // 목록 제목 생성
            listTitle.text = "해금 목록"; // 목록 제목 적용
            SetRect(listTitle.rectTransform, new Vector2(0f, 275f), new Vector2(440f, 44f)); // 목록 제목 배치

            GameObject itemRootObject = new GameObject("UnlockItems", typeof(RectTransform)); // 동적 해금 카드 부모 생성
            itemRootObject.transform.SetParent(listPanel.transform, false); // 목록 영역 자식 연결
            SetRect(itemRootObject.GetComponent<RectTransform>(), new Vector2(0f, -20f), new Vector2(460f, 520f)); // 카드 목록 영역 배치
            _itemRoot = itemRootObject.transform; // 카드 목록 부모 저장

            GameObject detailPanel = CreateImage("UnlockDetailPanel", panel.transform, new Vector2(390f, -25f), new Vector2(500f, 650f), SecondaryColor); // 선택 해금 상세 영역 생성
            BuildDetailPanel(detailPanel.transform); // 상세 정보 UI 생성

            _feedbackText = CreateText("Feedback", panel.transform, 17, FontStyle.Normal, TextAnchor.MiddleLeft); // 하단 결과 안내 생성
            _feedbackText.color = SoftTextColor; // 결과 안내 보조 색상 적용
            SetRect(_feedbackText.rectTransform, new Vector2(75f, -395f), new Vector2(900f, 36f)); // 결과 안내 배치

            Button closeButton = CreateButton("Close", panel.transform, "뒤로", new Vector2(-555f, -395f), new Vector2(250f, 64f)); // MainMenu 복귀 버튼 생성
            closeButton.onClick.AddListener(HandleClose); // 외부 닫기 콜백 연결

            BuildConfirmModal(panel.transform); // 영구 해금 확인 팝업 생성
        }

        private void BuildCategoryButtons(Transform parent)
        {
            MetaProgressCategory[] categories =
            {
                MetaProgressCategory.All, // 전체 카테고리
                MetaProgressCategory.Piece, // 기물 카테고리
                MetaProgressCategory.King, // 킹 카테고리
                MetaProgressCategory.Passive // 패시브 카테고리
            };

            for (int i = 0; i < categories.Length; i++)
            {
                MetaProgressCategory category = categories[i]; // 현재 카테고리 조회
                Button button = CreateButton($"Category_{category}", parent, GetCategoryLabel(category), new Vector2(0f, 220f - i * 112f), new Vector2(200f, 78f)); // 카테고리 버튼 생성
                MetaProgressCategory captured = category; // 버튼 콜백 카테고리 고정
                button.onClick.AddListener(() => HandleCategory(captured)); // 카테고리 전환 연결
                _categoryButtons[category] = button; // 카테고리 버튼 참조 저장
            }
        }

        private void BuildDetailPanel(Transform parent)
        {
            _detailTypeText = CreateText("Type", parent, 16, FontStyle.Bold, TextAnchor.MiddleLeft); // 상세 타입 문구 생성
            _detailTypeText.color = AccentColor; // 상세 타입 강조 색상 적용
            SetRect(_detailTypeText.rectTransform, new Vector2(0f, 270f), new Vector2(410f, 34f)); // 상세 타입 문구 배치

            _detailTitleText = CreateText("Name", parent, 32, FontStyle.Bold, TextAnchor.MiddleLeft); // 상세 이름 문구 생성
            SetRect(_detailTitleText.rectTransform, new Vector2(0f, 220f), new Vector2(410f, 58f)); // 상세 이름 문구 배치

            _detailDescriptionText = CreateText("Description", parent, 19, FontStyle.Normal, TextAnchor.UpperLeft); // 상세 설명 문구 생성
            _detailDescriptionText.color = SoftTextColor; // 상세 설명 보조 색상 적용
            _detailDescriptionText.horizontalOverflow = HorizontalWrapMode.Wrap; // 상세 설명 줄바꿈 허용
            _detailDescriptionText.verticalOverflow = VerticalWrapMode.Overflow; // 상세 설명 세로 표시 허용
            _detailDescriptionText.lineSpacing = 1.2f; // 상세 설명 줄 간격 적용
            SetRect(_detailDescriptionText.rectTransform, new Vector2(0f, 95f), new Vector2(410f, 180f)); // 상세 설명 배치

            _detailCostText = CreateText("Cost", parent, 23, FontStyle.Bold, TextAnchor.MiddleLeft); // 상세 비용 문구 생성
            SetRect(_detailCostText.rectTransform, new Vector2(0f, -55f), new Vector2(410f, 48f)); // 상세 비용 배치

            _detailStatusText = CreateText("Status", parent, 19, FontStyle.Bold, TextAnchor.MiddleLeft); // 상세 상태 문구 생성
            SetRect(_detailStatusText.rectTransform, new Vector2(0f, -105f), new Vector2(410f, 46f)); // 상세 상태 배치

            _unlockButton = CreateButton("Unlock", parent, "해금하기", new Vector2(0f, -235f), new Vector2(410f, 78f)); // 상세 해금 버튼 생성
            _unlockButtonText = _unlockButton.GetComponentInChildren<Text>(true); // 상세 해금 버튼 문구 참조 저장
            _unlockButton.onClick.AddListener(RequestUnlock); // 해금 확인 팝업 요청 연결
        }

        private void BuildConfirmModal(Transform parent)
        {
            _confirmRoot = CreateImage("UnlockConfirmRoot", parent, Vector2.zero, new Vector2(1420f, 920f), new Color(0f, 0f, 0f, 0.78f)); // 해금 확인 전체 오버레이 생성

            GameObject modal = CreateImage("UnlockConfirmPanel", _confirmRoot.transform, Vector2.zero, new Vector2(700f, 410f), new Color(0.07f, 0.08f, 0.105f, 1f)); // 해금 확인 중앙 패널 생성

            _confirmTitleText = CreateText("Title", modal.transform, 31, FontStyle.Bold, TextAnchor.MiddleLeft); // 확인 제목 생성
            SetRect(_confirmTitleText.rectTransform, new Vector2(0f, 120f), new Vector2(560f, 56f)); // 확인 제목 배치

            _confirmBodyText = CreateText("Body", modal.transform, 20, FontStyle.Normal, TextAnchor.UpperLeft); // 확인 본문 생성
            _confirmBodyText.color = SoftTextColor; // 확인 본문 보조 색상 적용
            _confirmBodyText.lineSpacing = 1.2f; // 확인 본문 줄 간격 적용
            SetRect(_confirmBodyText.rectTransform, new Vector2(0f, 20f), new Vector2(560f, 140f)); // 확인 본문 배치

            Button cancel = CreateButton("Cancel", modal.transform, "취소", new Vector2(-150f, -125f), new Vector2(240f, 64f)); // 확인 취소 버튼 생성
            cancel.onClick.AddListener(CancelUnlock); // 확인 취소 연결

            _confirmButton = CreateButton("Confirm", modal.transform, "해금", new Vector2(150f, -125f), new Vector2(240f, 64f)); // 확인 확정 버튼 생성
            _confirmButton.onClick.AddListener(ConfirmUnlock); // 실제 영구 해금 연결
            _confirmRoot.SetActive(false); // 기본 확인 팝업 숨김
        }

        private void HandleCategory(MetaProgressCategory category)
        {
            _panelState.ShowCategory(category); // 현재 영구 성장 카테고리 변경
            _feedbackText.text = string.Empty; // 이전 결과 안내 정리
            RefreshCategoryButtons(); // 카테고리 강조 상태 갱신
            RebuildItems(); // 선택 카테고리 해금 카드 재구성
            RefreshDetails(); // 선택 상세 정보 갱신
        }

        private void RebuildItems()
        {
            for (int i = 0; i < _itemObjects.Count; i++)
            {
                if (_itemObjects[i] != null) Destroy(_itemObjects[i]); // 이전 동적 카드 제거
            }

            _itemObjects.Clear(); // 이전 카드 참조 정리

            IReadOnlyList<MetaUnlockDefinition> definitions = _panelState.GetFilteredDefinitions(); // 현재 카테고리 해금 목록 조회
            MetaUnlockDefinition selected = _panelState.GetSelectedDefinition(); // 현재 선택 정의 조회

            if (selected == null && definitions.Count > 0)
            {
                _panelState.Select(definitions[0]); // 카테고리 첫 항목 기본 선택
            }

            for (int i = 0; i < definitions.Count; i++)
            {
                MetaUnlockDefinition definition = definitions[i]; // 현재 해금 카드 정의 조회
                int row = i; // 세로 카드 행 계산
                Button button = CreateUnlockCard(definition, new Vector2(0f, 205f - row * 100f)); // 동적 해금 카드 생성
                MetaUnlockDefinition captured = definition; // 카드 선택 콜백 정의 고정
                button.onClick.AddListener(() => HandleSelect(captured)); // 상세 선택 연결
                _itemObjects.Add(button.gameObject); // 동적 카드 참조 저장
            }
        }

        private Button CreateUnlockCard(MetaUnlockDefinition definition, Vector2 position)
        {
            MetaUnlockDisplayState state = MetaProgressPanelState.Evaluate(_progress, definition); // 현재 해금 표시 상태 계산
            Button button = CreateButton($"Unlock_{definition.UnlockId}", _itemRoot, string.Empty, position, new Vector2(440f, 82f)); // 해금 카드 버튼 생성
            Image image = button.GetComponent<Image>(); // 해금 카드 배경 조회
            image.color = GetStateColor(state); // 해금 상태 배경 색상 적용

            Text label = button.GetComponentInChildren<Text>(true); // 해금 카드 문구 조회
            if (label != null)
            {
                label.alignment = TextAnchor.MiddleLeft; // 카드 문구 좌측 정렬 적용
                label.fontSize = 19; // 카드 문구 크기 적용
                label.text = $"{definition.DisplayName}\n{GetStateLabel(state)} · {definition.Cost} Token"; // 카드 이름·상태·비용 표시
            }

            return button; // 완성 해금 카드 반환
        }

        private void HandleSelect(MetaUnlockDefinition definition)
        {
            _panelState.Select(definition); // 현재 상세 선택 정의 변경
            _feedbackText.text = string.Empty; // 이전 결과 안내 정리
            RebuildItems(); // 선택 상태 카드 강조 갱신
            RefreshDetails(); // 선택 상세 정보 갱신
        }

        private void RefreshHeader()
        {
            if (_progress == null) return; // 영구 진행 상태 누락 방어
            _tokenText.text = $"META TOKEN  {_progress.MetaTokens}"; // 현재 메타 토큰 잔액 표시
            _summaryText.text = $"기물 {_progress.UnlockedPieceIds.Count} · 킹 {_progress.UnlockedKingIds.Count} · 패시브 {_progress.UnlockedPassiveIds.Count}"; // 타입별 해금 수 표시
        }

        private void RefreshCategoryButtons()
        {
            foreach (KeyValuePair<MetaProgressCategory, Button> pair in _categoryButtons)
            {
                if (pair.Value == null) continue; // 누락 카테고리 버튼 제외
                Image image = pair.Value.GetComponent<Image>(); // 카테고리 버튼 배경 조회
                if (image == null) continue; // 카테고리 배경 누락 제외
                image.color = pair.Key == _panelState.CurrentCategory ? AccentColor : new Color(0.13f, 0.17f, 0.23f, 1f); // 현재 카테고리 강조 적용
            }
        }

        private void RefreshDetails()
        {
            MetaUnlockDefinition definition = _panelState.GetSelectedDefinition(); // 현재 선택 해금 정의 조회

            if (definition == null)
            {
                _detailTypeText.text = "NO SELECTION"; // 선택 없음 타입 표시
                _detailTitleText.text = "해금 항목 없음"; // 선택 없음 제목 표시
                _detailDescriptionText.text = "현재 카테고리에 표시할 영구 해금 항목이 없습니다."; // 선택 없음 설명 표시
                _detailCostText.text = string.Empty; // 선택 없음 비용 숨김
                _detailStatusText.text = string.Empty; // 선택 없음 상태 숨김
                _unlockButton.interactable = false; // 선택 없음 해금 버튼 비활성화
                _unlockButtonText.text = "선택 없음"; // 선택 없음 버튼 문구 적용
                return; // 상세 정보 처리 종료
            }

            MetaUnlockDisplayState state = MetaProgressPanelState.Evaluate(_progress, definition); // 현재 선택 해금 상태 계산
            _detailTypeText.text = GetTypeLabel(definition.UnlockType); // 선택 해금 타입 표시
            _detailTitleText.text = definition.DisplayName; // 선택 해금 이름 표시
            _detailDescriptionText.text = definition.Description; // 선택 해금 상세 설명 표시
            _detailCostText.text = $"비용  {definition.Cost} Meta Token"; // 선택 해금 비용 표시
            _detailStatusText.text = GetStateLabel(state); // 선택 해금 상태 표시
            _detailStatusText.color = GetStateColor(state); // 선택 해금 상태 색상 적용
            ConfigureUnlockButton(definition, state); // 선택 상태별 해금 버튼 설정
        }

        private void ConfigureUnlockButton(MetaUnlockDefinition definition, MetaUnlockDisplayState state)
        {
            if (state == MetaUnlockDisplayState.Unlocked)
            {
                _unlockButton.interactable = false; // 해금 완료 버튼 비활성화
                _unlockButtonText.text = "해금 완료"; // 해금 완료 버튼 문구 적용
                return; // 해금 완료 버튼 설정 종료
            }

            if (state == MetaUnlockDisplayState.Insufficient)
            {
                int shortage = Mathf.Max(0, definition.Cost - _progress.MetaTokens); // 부족 토큰 수 계산
                _unlockButton.interactable = false; // 토큰 부족 버튼 비활성화
                _unlockButtonText.text = $"{shortage} Token 부족"; // 부족 토큰 버튼 문구 적용
                return; // 토큰 부족 버튼 설정 종료
            }

            if (state == MetaUnlockDisplayState.Available)
            {
                _unlockButton.interactable = true; // 해금 가능 버튼 활성화
                _unlockButtonText.text = $"해금하기 · {definition.Cost} Token"; // 해금 가능 버튼 문구 적용
                return; // 해금 가능 버튼 설정 종료
            }

            _unlockButton.interactable = false; // 잠김 버튼 비활성화
            _unlockButtonText.text = "잠김"; // 잠김 버튼 문구 적용
        }

        private void RequestUnlock()
        {
            MetaUnlockDefinition definition = _panelState.GetSelectedDefinition(); // 현재 선택 해금 정의 조회
            if (MetaProgressPanelState.Evaluate(_progress, definition) != MetaUnlockDisplayState.Available) return; // 해금 가능 상태 외 확인 차단

            _pendingUnlock = definition; // 확인 대기 해금 정의 저장
            _confirmTitleText.text = $"{definition.DisplayName} 해금"; // 확인 팝업 제목 적용
            _confirmBodyText.text = $"{definition.Cost} Meta Token을 사용해 영구 해금합니다.\n\n해금 데이터는 즉시 영구 저장됩니다."; // 확인 팝업 비용·저장 안내 적용
            _confirmRoot.SetActive(true); // 해금 확인 팝업 표시
        }

        private void CancelUnlock()
        {
            _pendingUnlock = null; // 확인 대기 해금 정의 정리
            _confirmRoot.SetActive(false); // 해금 확인 팝업 숨김
        }

        private void ConfirmUnlock()
        {
            if (_pendingUnlock == null) return; // 확인 대기 정의 누락 차단

            MetaUnlockDefinition definition = _pendingUnlock; // 현재 확인 대상 정의 보관
            _pendingUnlock = null; // 확인 대기 상태 선정리
            _confirmRoot.SetActive(false); // 해금 확인 팝업 숨김

            if (!MetaUnlockService.TryUnlock(_progress, definition))
            {
                _feedbackText.text = "해금할 수 없습니다. 토큰 잔액 또는 해금 상태를 확인하세요."; // 해금 실패 안내
                RefreshPanel(); // 최신 진행 상태 재표시
                return; // 해금 실패 처리 종료
            }

            bool saved = MetaProgressService.Save(); // 영구 해금·토큰 변화 즉시 저장
            _feedbackText.text = saved
                ? $"{definition.DisplayName} 영구 해금 완료"
                : $"{definition.DisplayName} 해금 완료 · 저장 실패"; // 해금 저장 결과 안내
            _panelState.Select(definition); // 해금한 항목 선택 유지
            RefreshHeader(); // 토큰·해금 수 즉시 갱신
            RebuildItems(); // 해금 카드 상태 즉시 갱신
            RefreshDetails(); // 상세 상태 즉시 갱신
        }

        private void HandleClose()
        {
            CancelUnlock(); // 열린 확인 팝업 정리
            _closeRequested?.Invoke(); // MainMenu 뒤로가기 요청
        }

        private static string GetCategoryLabel(MetaProgressCategory category)
        {
            if (category == MetaProgressCategory.Piece) return "기물"; // 기물 카테고리 문구 반환
            if (category == MetaProgressCategory.King) return "킹"; // 킹 카테고리 문구 반환
            if (category == MetaProgressCategory.Passive) return "패시브"; // 패시브 카테고리 문구 반환
            return "전체"; // 기본 전체 카테고리 문구 반환
        }

        private static string GetTypeLabel(MetaUnlockType type)
        {
            if (type == MetaUnlockType.King) return "KING UNLOCK"; // 킹 타입 문구 반환
            if (type == MetaUnlockType.Passive) return "PASSIVE UNLOCK"; // 패시브 타입 문구 반환
            return "PIECE UNLOCK"; // 기본 기물 타입 문구 반환
        }

        private static string GetStateLabel(MetaUnlockDisplayState state)
        {
            if (state == MetaUnlockDisplayState.Unlocked) return "해금 완료"; // 해금 완료 상태 문구 반환
            if (state == MetaUnlockDisplayState.Available) return "해금 가능"; // 해금 가능 상태 문구 반환
            if (state == MetaUnlockDisplayState.Insufficient) return "토큰 부족"; // 토큰 부족 상태 문구 반환
            return "잠김"; // 기본 잠김 상태 문구 반환
        }

        private static Color GetStateColor(MetaUnlockDisplayState state)
        {
            if (state == MetaUnlockDisplayState.Unlocked) return UnlockedColor; // 해금 완료 상태 색상 반환
            if (state == MetaUnlockDisplayState.Available) return AccentColor; // 해금 가능 상태 색상 반환
            if (state == MetaUnlockDisplayState.Insufficient) return WarningColor; // 토큰 부족 상태 색상 반환
            return new Color(0.12f, 0.13f, 0.16f, 1f); // 기본 잠김 상태 색상 반환
        }

        private static GameObject CreateImage(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var result = new GameObject(name, typeof(RectTransform), typeof(Image)); // Image UI 오브젝트 생성
            result.transform.SetParent(parent, false); // UI 부모 연결
            SetRect(result.GetComponent<RectTransform>(), position, size); // UI 위치·크기 적용
            result.GetComponent<Image>().color = color; // UI 배경 색상 적용
            return result; // 완성 UI 오브젝트 반환
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size)
        {
            var result = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); // Button UI 오브젝트 생성
            result.transform.SetParent(parent, false); // UI 부모 연결
            SetRect(result.GetComponent<RectTransform>(), position, size); // 버튼 위치·크기 적용

            Image image = result.GetComponent<Image>(); // 버튼 배경 조회
            Color baseColor = new Color(0.13f, 0.17f, 0.23f, 1f); // 기본 버튼 색상
            image.color = baseColor; // 기본 버튼 배경 적용

            Button button = result.GetComponent<Button>(); // Button 컴포넌트 조회
            button.targetGraphic = image; // 버튼 대상 그래픽 지정
            ColorBlock colors = button.colors; // 버튼 상태 색상 조회
            colors.normalColor = Color.white; // Image.color 기반 기본 상태 유지
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f); // Hover 밝기 적용
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f); // Pressed 밝기 적용
            colors.selectedColor = colors.highlightedColor; // 선택 밝기 적용
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.72f); // Disabled 밝기 적용
            colors.colorMultiplier = 1f; // 버튼 색상 배수 유지
            colors.fadeDuration = 0.08f; // 버튼 전환 시간 적용
            button.colors = colors; // 버튼 상태 색상 저장

            Text text = CreateText("Label", result.transform, 21, FontStyle.Bold, TextAnchor.MiddleCenter); // 버튼 문구 생성
            text.text = label; // 버튼 문구 적용
            Stretch(text.rectTransform, 10f); // 버튼 문구 내부 여백 적용
            return button; // 완성 버튼 반환
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle style, TextAnchor alignment)
        {
            var result = new GameObject(name, typeof(RectTransform), typeof(Text)); // Text UI 오브젝트 생성
            result.transform.SetParent(parent, false); // UI 부모 연결
            Text text = result.GetComponent<Text>(); // Text 컴포넌트 조회
            text.font = GetRuntimeFont(); // 한글 런타임 폰트 적용
            text.fontSize = fontSize; // 글자 크기 적용
            text.fontStyle = style; // 글자 스타일 적용
            text.alignment = alignment; // 텍스트 정렬 적용
            text.color = Color.white; // 기본 글자색 적용
            text.raycastTarget = false; // 버튼 입력 간섭 제거
            return text; // 완성 Text 반환
        }

        private static Font GetRuntimeFont()
        {
            if (_runtimeFont != null) return _runtimeFont; // 기존 런타임 폰트 재사용
            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 24); // 시스템 한글 폰트 생성
            if (_runtimeFont == null) _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 fallback
            return _runtimeFont; // 최종 런타임 폰트 반환
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f); // 중앙 앵커 시작 적용
            rect.anchorMax = new Vector2(0.5f, 0.5f); // 중앙 앵커 끝 적용
            rect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 적용
            rect.anchoredPosition = position; // UI 위치 적용
            rect.sizeDelta = size; // UI 크기 적용
        }

        private static void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero; // Stretch 시작 앵커 적용
            rect.anchorMax = Vector2.one; // Stretch 끝 앵커 적용
            rect.offsetMin = new Vector2(padding, padding); // 좌하단 여백 적용
            rect.offsetMax = new Vector2(-padding, -padding); // 우상단 여백 적용
        }
    }
}
