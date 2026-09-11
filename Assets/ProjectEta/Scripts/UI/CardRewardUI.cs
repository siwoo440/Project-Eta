using System.Collections.Generic; // List<T>·IReadOnlyList<T> 사용
using UnityEngine; // MonoBehaviour·GameObject·Color·Vector2 사용
using UnityEngine.EventSystems; // EventSystem 사용
using UnityEngine.InputSystem.UI; // 새 Input System UI 입력 모듈 사용
using UnityEngine.UI; // Canvas·Button·Image·Outline·Text 사용
using ProjectEta.Battle; // BattleController 사용
using ProjectEta.Pieces; // PieceDefinition 사용
using ProjectEta.Run; // CardRewardSource·RunEconomyService 사용

namespace ProjectEta.UI // 카드 보상 UI 네임스페이스
{
    public sealed class CardRewardUI : MonoBehaviour // 65일차 Reward 카드 3택 정식 UI
    {
        private const float CardWidth = 300f; // 보상 카드 폭
        private const float CardHeight = 392f; // 보상 카드 높이
        private const float CardGap = 46f; // 카드 사이 간격

        private sealed class RewardCardView // 후보 카드 1장의 화면 참조 묶음
        {
            public PieceDefinition Definition; // 실제 후보 카드 정의
            public GameObject Root; // 카드 루트 오브젝트
            public Image Frame; // 선택 강조 프레임
            public Outline Outline; // 선택 외곽선
        }

        private readonly List<RewardCardView> _cardViews = new List<RewardCardView>(); // 현재 생성된 후보 카드 UI 목록
        private Canvas _canvas; // 카드 보상 전용 Canvas
        private GameObject _root; // 전체 화면 보상 루트
        private Text _titleText; // 보상 제목
        private Text _statusText; // Gold·King HP·보유 카드 상태
        private Text _detailTitleText; // 선택 카드 상세 제목
        private Text _detailBodyText; // 선택 카드 상세 정보
        private Button _confirmButton; // 선택 확정 버튼
        private Text _confirmButtonText; // 선택 확정 버튼 문구
        private PieceDefinition _selectedDefinition; // 현재 선택된 후보 카드
        private System.Action<PieceDefinition> _selectionCallback; // 카드 선택 완료 콜백
        private EventSystem _createdEventSystem; // 직접 생성한 EventSystem 참조
        private static Font _runtimeFont; // 한글 런타임 폰트 캐시

        public bool IsVisible => _root != null && _root.activeSelf; // 현재 카드 보상 화면 표시 여부

        public void Show(IReadOnlyList<PieceDefinition> candidates, CardRewardSource source, System.Action<PieceDefinition> selectionCallback) // 기존 호출 호환 카드 후보 화면 표시
        {
            Show(candidates, source, null, selectionCallback); // 품질 정보 없는 기존 호출을 통합 표시로 위임
        }

        public void Show(IReadOnlyList<PieceDefinition> candidates, CardRewardSource source, CardRewardProfile profile, System.Action<PieceDefinition> selectionCallback) // Stage·품질을 포함한 카드 후보 화면 표시
        {
            EnsureUI(); // 보상 Canvas 최초 생성
            ClearCardObjects(); // 이전 후보 UI 제거
            _selectionCallback = selectionCallback; // 현재 선택 콜백 저장
            _selectedDefinition = null; // 이전 선택 카드 초기화
            string sourceTitle = source == CardRewardSource.RewardNode ? "REWARD STAGE" : "BATTLE REWARD"; // 보상 발생 경로 제목 계산
            _titleText.text = profile == null
                ? $"{sourceTitle} · 카드 1장 선택" // 기존 호출 기본 제목 표시
                : $"{sourceTitle} · S{profile.Stage} · {profile.DisplayName}"; // 현재 Stage·보상 품질 표시
            RefreshRunStatus(); // 현재 런 Gold·HP·보유 카드 표시
            RefreshDetailPanel(); // 상세 패널 초기화
            RefreshConfirmButton(); // 확정 버튼 초기 비활성화

            int count = candidates != null ? candidates.Count : 0; // 실제 후보 수 계산
            float totalWidth = count > 0 ? CardWidth * count + CardGap * (count - 1) : 0f; // 후보 카드 전체 폭 계산
            float startX = -totalWidth * 0.5f + CardWidth * 0.5f; // 첫 카드 중심 X 계산

            for (int i = 0; i < count; i++) // 후보 카드 순회
            {
                PieceDefinition definition = candidates[i]; // 현재 후보 정의 조회

                if (definition == null) // 빈 후보 확인
                {
                    continue; // 빈 후보 제외
                }

                float x = startX + i * (CardWidth + CardGap); // 현재 카드 화면 X 계산
                CreateRewardCard(definition, x); // 실제 선택 카드 UI 생성
            }

            _root.SetActive(true); // 보상 화면 활성화
        }

        public void Hide() // 현재 카드 보상 화면 숨김
        {
            if (_root != null) // 보상 루트 생성 여부 확인
            {
                _root.SetActive(false); // 전체 보상 루트 비활성화
            }

            _selectionCallback = null; // 이전 선택 콜백 제거
            _selectedDefinition = null; // 현재 선택 카드 제거
            ClearCardObjects(); // 후보 카드 UI 제거
            RefreshDetailPanel(); // 상세 패널 초기화
            RefreshConfirmButton(); // 확정 버튼 초기화
        }

        private void EnsureUI() // 카드 보상 Canvas와 배경을 한 번만 생성
        {
            if (_canvas != null) // 기존 Canvas 확인
            {
                return; // 중복 생성 차단
            }

            EnsureEventSystem(); // UI 클릭용 EventSystem 보장

            var canvasObject = new GameObject("CardRewardCanvas_Day65", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 보상 전용 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 컨트롤러 호스트 자식 연결
            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 저장
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 위 직접 표시
            _canvas.sortingOrder = UiLayerOrder.RewardModal; // 공통 Reward 모달 계층 적용

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // 해상도 대응 스케일러 확보
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 스케일 사용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 프로젝트 개발 UI 기준 해상도
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 보정

            BuildRoot(canvasObject.transform); // 전체 보상 배경·제목·상세 패널 생성
        }

        private void BuildRoot(Transform parent) // 전체 화면 입력 차단 배경과 정식 Reward 레이아웃 생성
        {
            _root = new GameObject("CardRewardRoot", typeof(RectTransform), typeof(Image)); // 전체 화면 보상 루트 생성
            _root.transform.SetParent(parent, false); // Canvas 자식 연결

            RectTransform rect = _root.GetComponent<RectTransform>(); // 전체 화면 RectTransform 확보
            rect.anchorMin = Vector2.zero; // 좌하단 Stretch 시작
            rect.anchorMax = Vector2.one; // 우상단 Stretch 끝
            rect.offsetMin = Vector2.zero; // 좌하단 여백 제거
            rect.offsetMax = Vector2.zero; // 우상단 여백 제거

            Image blocker = _root.GetComponent<Image>(); // 전체 화면 배경 이미지 확보
            blocker.color = new Color(0.018f, 0.022f, 0.034f, 0.96f); // 전투 UI와 연결되는 어두운 배경 적용
            blocker.raycastTarget = true; // 지도 클릭 차단

            var headerObject = new GameObject("RewardHeader", typeof(RectTransform), typeof(Image)); // 상단 정보 패널 생성
            headerObject.transform.SetParent(_root.transform, false); // 보상 루트 자식 연결
            RectTransform headerRect = headerObject.GetComponent<RectTransform>(); // 상단 패널 RectTransform 확보
            SetRect(headerRect, new Vector2(0f, 445f), new Vector2(1280f, 116f)); // 상단 중앙 배치
            Image headerBackground = headerObject.GetComponent<Image>(); // 상단 패널 배경 확보
            headerBackground.color = new Color(0.045f, 0.052f, 0.072f, 0.98f); // 정식 HUD 패널 색상 적용
            headerBackground.raycastTarget = false; // 카드 선택 클릭 간섭 제거

            _titleText = CreateText("RewardTitle", headerObject.transform, 32, FontStyle.Bold); // 보상 제목 생성
            _titleText.alignment = TextAnchor.MiddleLeft; // 제목 좌측 정렬
            SetRect(_titleText.rectTransform, new Vector2(-350f, 18f), new Vector2(540f, 48f)); // 제목 위치·크기 적용

            _statusText = CreateText("RewardStatus", headerObject.transform, 21, FontStyle.Bold); // 현재 런 상태 생성
            _statusText.alignment = TextAnchor.MiddleRight; // 상태 우측 정렬
            SetRect(_statusText.rectTransform, new Vector2(310f, 18f), new Vector2(650f, 48f)); // 상태 위치·크기 적용

            Text guideText = CreateText("RewardGuide", headerObject.transform, 17, FontStyle.Normal); // 선택 안내 문구 생성
            guideText.text = "후보를 비교한 뒤 한 장을 선택하고 아래의 선택 확정 버튼을 누르세요."; // Reward 조작 안내 적용
            guideText.alignment = TextAnchor.MiddleLeft; // 안내 좌측 정렬
            guideText.color = new Color(0.78f, 0.82f, 0.90f, 1f); // 보조 문구 색상 적용
            SetRect(guideText.rectTransform, new Vector2(-105f, -28f), new Vector2(1030f, 38f)); // 안내 위치 적용

            BuildDetailPanel(_root.transform); // 하단 카드 상세 패널 생성
            BuildConfirmButton(_root.transform); // 선택 확정 버튼 생성
            _root.SetActive(false); // 기본 숨김 상태 지정
        }

        private void BuildDetailPanel(Transform parent) // 선택한 후보 카드의 상세 비교 패널 생성
        {
            var panelObject = new GameObject("RewardDetailPanel", typeof(RectTransform), typeof(Image)); // 상세 패널 생성
            panelObject.transform.SetParent(parent, false); // 보상 루트 자식 연결
            RectTransform panelRect = panelObject.GetComponent<RectTransform>(); // 상세 패널 RectTransform 확보
            SetRect(panelRect, new Vector2(-115f, -342f), new Vector2(980f, 142f)); // 카드 아래 상세 패널 배치
            Image background = panelObject.GetComponent<Image>(); // 상세 패널 배경 확보
            background.color = new Color(0.035f, 0.040f, 0.055f, 0.98f); // 상세 패널 배경색 적용
            background.raycastTarget = false; // 카드 클릭 간섭 제거

            _detailTitleText = CreateText("DetailTitle", panelObject.transform, 23, FontStyle.Bold); // 상세 카드 제목 생성
            _detailTitleText.alignment = TextAnchor.MiddleLeft; // 상세 제목 좌측 정렬
            SetRect(_detailTitleText.rectTransform, new Vector2(-275f, 36f), new Vector2(380f, 44f)); // 상세 제목 위치 적용

            _detailBodyText = CreateText("DetailBody", panelObject.transform, 18, FontStyle.Normal); // 상세 카드 본문 생성
            _detailBodyText.alignment = TextAnchor.UpperLeft; // 상세 본문 좌상단 정렬
            _detailBodyText.horizontalOverflow = HorizontalWrapMode.Wrap; // 긴 설명 줄바꿈 허용
            _detailBodyText.verticalOverflow = VerticalWrapMode.Truncate; // 패널 바깥 설명 잘라내기
            SetRect(_detailBodyText.rectTransform, new Vector2(65f, -16f), new Vector2(720f, 84f)); // 상세 본문 위치 적용
        }

        private void BuildConfirmButton(Transform parent) // 선택 확정 버튼 생성
        {
            var buttonObject = new GameObject("ConfirmRewardButton", typeof(RectTransform), typeof(Image), typeof(Button)); // 확정 버튼 오브젝트 생성
            buttonObject.transform.SetParent(parent, false); // 보상 루트 자식 연결
            RectTransform rect = buttonObject.GetComponent<RectTransform>(); // 확정 버튼 RectTransform 확보
            SetRect(rect, new Vector2(535f, -342f), new Vector2(280f, 92f)); // 상세 패널 우측 배치
            Image background = buttonObject.GetComponent<Image>(); // 확정 버튼 배경 확보
            background.color = new Color(0.62f, 0.44f, 0.12f, 0.98f); // 금색 계열 확정 버튼 적용
            _confirmButton = buttonObject.GetComponent<Button>(); // 확정 Button 확보
            _confirmButton.targetGraphic = background; // 버튼 대상 그래픽 지정
            _confirmButton.onClick.AddListener(HandleConfirmClicked); // 선택 확정 콜백 연결

            _confirmButtonText = CreateText("ConfirmText", buttonObject.transform, 24, FontStyle.Bold); // 확정 버튼 문구 생성
            SetRect(_confirmButtonText.rectTransform, Vector2.zero, new Vector2(248f, 66f)); // 확정 문구 영역 적용
        }

        private void CreateRewardCard(PieceDefinition definition, float centerX) // 카드 후보 1장의 선택 UI 생성
        {
            var cardObject = new GameObject($"RewardCard_{definition.PieceId}", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button)); // 카드 버튼 오브젝트 생성
            cardObject.transform.SetParent(_root.transform, false); // 보상 루트 자식 연결
            RectTransform rect = cardObject.GetComponent<RectTransform>(); // 카드 RectTransform 확보
            SetRect(rect, new Vector2(centerX, 42f), new Vector2(CardWidth, CardHeight)); // 카드 위치·크기 적용

            Image frame = cardObject.GetComponent<Image>(); // 카드 프레임 이미지 확보
            frame.color = GetGradeColor(definition); // 등급별 카드 프레임 적용
            Outline outline = cardObject.GetComponent<Outline>(); // 선택 외곽선 확보
            outline.effectColor = new Color(0.91f, 0.72f, 0.24f, 1f); // 선택 시 금색 외곽선 설정
            outline.effectDistance = new Vector2(4f, -4f); // 선택 외곽선 두께 설정
            outline.enabled = false; // 기본 선택 외곽선 숨김

            Button button = cardObject.GetComponent<Button>(); // 카드 선택 Button 확보
            button.targetGraphic = frame; // 카드 프레임을 버튼 상태 그래픽으로 사용
            PieceDefinition capturedDefinition = definition; // 반복문 클로저용 현재 카드 고정
            button.onClick.AddListener(() => HandleCardClicked(capturedDefinition)); // 후보 선택 콜백 연결

            var paperObject = new GameObject("CardBody", typeof(RectTransform), typeof(Image)); // 카드 내부 본체 생성
            paperObject.transform.SetParent(cardObject.transform, false); // 카드 프레임 자식 연결
            RectTransform paperRect = paperObject.GetComponent<RectTransform>(); // 카드 본체 RectTransform 확보
            Stretch(paperRect, 8f); // 프레임 안쪽 여백 적용
            Image paper = paperObject.GetComponent<Image>(); // 카드 본체 이미지 확보
            paper.color = new Color(0.055f, 0.060f, 0.078f, 0.98f); // 어두운 카드 본체 적용
            paper.raycastTarget = false; // 루트 Button 입력 방해 차단

            Text nameText = CreateText("Name", paperObject.transform, 24, FontStyle.Bold); // 기물 이름 텍스트 생성
            nameText.text = definition.DisplayName; // 기물 표시 이름 적용
            SetRect(nameText.rectTransform, new Vector2(0f, 160f), new Vector2(260f, 42f)); // 이름 위치 적용

            Text gradeText = CreateText("Grade", paperObject.transform, 19, FontStyle.Bold); // 등급 텍스트 생성
            gradeText.text = new string('★', Mathf.Clamp((int)definition.Grade, 1, 5)); // 1~5성 별표 표시
            gradeText.color = new Color(0.95f, 0.78f, 0.28f, 1f); // 별 등급 금색 적용
            SetRect(gradeText.rectTransform, new Vector2(0f, 127f), new Vector2(260f, 30f)); // 등급 위치 적용

            var artworkObject = new GameObject("Artwork", typeof(RectTransform), typeof(Image)); // 카드 Artwork 영역 생성
            artworkObject.transform.SetParent(paperObject.transform, false); // 카드 본체 자식 연결
            RectTransform artworkRect = artworkObject.GetComponent<RectTransform>(); // Artwork RectTransform 확보
            SetRect(artworkRect, new Vector2(0f, 45f), new Vector2(252f, 128f)); // Artwork 위치·크기 적용
            Image artwork = artworkObject.GetComponent<Image>(); // Artwork 이미지 확보
            artwork.sprite = definition.CardArtwork; // PieceDefinition 카드 Artwork 적용
            artwork.preserveAspect = true; // 원본 비율 유지
            artwork.color = definition.CardArtwork != null ? Color.white : new Color(0.12f, 0.13f, 0.16f, 1f); // Artwork 없는 카드 대체 색상 적용
            artwork.raycastTarget = false; // 카드 클릭 간섭 제거

            Text statText = CreateText("Stats", paperObject.transform, 19, FontStyle.Bold); // HP·ATK 텍스트 생성
            statText.text = $"ATK {definition.BaseAtk}    HP {definition.BaseHp}"; // 기본 능력치 표시
            SetRect(statText.rectTransform, new Vector2(0f, -42f), new Vector2(252f, 36f)); // 능력치 위치 적용

            Text categoryText = CreateText("Category", paperObject.transform, 14, FontStyle.Normal); // 분류 텍스트 생성
            categoryText.text = $"{definition.Category} · {definition.MovementType}"; // 획득 분류·이동 타입 표시
            categoryText.color = new Color(0.74f, 0.78f, 0.86f, 1f); // 보조 정보 색상 적용
            SetRect(categoryText.rectTransform, new Vector2(0f, -78f), new Vector2(252f, 28f)); // 분류 위치 적용

            Text descriptionText = CreateText("Description", paperObject.transform, 14, FontStyle.Normal); // 카드 설명 텍스트 생성
            descriptionText.text = string.IsNullOrWhiteSpace(definition.Description) ? "카드 설명 없음" : definition.Description; // 설명 기본값 보정
            descriptionText.alignment = TextAnchor.UpperLeft; // 카드 설명 좌상단 정렬
            descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap; // 카드 폭 안에서 줄바꿈
            descriptionText.verticalOverflow = VerticalWrapMode.Truncate; // 카드 높이 초과 문구 잘라내기
            SetRect(descriptionText.rectTransform, new Vector2(0f, -135f), new Vector2(252f, 74f)); // 설명 위치 적용

            _cardViews.Add(new RewardCardView // 카드 뷰 참조 등록
            {
                Definition = definition, // 후보 정의 저장
                Root = cardObject, // 카드 루트 저장
                Frame = frame, // 프레임 저장
                Outline = outline, // 외곽선 저장
            });
        }

        private void HandleCardClicked(PieceDefinition definition) // 후보 카드 클릭 시 미리 선택 상태만 변경
        {
            if (definition == null || _selectionCallback == null) // 잘못된 선택 상태 확인
            {
                return; // 선택 처리 차단
            }

            _selectedDefinition = definition; // 현재 후보를 선택 카드로 저장
            RefreshCardSelectionVisuals(); // 카드 선택 강조 갱신
            RefreshDetailPanel(); // 하단 상세 정보 갱신
            RefreshConfirmButton(); // 선택 확정 버튼 활성화
        }

        private void HandleConfirmClicked() // 현재 선택 카드 실제 획득 확정
        {
            if (_selectedDefinition == null || _selectionCallback == null) // 선택 카드·콜백 확인
            {
                return; // 빈 확정 입력 차단
            }

            PieceDefinition confirmed = _selectedDefinition; // 콜백 중 UI 상태 변경에 대비해 선택 카드 보관
            _confirmButton.interactable = false; // 중복 확정 입력 즉시 차단
            _selectionCallback.Invoke(confirmed); // 기존 CardRewardController 획득 처리 요청
        }

        private void RefreshCardSelectionVisuals() // 후보 카드 선택 강조 상태 갱신
        {
            for (int i = 0; i < _cardViews.Count; i++) // 현재 후보 카드 순회
            {
                RewardCardView view = _cardViews[i]; // 현재 카드 뷰 조회
                bool selected = view.Definition == _selectedDefinition; // 현재 선택 카드 여부 확인
                view.Outline.enabled = selected; // 선택 카드만 금색 외곽선 표시
                view.Root.transform.localScale = selected ? Vector3.one * 1.035f : Vector3.one; // 선택 카드만 약간 확대
                view.Frame.color = selected ? new Color(0.70f, 0.50f, 0.16f, 1f) : GetGradeColor(view.Definition); // 선택 프레임 강조 적용
            }
        }

        private void RefreshDetailPanel() // 선택 카드 상세 정보 표시
        {
            if (_detailTitleText == null || _detailBodyText == null) // 상세 UI 생성 여부 확인
            {
                return; // 생성 전 갱신 생략
            }

            if (_selectedDefinition == null) // 선택 카드 없음 확인
            {
                _detailTitleText.text = "카드 비교"; // 기본 상세 제목 표시
                _detailBodyText.text = "세 후보의 등급·ATK·HP·설명을 비교한 뒤 카드를 선택하세요."; // 기본 안내 문구 표시
                return; // 선택 상세 처리 종료
            }

            PieceDefinition card = _selectedDefinition; // 현재 선택 카드 조회
            _detailTitleText.text = $"{card.DisplayName}  {new string('★', Mathf.Clamp((int)card.Grade, 1, 5))}"; // 선택 카드 이름·등급 표시
            string description = string.IsNullOrWhiteSpace(card.Description) ? "카드 설명 없음" : card.Description; // 설명 기본값 보정
            _detailBodyText.text = $"ATK {card.BaseAtk}    HP {card.BaseHp}    |    {card.Category} · {card.MovementType}\n{description}"; // 선택 카드 상세 정보 표시
        }

        private void RefreshConfirmButton() // 선택 확정 버튼 상태 갱신
        {
            if (_confirmButton == null || _confirmButtonText == null) // 확정 UI 생성 여부 확인
            {
                return; // 생성 전 갱신 생략
            }

            bool canConfirm = _selectedDefinition != null && _selectionCallback != null; // 현재 확정 가능 여부 계산
            _confirmButton.interactable = canConfirm; // 버튼 상호작용 상태 적용
            _confirmButtonText.text = canConfirm ? "선택 확정" : "카드를 선택하세요"; // 현재 상태 문구 표시
        }

        private void RefreshRunStatus() // 현재 런 Gold·HP·보유 카드 상태 표시
        {
            if (_statusText == null) // 상태 UI 생성 여부 확인
            {
                return; // 생성 전 갱신 생략
            }

            BattleController battleController = Object.FindFirstObjectByType<BattleController>(); // 현재 전투 컨트롤러 탐색
            RunState runState = battleController != null ? battleController.RunState : null; // 현재 런 상태 조회

            if (runState == null) // 런 상태 누락 확인
            {
                _statusText.text = string.Empty; // 상태 표시 생략
                return; // 추가 조회 중단
            }

            RunEconomyState economy = RunEconomyService.GetOrCreate(runState); // 현재 런 Gold 상태 연결
            int gold = economy != null ? economy.Currency : 0; // 현재 Gold 조회
            int ownedCardCount = runState.Deck != null ? runState.Deck.OwnedCardPool.Count : 0; // 현재 보유 카드 수 조회
            _statusText.text = $"Gold {gold}    |    King HP {runState.KingHp}/{RunEconomyRules.PrototypeKingMaxHp}    |    Cards {ownedCardCount}"; // Reward 공통 상태 표시
        }

        private void ClearCardObjects() // 현재 후보 카드 UI 정리
        {
            for (int i = 0; i < _cardViews.Count; i++) // 생성 카드 순회
            {
                if (_cardViews[i].Root != null) // 카드 루트 존재 여부 확인
                {
                    Destroy(_cardViews[i].Root); // 런타임 카드 버튼 제거
                }
            }

            _cardViews.Clear(); // 카드 UI 목록 초기화
        }

        private static Color GetGradeColor(PieceDefinition definition) // 현재 카드 등급에 따른 프레임색 선택
        {
            int grade = definition != null ? (int)definition.Grade : 1; // 카드 등급 숫자 변환

            if (grade >= 5) // 5성 카드 확인
            {
                return new Color(0.57f, 0.38f, 0.12f, 1f); // 5성 금갈색 계열 적용
            }

            if (grade == 4) // 4성 카드 확인
            {
                return new Color(0.48f, 0.24f, 0.46f, 1f); // 4성 자주색 계열 적용
            }

            if (grade == 3) // 3성 카드 확인
            {
                return new Color(0.31f, 0.23f, 0.48f, 1f); // 3성 보라 계열 적용
            }

            if (grade == 2) // 2성 카드 확인
            {
                return new Color(0.18f, 0.31f, 0.46f, 1f); // 2성 청색 계열 적용
            }

            return new Color(0.25f, 0.27f, 0.31f, 1f); // 1성 회색 계열 적용
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle fontStyle) // 공통 Text 생성
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // Text 오브젝트 생성
            textObject.transform.SetParent(parent, false); // 부모 자식 연결
            Text text = textObject.GetComponent<Text>(); // Text 컴포넌트 확보
            text.font = GetRuntimeFont(); // 한글 런타임 폰트 적용
            text.fontSize = fontSize; // 글자 크기 적용
            text.fontStyle = fontStyle; // 글자 굵기 적용
            text.alignment = TextAnchor.MiddleCenter; // 중앙 정렬 적용
            text.color = Color.white; // 흰색 문구 적용
            text.raycastTarget = false; // 카드 버튼 클릭 방해 차단
            return text; // 완성 Text 반환
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size) // 중앙 기준 UI 위치·크기 적용
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f); // 중앙 앵커 시작
            rect.anchorMax = new Vector2(0.5f, 0.5f); // 중앙 앵커 끝
            rect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 적용
            rect.anchoredPosition = position; // UI 위치 적용
            rect.sizeDelta = size; // UI 크기 적용
        }

        private static void Stretch(RectTransform rect, float padding) // 부모 RectTransform에 여백 포함 Stretch 적용
        {
            rect.anchorMin = Vector2.zero; // Stretch 시작 앵커 적용
            rect.anchorMax = Vector2.one; // Stretch 끝 앵커 적용
            rect.offsetMin = new Vector2(padding, padding); // 좌하단 여백 적용
            rect.offsetMax = new Vector2(-padding, -padding); // 우상단 여백 적용
        }

        private void EnsureEventSystem() // 카드 선택 클릭용 EventSystem 보장
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) // 기존 EventSystem 존재 여부 확인
            {
                return; // 기존 EventSystem 재사용
            }

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 새 Input System EventSystem 생성
            _createdEventSystem = eventSystemObject.GetComponent<EventSystem>(); // 직접 생성 EventSystem 참조 저장
        }

        private static Font GetRuntimeFont() // 한글 표시 가능한 런타임 폰트 확보
        {
            if (_runtimeFont != null) // 기존 폰트 캐시 확인
            {
                return _runtimeFont; // 기존 폰트 재사용
            }

            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 24); // 시스템 한글 폰트 우선 생성

            if (_runtimeFont == null) // 시스템 폰트 생성 실패 확인
            {
                _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 실패 시 Unity 기본 폰트 사용
            }

            return _runtimeFont; // 최종 폰트 반환
        }

        private void OnDestroy() // UI 제거 시 직접 생성 EventSystem 정리
        {
            if (_createdEventSystem != null) // 직접 생성 EventSystem 존재 여부 확인
            {
                Destroy(_createdEventSystem.gameObject); // 직접 만든 EventSystem 제거
            }
        }
    }
}
