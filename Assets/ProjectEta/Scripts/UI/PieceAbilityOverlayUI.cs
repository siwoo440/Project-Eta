using System.Collections; // 초기 연결 대기
using System.Collections.Generic; // 버튼 목록
using System.Text; // 설명 문구 구성
using UnityEngine; // UI 오브젝트 생성
using UnityEngine.UI; // 텍스트와 버튼
using ProjectEta.Abilities; // 능력 선택 처리
using ProjectEta.Board; // 보드 입력 참조
using ProjectEta.Pieces; // 선택 기물 참조

namespace ProjectEta.UI // 기물 능력 UI 영역
{ // 범위 시작
    public sealed class PieceAbilityOverlayUI : MonoBehaviour // 선택 기물의 설명과 능력 버튼
    { // 범위 시작
        private BoardInputController _boardInput; // 실제 전투 입력
        private GameObject _panel; // 능력 패널
        private Text _text; // 능력 설명
        private readonly List<Button> _buttons = new List<Button>(); // 다시 그릴 선택 버튼
        private string _lastState; // 마지막 표시 상태
        private static Font _font; // 한글 글꼴 캐시

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // 씬 진입 시 자동 설치
        private static void Install() // 프리팹 수정 없는 UI 설치
        { // 범위 시작
            if (Object.FindFirstObjectByType<PieceAbilityOverlayUI>() != null) // 중복 UI 확인
            { // 범위 시작
                return; // 기존 UI 유지
            } // 범위 종료
            new GameObject("PieceAbilityOverlayUI").AddComponent<PieceAbilityOverlayUI>(); // 능력 UI 생성
        } // 범위 종료

        private IEnumerator Start() // 보드 연결과 패널 생성
        { // 범위 시작
            EnsureUI(); // 화면 UI 생성
            for (int frame = 0; frame < 120 && _boardInput == null; frame++) // 보드 생성 대기
            { // 범위 시작
                _boardInput = Object.FindFirstObjectByType<BoardInputController>(); // 활성 입력 조회
                if (_boardInput == null) // 연결 대기 확인
                { // 범위 시작
                    yield return null; // 다음 프레임 대기
                } // 범위 종료
            } // 범위 종료
            if (_boardInput != null) // 입력 연결 여부
            { // 범위 시작
                _boardInput.SelectionChanged += Refresh; // 기물 선택 구독
            } // 범위 종료
            Refresh(null); // 초기 숨김
        } // 범위 종료

        private void Update() // 행동 권한과 형태 변경 감지
        { // 범위 시작
            if (_boardInput == null) // 보드 미연결 확인
            { // 범위 시작
                return; // 갱신 대기
            } // 범위 종료
            var piece = _boardInput.SelectedPiece; // 현재 선택 기물
            string state = $"{piece?.GetHashCode()}:{piece?.MovementCycleIndex}:{_boardInput.CanUseCombatInput}:{_boardInput.CanUseDeploymentInput}:{_boardInput.PendingAbility?.AbilityId}"; // 변경 감지용 표시 상태
            if (state != _lastState) // 표시 변경 확인
            { // 범위 시작
                _lastState = state; // 상태 기록
                Refresh(piece); // 표시와 버튼 갱신
            } // 범위 종료
        } // 범위 종료

        private void Refresh(PieceRuntimeState piece) // 선택 설명과 버튼 재구성
        { // 범위 시작
            if (_text == null) // UI 생성 여부
            { // 범위 시작
                return; // 초기화 대기
            } // 범위 종료
            foreach (var button in _buttons) // 기존 선택 버튼 순회
            { // 범위 시작
                button.gameObject.SetActive(false); // 이전 버튼 입력 즉시 차단
                Destroy(button.gameObject); // 이전 버튼 정리
            } // 범위 종료
            _buttons.Clear(); // 버튼 목록 초기화
            bool visible = piece?.Definition != null && !piece.IsDead && piece.Definition.Abilities.Length > 0; // 표시 가능한 선택 확인
            _panel.SetActive(visible); // 기물 선택 시 패널 표시
            if (!visible) // 선택 없음 확인
            { // 범위 시작
                return; // 설명 작성 중단
            } // 범위 종료
            var builder = new StringBuilder(); // 설명 문구 구성
            builder.AppendLine(piece.Definition.DisplayName + " · 능력"); // 기물 제목
            foreach (var ability in piece.Definition.Abilities) // 보유 능력 순회
            { // 범위 시작
                if (ability == null) // 빈 정의 확인
                { // 범위 시작
                    continue; // 비어 있는 능력 제외
                } // 범위 종료
                builder.AppendLine("• " + ability.DisplayName); // 능력 이름 표시
                builder.AppendLine(ability.Description); // 조건과 임시 수치 표시
                if (FiveStarActiveAbilityService.IsSageChoice(ability)) // 대현자 행동 선택 확인
                { // 범위 시작
                    var selectedAbility = ability; // 버튼별 능력 참조 보존
                    AddButton(ability.DisplayName.Replace("현자의 선택: ", ""), () => _boardInput.TryBeginSelectedAbility(selectedAbility), _boardInput.CanUseCombatInput); // 회복과 독과 보호 선택 버튼
                } // 범위 종료
            } // 범위 종료
            if (FiveStarCombatAbilityResolver.FindAbility(piece, FiveStarAbilityIds.Phantom) != null) // 환영 형태 선택 기물 확인
            { // 범위 시작
                builder.AppendLine("현재 형태: " + FiveStarActiveAbilityService.GetFormName(piece.MovementCycleIndex)); // 현재 이동 형태 표시
                builder.AppendLine("배치 턴에 형태 버튼으로 선택"); // 배치 전용 입력 안내
                for (int i = 0; i < 5; i++) // 5가지 형태 순회
                { // 범위 시작
                    int form = i; // 버튼별 형태 번호 보존
                    AddButton(FiveStarActiveAbilityService.GetFormName(form), () => _boardInput.TrySelectPhantomForm(form), _boardInput.CanUseDeploymentInput); // 배치 형태 선택 버튼
                } // 범위 종료
            } // 범위 종료
            if (_boardInput.PendingAbility != null) // 능력 대상 지정 대기 확인
            { // 범위 시작
                builder.AppendLine("▶ " + _boardInput.PendingAbility.DisplayName + ": 강조된 기물을 클릭"); // 대상 선택 안내
                AddButton("선택 취소", () => _boardInput.CancelSelectedAbility(), true); // 일반 이동으로 돌아가는 버튼
            } // 범위 종료
            _text.text = builder.ToString().TrimEnd(); // 완성된 설명 반영
        } // 범위 종료

        private void AddButton(string label, UnityEngine.Events.UnityAction action, bool enabled) // 능력 선택 버튼 생성
        { // 범위 시작
            int index = _buttons.Count; // 버튼 배치 순번
            var host = new GameObject("Ability_" + label, typeof(RectTransform), typeof(Image), typeof(Button)); // 버튼 오브젝트 생성
            host.transform.SetParent(_panel.transform, false); // 패널 하위 연결
            var rect = host.GetComponent<RectTransform>(); // 버튼 위치 참조
            rect.anchorMin = rect.anchorMax = Vector2.zero; // 패널 왼쪽 아래 기준
            rect.pivot = Vector2.zero; // 버튼 왼쪽 아래 기준
            rect.anchoredPosition = new Vector2(14f + index % 2 * 168f, 92f - index / 2 * 38f); // 두 열의 버튼 배치
            rect.sizeDelta = new Vector2(158f, 32f); // 클릭 영역 크기
            host.GetComponent<Image>().color = new Color(0.24f, 0.2f, 0.1f, 1f); // 금색 계열 버튼 배경
            var button = host.GetComponent<Button>(); // 실제 버튼 참조
            button.interactable = enabled; // 턴 권한에 맞춘 입력 허용
            button.onClick.AddListener(action); // 버튼 동작 연결
            var text = NewText("Label", host.transform); // 버튼 문구 생성
            text.text = label; // 버튼 이름 표시
            text.alignment = TextAnchor.MiddleCenter; // 중앙 정렬
            text.rectTransform.anchorMin = Vector2.zero; // 전체 버튼 문구 시작
            text.rectTransform.anchorMax = Vector2.one; // 전체 버튼 문구 끝
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero; // 문구 여백 제거
            _buttons.Add(button); // 생성 버튼 목록에 등록
        } // 범위 종료

        private void EnsureUI() // 능력 패널 생성
        { // 범위 시작
            var canvasObject = new GameObject("PieceAbilityOverlayCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 독립 UI 캔버스
            canvasObject.transform.SetParent(transform, false); // UI 루트에 연결
            var canvas = canvasObject.GetComponent<Canvas>(); // 캔버스 참조
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 좌표 UI
            canvas.sortingOrder = 96; // 기존 패널 표시 순서 유지
            var scaler = canvasObject.GetComponent<CanvasScaler>(); // 해상도 비율 조절
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 배율
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기존 UI 기준 해상도
            scaler.matchWidthOrHeight = 0.5f; // 가로와 세로 배율 절충
            _panel = new GameObject("AbilityPanel", typeof(RectTransform), typeof(Image)); // 능력 설명 패널 생성
            _panel.transform.SetParent(canvasObject.transform, false); // 캔버스에 패널 연결
            var rect = _panel.GetComponent<RectTransform>(); // 패널 위치 참조
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f); // 화면 오른쪽 중앙 기준
            rect.pivot = new Vector2(1f, 0.5f); // 패널 오른쪽 중앙 기준
            rect.anchoredPosition = new Vector2(-10f, -120f); // 기존 우측 위치 유지
            rect.sizeDelta = new Vector2(360f, 370f); // 설명과 선택 버튼 영역 확보
            _panel.GetComponent<Image>().color = new Color(0.06f, 0.07f, 0.1f, 0.94f); // 배경 대비 확보
            _panel.GetComponent<Image>().raycastTarget = true; // 패널 뒤 보드 클릭 차단
            _text = NewText("AbilityText", _panel.transform); // 설명 텍스트 생성
            _text.alignment = TextAnchor.UpperLeft; // 설명 왼쪽 위 정렬
            _text.rectTransform.anchorMin = new Vector2(0f, 1f); // 패널 위쪽 기준
            _text.rectTransform.anchorMax = new Vector2(1f, 1f); // 패널 가로 폭 사용
            _text.rectTransform.pivot = new Vector2(0.5f, 1f); // 설명 상단 기준
            _text.rectTransform.anchoredPosition = new Vector2(0f, -12f); // 상단 여백
            _text.rectTransform.sizeDelta = new Vector2(-28f, 226f); // 버튼 위 설명 영역
            _panel.SetActive(false); // 초기 선택 없음
        } // 범위 종료

        private static Text NewText(string name, Transform parent) // 공통 한글 텍스트 생성
        { // 범위 시작
            var host = new GameObject(name, typeof(RectTransform), typeof(Text)); // 텍스트 오브젝트 생성
            host.transform.SetParent(parent, false); // 부모 UI 연결
            var text = host.GetComponent<Text>(); // 텍스트 컴포넌트 조회
            if (_font == null) // 글꼴 미설정 확인
            { // 범위 시작
                _font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 20); // 한글 지원 글꼴 선택
                if (_font == null) // 시스템 글꼴 없음 확인
                { // 범위 시작
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 기본 글꼴 대체
                } // 범위 종료
            } // 범위 종료
            text.font = _font; // 글꼴 적용
            text.fontSize = 15; // 설명 글자 크기
            text.color = Color.white; // 글자 대비 확보
            text.horizontalOverflow = HorizontalWrapMode.Wrap; // 긴 문구 자동 줄바꿈
            text.verticalOverflow = VerticalWrapMode.Truncate; // 패널 밖 문구 차단
            text.raycastTarget = false; // 버튼 클릭 전달
            return text; // 완성 텍스트 반환
        } // 범위 종료

        private void OnDestroy() // UI 종료 처리
        { // 범위 시작
            if (_boardInput != null) // 입력 연결 확인
            { // 범위 시작
                _boardInput.SelectionChanged -= Refresh; // 선택 이벤트 구독 해제
            } // 범위 종료
        } // 범위 종료
    } // 범위 종료
} // 범위 종료
