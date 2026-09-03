using UnityEngine; // MonoBehaviour, GameObject, RectTransform, Font 등을 사용하기 위한 네임스페이스
using UnityEngine.UI; // CanvasScaler, GraphicRaycaster, Image, Text를 사용하기 위한 네임스페이스

namespace ProjectEta.Battle // 전투 턴 관련 타입을 모아두는 네임스페이스
{
    public class TurnStatusUI : MonoBehaviour // 화면 상단 중앙에 현재 턴 상태를 표시하는 런타임 Canvas UI 컴포넌트
    {
        public Canvas StatusCanvas => _statusCanvas; // 테스트와 외부 코드에서 생성된 Canvas를 확인하기 위한 프로퍼티
        public RectTransform PanelRect => _panelRect; // 테스트와 외부 코드에서 상단 패널 위치를 확인하기 위한 프로퍼티
        public string DisplayText => _label != null ? _label.text : string.Empty; // 현재 화면에 표시 중인 턴 문구

        private TurnManager _turnManager; // 표시할 실제 턴 상태를 제공하는 턴 매니저
        private Canvas _statusCanvas; // Screen Space Overlay 방식의 턴 상태 전용 Canvas
        private RectTransform _panelRect; // 화면 상단 중앙에 배치되는 상태 패널 RectTransform
        private Text _label; // 턴 번호와 현재 턴 주체를 표시하는 텍스트

        public void Bind(TurnManager turnManager) // 턴 매니저를 UI에 연결하고 즉시 현재 상태를 표시하는 메서드
        {
            if (_turnManager != null) // 이전 턴 매니저가 연결돼 있었다면
            {
                _turnManager.TurnChanged -= HandleTurnChanged; // 이전 이벤트 구독을 해제
            }

            _turnManager = turnManager; // 새 턴 매니저 참조 저장

            if (_turnManager == null) // 잘못된 null 턴 매니저가 전달됐다면
            {
                return; // UI 연결을 진행하지 않음
            }

            EnsureCanvas(); // 아직 Canvas가 없으면 상단 중앙 Canvas UI를 런타임 생성
            _turnManager.TurnChanged += HandleTurnChanged; // 턴 변경 이벤트를 구독
            Refresh(_turnManager.CurrentState, _turnManager.TurnNumber); // 현재 턴 상태를 즉시 표시
        }

        private void EnsureCanvas() // 화면 상단 중앙 턴 UI 구조를 한 번만 생성하는 메서드
        {
            if (_statusCanvas != null) // 이미 Canvas가 만들어져 있다면
            {
                return; // 중복 생성하지 않음
            }

            var canvasObject = new GameObject("TurnStatusCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 턴 상태 전용 Canvas 오브젝트 생성
            canvasObject.transform.SetParent(transform, false); // BattleController 오브젝트의 자식으로 연결
            _statusCanvas = canvasObject.GetComponent<Canvas>(); // 생성한 Canvas 컴포넌트 참조 확보
            _statusCanvas.renderMode = RenderMode.ScreenSpaceOverlay; // 카메라와 무관하게 화면 위에 표시
            _statusCanvas.sortingOrder = 100; // 다른 기본 UI보다 앞에 보이도록 높은 정렬 순서 지정

            var scaler = canvasObject.GetComponent<CanvasScaler>(); // 해상도 대응용 CanvasScaler 참조 확보
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기에 맞춰 UI 크기를 보정
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도 설정
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 가로·세로 비율을 함께 고려하는 방식 사용
            scaler.matchWidthOrHeight = 0.5f; // 가로와 세로를 동일 비중으로 보정

            var panelObject = new GameObject("TurnStatusPanel", typeof(RectTransform), typeof(Image)); // 반투명 배경 패널 오브젝트 생성
            panelObject.transform.SetParent(canvasObject.transform, false); // Canvas의 자식으로 연결
            _panelRect = panelObject.GetComponent<RectTransform>(); // 패널 RectTransform 참조 확보
            _panelRect.anchorMin = new Vector2(0.5f, 1f); // 최소 앵커를 화면 상단 중앙에 고정
            _panelRect.anchorMax = new Vector2(0.5f, 1f); // 최대 앵커도 화면 상단 중앙에 고정
            _panelRect.pivot = new Vector2(0.5f, 1f); // 패널의 윗중앙을 기준점으로 사용
            _panelRect.anchoredPosition = new Vector2(0f, -24f); // 화면 위쪽에서 24픽셀 아래로 배치
            _panelRect.sizeDelta = new Vector2(360f, 58f); // 턴 문구가 충분히 보이는 패널 크기 지정

            var panelImage = panelObject.GetComponent<Image>(); // 패널 배경 Image 참조 확보
            panelImage.color = new Color(0f, 0f, 0f, 0.72f); // 화면을 가리지 않는 반투명 검정 배경 적용
            panelImage.raycastTarget = false; // 턴 표시 UI가 보드 클릭을 가로채지 않도록 레이캐스트 비활성화

            var textObject = new GameObject("TurnStatusText", typeof(RectTransform), typeof(Text)); // 실제 턴 상태 텍스트 오브젝트 생성
            textObject.transform.SetParent(panelObject.transform, false); // 배경 패널의 자식으로 연결
            var textRect = textObject.GetComponent<RectTransform>(); // 텍스트 RectTransform 참조 확보
            textRect.anchorMin = Vector2.zero; // 패널 좌하단부터 시작하도록 설정
            textRect.anchorMax = Vector2.one; // 패널 우상단까지 늘어나도록 설정
            textRect.offsetMin = Vector2.zero; // 왼쪽·아래 여백 없이 패널 전체 사용
            textRect.offsetMax = Vector2.zero; // 오른쪽·위 여백 없이 패널 전체 사용

            _label = textObject.GetComponent<Text>(); // 생성한 Text 컴포넌트 참조 확보
            _label.font = CreateRuntimeFont(); // 한글 표시가 가능한 시스템 폰트를 우선 생성해 연결
            _label.fontSize = 24; // 상단에서 쉽게 읽을 수 있는 글자 크기 설정
            _label.fontStyle = FontStyle.Bold; // 턴 상태를 강조하기 위해 굵게 표시
            _label.alignment = TextAnchor.MiddleCenter; // 문구를 패널 정중앙에 배치
            _label.color = Color.white; // 어두운 배경 위에서 읽기 쉬운 흰색 적용
            _label.raycastTarget = false; // 텍스트도 마우스 레이캐스트를 막지 않도록 설정
        }

        private static Font CreateRuntimeFont() // Windows 한글을 우선 지원하면서 다른 환경에서도 동작하도록 폰트를 찾는 메서드
        {
            var systemFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 24); // 운영체제의 한글·기본 폰트 후보로 동적 폰트 생성 시도
            if (systemFont != null) // 시스템 폰트를 정상적으로 만들었다면
            {
                return systemFont; // 해당 폰트를 사용
            }

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 시스템 폰트를 못 찾았을 때 Unity 기본 런타임 폰트 사용
        }

        private void HandleTurnChanged(TurnState state, int turnNumber) // TurnManager 이벤트를 받아 UI를 갱신하는 콜백
        {
            Refresh(state, turnNumber); // 전달받은 상태와 턴 번호로 화면 문구 갱신
        }

        private void Refresh(TurnState state, int turnNumber) // 현재 턴 상태를 사람이 읽기 쉬운 문구로 표시하는 메서드
        {
            if (_label == null) // 아직 Text가 생성되지 않았다면
            {
                return; // 표시할 대상이 없으므로 종료
            }

            _label.text = BuildLabel(state, turnNumber); // 상태와 턴 번호를 조합한 최종 문구를 화면에 적용
        }

        private string BuildLabel(TurnState state, int turnNumber) // TurnState를 한국어 UI 문구로 변환하는 메서드(13일차: 전투 종료 시 승리/패배 구분을 위해 인스턴스 메서드로 변경)
        {
            switch (state) // 현재 턴 상태에 따라 표시 문구를 선택
            {
                case TurnState.PlayerTurn: // 플레이어 턴이면
                    return $"{turnNumber}턴 · 플레이어 턴"; // 플레이어 턴 문구 반환
                case TurnState.EnemyTurn: // 적 턴이면
                    return $"{turnNumber}턴 · 적 턴"; // 적 턴 문구 반환
                case TurnState.BattleEnded when _turnManager?.Outcome == BattleOutcome.Victory: // 전투가 승리로 끝났으면
                    return $"{turnNumber}턴 · 승리"; // 승리 문구 반환
                case TurnState.BattleEnded when _turnManager?.Outcome == BattleOutcome.Defeat: // 전투가 패배로 끝났으면
                    return $"{turnNumber}턴 · 패배"; // 패배 문구 반환
                default: // 결과가 아직 없는 전투 종료 상태라면
                    return $"{turnNumber}턴 · 전투 종료"; // 기존 일반 전투 종료 문구 반환
            }
        }

        private void OnDestroy() // UI 호스트가 제거될 때 호출되는 정리 메서드
        {
            if (_turnManager != null) // 연결된 턴 매니저가 남아 있다면
            {
                _turnManager.TurnChanged -= HandleTurnChanged; // 이벤트 구독을 해제해 불필요한 참조를 제거
            }
        }
    }
}
