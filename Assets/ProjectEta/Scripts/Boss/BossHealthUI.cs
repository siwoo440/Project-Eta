using System.Collections.Generic; // HashSet<T>를 사용하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour, GameObject, Font, Color, Vector2 등을 사용하기 위한 네임스페이스
using UnityEngine.UI; // Canvas, CanvasScaler, Image, Text, Shadow를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceRuntimeState와 PieceCategory를 사용하기 위한 네임스페이스

namespace ProjectEta.Boss // 보스 전투 관련 타입을 모아두는 네임스페이스
{
    public sealed class BossHealthUI : MonoBehaviour // 살아 있는 적 Boss의 현재 HP를 화면 상단에 항상 표시하는 런타임 UI
    {
        private const float MissingBossScanInterval = 0.25f; // 아직 보스를 찾지 못했을 때만 보드 재탐색하는 간격

        private BoardState _board; // 현재 Battle 씬의 실제 보드 상태
        private PieceRuntimeState _trackedBoss; // 현재 UI가 추적 중인 살아 있는 보스 한 기
        private Canvas _canvas; // 보스 체력 전용 Screen Space Overlay Canvas
        private Text _label; // 보스 이름과 현재/최대 HP를 표시하는 텍스트
        private Image _healthFill; // 0~1 비율로 줄어드는 체력바 전경
        private int _lastHp = int.MinValue; // 불필요한 UI 갱신을 막기 위한 마지막 현재 HP
        private int _lastMaxHp = int.MinValue; // 불필요한 UI 갱신을 막기 위한 마지막 최대 HP
        private float _nextMissingBossScanTime; // 보스가 없을 때 다음 보드 탐색 시간

        public PieceRuntimeState TrackedBoss => _trackedBoss; // 현재 표시 중인 보스를 테스트·디버그에서 읽는 프로퍼티
        public string DisplayText => _label != null ? _label.text : string.Empty; // 현재 표시 문자열을 읽는 프로퍼티

        public void Bind(BoardState board) // 현재 Battle 보드를 연결하고 즉시 보스 탐색을 시작하는 메서드
        {
            _board = board; // 실제 보드 참조 저장
            _trackedBoss = null; // 이전 전투에서 추적하던 보스 참조 초기화
            _lastHp = int.MinValue; // 다음 표시에서 강제 갱신
            _lastMaxHp = int.MinValue; // 다음 표시에서 강제 갱신
            FindAndShowBoss(); // 첫 프레임부터 가능한 한 빨리 보스 HP를 표시
        }

        public void Show(PieceRuntimeState boss) // 외부 전투 훅이 알고 있는 보스를 즉시 UI에 반영하는 메서드
        {
            if (!IsAliveEnemyBoss(boss)) // 죽었거나 보스가 아닌 대상이면
            {
                Hide(); // 잘못된 체력 표시를 남기지 않음
                return; // 더 처리하지 않음
            }

            _trackedBoss = boss; // 새 추적 보스 저장
            EnsureCanvas(); // 최초 호출이면 UI 오브젝트 생성
            RefreshVisual(true); // 현재 HP를 즉시 강제 반영
        }

        public void Hide() // 살아 있는 보스가 없을 때 체력 UI를 숨기는 메서드
        {
            _trackedBoss = null; // 추적 대상 제거
            _lastHp = int.MinValue; // 다음 보스가 등장하면 다시 갱신하도록 초기화
            _lastMaxHp = int.MinValue; // 다음 보스가 등장하면 다시 갱신하도록 초기화
            if (_canvas != null) _canvas.enabled = false; // 기존 UI 오브젝트는 재사용을 위해 유지하고 화면에서만 숨김
        }

        private void Update() // 보스 HP 변화와 늦게 생성되는 보스를 가벼운 방식으로 추적하는 메서드
        {
            if (_board == null) return; // 현재 Battle 보드가 없으면 처리할 수 없음

            if (!IsAliveEnemyBoss(_trackedBoss)) // 아직 보스를 못 찾았거나 추적 보스가 사망했다면
            {
                if (Time.unscaledTime < _nextMissingBossScanTime) return; // 0.25초 간격 전에는 10x10 보드를 다시 훑지 않음
                _nextMissingBossScanTime = Time.unscaledTime + MissingBossScanInterval; // 다음 탐색 시간 예약
                FindAndShowBoss(); // 자동 스포너처럼 늦게 등장한 보스 탐색
                return; // 이번 프레임 처리 종료
            }

            RefreshVisual(false); // 살아 있는 보스는 HP 숫자가 실제로 바뀐 경우에만 UI 갱신
        }

        private void FindAndShowBoss() // 현재 보드에서 살아 있는 적 Boss 한 기를 결정론적으로 찾는 메서드
        {
            if (_board == null) // 보드 연결이 없으면
            {
                Hide(); // UI 숨김
                return; // 탐색 불가
            }

            var visited = new HashSet<PieceRuntimeState>(); // 2x2 네 칸이 같은 보스를 가리켜도 한 번만 검사하기 위한 집합

            for (int y = 0; y < BoardState.Height; y++) // Y 우선 순회로 같은 상황에서 항상 같은 보스를 선택
            {
                for (int x = 0; x < BoardState.Width; x++) // X 방향 순회
                {
                    PieceRuntimeState piece = _board.GetTile(new Vector2Int(x, y))?.OccupyingPiece; // 현재 칸 점유 기물 조회
                    if (piece == null || !visited.Add(piece)) continue; // 빈 칸과 같은 대형 기물 중복 칸 제외
                    if (!IsAliveEnemyBoss(piece)) continue; // 살아 있는 적 보스가 아니면 제외
                    Show(piece); // 첫 보스를 즉시 추적·표시
                    return; // 한 체력바에서는 보스 한 기만 표시
                }
            }

            Hide(); // 살아 있는 보스가 하나도 없으면 체력 UI 숨김
        }

        private void RefreshVisual(bool force) // 현재 추적 보스의 HP 숫자와 체력바를 필요할 때만 갱신하는 메서드
        {
            if (!IsAliveEnemyBoss(_trackedBoss)) // 갱신 직전 보스가 사망했으면
            {
                Hide(); // 체력 UI 제거
                return; // 추가 계산 중단
            }

            int currentHp = Mathf.Max(0, _trackedBoss.CurrentHp); // 현재 HP를 음수 없이 읽음
            int maxHp = _trackedBoss.Definition != null ? Mathf.Max(0, _trackedBoss.Definition.BaseHp) : 0; // 정의의 최대 HP 읽기
            if (!force && currentHp == _lastHp && maxHp == _lastMaxHp) return; // 값이 그대로면 Text/Image 갱신 비용을 만들지 않음

            EnsureCanvas(); // UI가 아직 없다면 생성
            if (_canvas == null || _label == null || _healthFill == null) return; // UI 생성 실패 시 안전 종료

            _canvas.enabled = true; // 살아 있는 보스가 있으므로 UI 표시
            _label.text = BuildDisplayText(_trackedBoss.Definition.DisplayName, currentHp, maxHp); // 보스 이름과 HP 숫자 갱신
            _healthFill.fillAmount = CalculateHealth01(currentHp, maxHp); // 체력 비율을 0~1로 반영

            _lastHp = currentHp; // 이번 현재 HP를 캐시
            _lastMaxHp = maxHp; // 이번 최대 HP를 캐시
        }

        public static string BuildDisplayText(string bossName, int currentHp, int maxHp) // 런타임과 테스트가 공유하는 보스 HP 표시 문구 생성 함수
        {
            string safeName = string.IsNullOrWhiteSpace(bossName) ? "Boss" : bossName; // 표시 이름이 비어 있으면 기본 이름 사용
            return $"BOSS    {safeName}    HP {Mathf.Max(0, currentHp)} / {Mathf.Max(0, maxHp)}"; // 현재/최대 HP를 한 줄로 명확하게 표시
        }

        public static float CalculateHealth01(int currentHp, int maxHp) // 체력바 fillAmount에 사용할 안전한 0~1 비율 계산 함수
        {
            if (maxHp <= 0) return 0f; // 최대 HP가 잘못된 경우 0으로 처리
            return Mathf.Clamp01((float)currentHp / maxHp); // 현재 HP를 최대 HP로 나누고 0~1 범위로 제한
        }

        private static bool IsAliveEnemyBoss(PieceRuntimeState piece) // UI 추적 대상이 살아 있는 적 Boss인지 검사하는 메서드
        {
            if (piece?.Definition == null) return false; // 정의 없는 기물은 보스 UI 대상이 아님
            if (piece.IsPlayerPiece || piece.IsDead) return false; // 플레이어 기물 또는 사망 기물 제외
            return piece.Definition.Category == PieceCategory.Boss; // Boss 카테고리만 표시
        }

        private void EnsureCanvas() // 기존 전투 상단 UI 아래에 보스 체력 패널과 체력바를 런타임 생성하는 메서드
        {
            if (_canvas != null) return; // 이미 생성했으면 재사용

            var canvasObject = new GameObject("BossHealthCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 보스 체력 전용 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 컨트롤러 오브젝트의 자식으로 연결
            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 확보
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 좌표 기준 렌더링
            _canvas.sortingOrder = 98; // TurnStatusUI·RoundSummaryUI 아래 계층에서 표시
            _canvas.enabled = false; // 보스를 찾기 전에는 숨김

            var scaler = canvasObject.GetComponent<CanvasScaler>(); // 해상도 대응 스케일러 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기에 따라 자동 스케일
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기존 UI와 동일 기준 해상도
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 가로·세로 모두 고려
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 동일 비중

            var panelObject = new GameObject("BossHealthPanel", typeof(RectTransform), typeof(Image)); // 체력 UI 전체 배경 패널 생성
            panelObject.transform.SetParent(canvasObject.transform, false); // Canvas 자식 연결
            var panelRect = panelObject.GetComponent<RectTransform>(); // 패널 위치·크기 제어용 RectTransform
            panelRect.anchorMin = new Vector2(0.5f, 1f); // 화면 상단 중앙 앵커
            panelRect.anchorMax = new Vector2(0.5f, 1f); // 화면 상단 중앙 앵커
            panelRect.pivot = new Vector2(0.5f, 1f); // 윗중앙 기준 배치
            panelRect.anchoredPosition = new Vector2(0f, -146f); // RoundSummaryUI 바로 아래에 배치
            panelRect.sizeDelta = new Vector2(620f, 54f); // 이름·HP·체력바가 들어가는 폭과 높이

            var panelImage = panelObject.GetComponent<Image>(); // 패널 배경 이미지 조회
            panelImage.color = new Color(0.12f, 0.015f, 0.015f, 0.94f); // 어두운 보스 전용 배경
            panelImage.raycastTarget = false; // 보드 클릭을 가로채지 않음

            var barBackgroundObject = new GameObject("BossHealthBarBackground", typeof(RectTransform), typeof(Image)); // 체력바 배경 생성
            barBackgroundObject.transform.SetParent(panelObject.transform, false); // 패널 자식 연결
            var barBackgroundRect = barBackgroundObject.GetComponent<RectTransform>(); // 배경 바 RectTransform 조회
            barBackgroundRect.anchorMin = new Vector2(0.03f, 0.10f); // 좌측 3%, 하단 10%
            barBackgroundRect.anchorMax = new Vector2(0.97f, 0.34f); // 우측 97%, 높이 34%까지
            barBackgroundRect.offsetMin = Vector2.zero; // 앵커 그대로 사용
            barBackgroundRect.offsetMax = Vector2.zero; // 앵커 그대로 사용
            var barBackgroundImage = barBackgroundObject.GetComponent<Image>(); // 체력바 배경 Image 조회
            barBackgroundImage.color = new Color(0.08f, 0.08f, 0.08f, 1f); // 남은 체력이 줄었을 때 보이는 짙은 배경
            barBackgroundImage.raycastTarget = false; // 입력 차단 금지

            var barFillObject = new GameObject("BossHealthBarFill", typeof(RectTransform), typeof(Image)); // 실제 현재 HP를 표현하는 전경 바 생성
            barFillObject.transform.SetParent(barBackgroundObject.transform, false); // 체력바 배경 자식 연결
            var barFillRect = barFillObject.GetComponent<RectTransform>(); // 전경 바 RectTransform 조회
            barFillRect.anchorMin = Vector2.zero; // 배경 전체를 채움
            barFillRect.anchorMax = Vector2.one; // 배경 전체를 채움
            barFillRect.offsetMin = new Vector2(2f, 2f); // 내부 테두리 여백
            barFillRect.offsetMax = new Vector2(-2f, -2f); // 내부 테두리 여백
            _healthFill = barFillObject.GetComponent<Image>(); // 실제 fillAmount를 바꿀 Image 저장
            _healthFill.color = new Color(0.78f, 0.05f, 0.04f, 1f); // 보스 HP를 나타내는 붉은색
            _healthFill.type = Image.Type.Filled; // fillAmount 기반으로 줄어드는 타입 사용
            _healthFill.fillMethod = Image.FillMethod.Horizontal; // 좌우 방향 체력바
            _healthFill.fillOrigin = 0; // 왼쪽에서 오른쪽으로 채움
            _healthFill.fillAmount = 1f; // 생성 시 만피 상태
            _healthFill.raycastTarget = false; // 입력 차단 금지

            var textObject = new GameObject("BossHealthText", typeof(RectTransform), typeof(Text), typeof(Shadow)); // 보스 이름·HP 숫자 텍스트 생성
            textObject.transform.SetParent(panelObject.transform, false); // 패널 자식 연결
            var textRect = textObject.GetComponent<RectTransform>(); // 텍스트 RectTransform 조회
            textRect.anchorMin = new Vector2(0f, 0.34f); // 체력바 위 영역부터 시작
            textRect.anchorMax = Vector2.one; // 패널 상단까지 사용
            textRect.offsetMin = new Vector2(12f, 0f); // 좌측 여백
            textRect.offsetMax = new Vector2(-12f, 0f); // 우측 여백

            _label = textObject.GetComponent<Text>(); // 실제 Text 컴포넌트 저장
            _label.font = CreateRuntimeFont(); // 한글 지원 시스템 폰트 우선 사용
            _label.fontSize = 20; // 전투 중 빠르게 읽을 수 있는 크기
            _label.fontStyle = FontStyle.Bold; // 보스 UI 강조
            _label.alignment = TextAnchor.MiddleCenter; // 상단 중앙 한 줄 정렬
            _label.color = new Color(1f, 0.91f, 0.84f, 1f); // 어두운 배경 위 밝은 글자
            _label.raycastTarget = false; // 입력 차단 금지

            var shadow = textObject.GetComponent<Shadow>(); // 텍스트 그림자 조회
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f); // 배경과 글자 대비 강화
            shadow.effectDistance = new Vector2(1f, -1f); // 작은 그림자 거리
            shadow.useGraphicAlpha = true; // 원본 알파 반영
        }

        private static Font CreateRuntimeFont() // 기존 전투 UI와 같은 한글 우선 폰트 폴백 규칙
        {
            Font systemFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 20); // 한국어 시스템 폰트 우선 탐색
            if (systemFont != null) return systemFont; // 사용 가능한 시스템 폰트가 있으면 반환
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 마지막으로 Unity 기본 런타임 폰트 사용
        }
    }
}
