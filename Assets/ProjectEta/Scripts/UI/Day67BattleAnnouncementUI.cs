using System.Collections; // 알림 페이드 코루틴 사용
using UnityEngine; // MonoBehaviour·GameObject·Color 사용
using UnityEngine.UI; // Canvas·CanvasGroup·Image·Text 사용

namespace ProjectEta.UI
{
    [DefaultExecutionOrder(1590)]
    public sealed class Day67BattleAnnouncementUI : MonoBehaviour
    {
        private const float FadeInDuration = 0.13f; // 알림 등장 시간
        private const float FadeOutDuration = 0.18f; // 알림 퇴장 시간

        private readonly Day67AnnouncementQueue _queue = new Day67AnnouncementQueue(); // 공통 순차 알림 큐
        private Canvas _canvas; // 알림 전용 화면 Canvas
        private CanvasGroup _group; // 전체 알림 투명도 제어
        private RectTransform _panelRect; // 중앙 알림 패널 위치·스케일
        private Image _panelImage; // 종류별 패널 배경
        private Image _accentImage; // 종류별 강조선
        private Text _titleText; // 큰 중앙 문구
        private Text _subtitleText; // 보조 문구
        private Coroutine _playRoutine; // 현재 재생 알림 코루틴
        private static Font _runtimeFont; // 한글 대응 런타임 폰트 캐시

        public bool IsBusy => _playRoutine != null || _queue.Count > 0; // 현재 재생·대기 상태 공개
        public int PendingCount => _queue.Count; // 테스트·디버그용 대기 수 공개

        private void Awake()
        {
            EnsureUI(); // 알림 UI 선생성
        }

        private void Update()
        {
            if (_playRoutine != null || _queue.Count == 0) return; // 현재 재생·대기 없음 처리 생략
            _playRoutine = StartCoroutine(PlayQueue()); // 등록 알림 순차 재생 시작
        }

        public bool Enqueue(Day67BattleAnnouncement announcement)
        {
            EnsureUI(); // 외부 호출 시 UI 준비 보장
            return _queue.Enqueue(announcement); // 공통 큐에 알림 등록
        }

        public void ClearPending()
        {
            _queue.Clear(); // 아직 표시하지 않은 알림 제거
        }

        private IEnumerator PlayQueue()
        {
            while (_queue.TryDequeue(out Day67BattleAnnouncement announcement))
            {
                ApplyAnnouncement(announcement); // 현재 알림 문구·색상 적용
                yield return Fade(0f, 1f, FadeInDuration, 0.94f, 1f); // 짧은 확대와 함께 등장
                yield return WaitUnscaled(announcement.HoldSeconds); // 알림 종류별 유지
                yield return Fade(1f, 0f, FadeOutDuration, 1f, 1.035f); // 살짝 확대하며 퇴장
                _group.alpha = 0f; // 다음 알림 전 완전 숨김
            }

            _playRoutine = null; // 전체 대기열 재생 완료 기록
        }

        private void ApplyAnnouncement(Day67BattleAnnouncement announcement)
        {
            _titleText.text = announcement.Title; // 큰 제목 반영
            _subtitleText.text = announcement.Subtitle; // 보조 문구 반영
            Color accent = ResolveAccent(announcement.Kind); // 알림 종류 강조색 계산
            _accentImage.color = accent; // 하단 강조선 색 적용
            _titleText.color = accent; // 큰 문구 강조색 적용
            _panelImage.color = ResolvePanelColor(announcement.Kind); // 종류별 어두운 배경 적용
        }

        private IEnumerator Fade(float fromAlpha, float toAlpha, float duration, float fromScale, float toScale)
        {
            float elapsed = 0f; // 현재 페이드 진행 시간

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime; // 게임 시간 정지와 무관한 연출 진행
                float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration); // 정규화 진행률 계산
                float eased = 1f - Mathf.Pow(1f - t, 3f); // 빠르게 붙는 ease-out 적용
                _group.alpha = Mathf.Lerp(fromAlpha, toAlpha, eased); // 전체 투명도 보간
                float scale = Mathf.Lerp(fromScale, toScale, eased); // 중앙 패널 확대 보간
                _panelRect.localScale = new Vector3(scale, scale, 1f); // 균일 확대 적용
                yield return null; // 다음 프레임까지 대기
            }

            _group.alpha = toAlpha; // 최종 투명도 고정
            _panelRect.localScale = new Vector3(toScale, toScale, 1f); // 최종 스케일 고정
        }

        private static IEnumerator WaitUnscaled(float seconds)
        {
            float endTime = Time.unscaledTime + Mathf.Max(0f, seconds); // 실제 시간 기준 종료 시점 계산
            while (Time.unscaledTime < endTime) yield return null; // 지정 시간 동안 프레임 대기
        }

        private void EnsureUI()
        {
            if (_canvas != null) return; // 중복 Canvas 생성 차단

            GameObject canvasObject = new GameObject("Day67BattleAnnouncementCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 알림 전용 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 현재 호스트 자식 연결
            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 저장
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 고정 알림 사용
            _canvas.sortingOrder = UiLayerOrder.BattleAnnouncement; // 공통 전투 알림 계층 적용

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // 화면 스케일러 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 기준 해상도 기반 크기 적용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 프로젝트 공통 기준 해상도 사용
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 보정

            GameObject panelObject = new GameObject("AnnouncementPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image)); // 중앙 알림 패널 생성
            panelObject.transform.SetParent(canvasObject.transform, false); // Canvas 자식 배치
            _panelRect = panelObject.GetComponent<RectTransform>(); // 패널 RectTransform 저장
            SetRect(_panelRect, Vector2.zero, new Vector2(980f, 190f)); // 화면 중앙 넓은 배너 배치
            _group = panelObject.GetComponent<CanvasGroup>(); // 페이드 그룹 저장
            _group.alpha = 0f; // 시작 시 숨김
            _group.blocksRaycasts = false; // 전투 입력 방해 금지
            _group.interactable = false; // 알림 자체 입력 없음
            _panelImage = panelObject.GetComponent<Image>(); // 배경 이미지 저장
            _panelImage.raycastTarget = false; // 보드 입력 차단 금지

            _titleText = CreateText("Title", panelObject.transform, 52, FontStyle.Bold); // 큰 제목 생성
            SetRect(_titleText.rectTransform, new Vector2(0f, 24f), new Vector2(900f, 74f)); // 제목 상단 배치

            _subtitleText = CreateText("Subtitle", panelObject.transform, 22, FontStyle.Bold); // 보조 문구 생성
            _subtitleText.color = new Color(0.85f, 0.88f, 0.93f, 1f); // 보조 문구 밝기 설정
            SetRect(_subtitleText.rectTransform, new Vector2(0f, -38f), new Vector2(880f, 42f)); // 보조 문구 하단 배치

            GameObject accentObject = new GameObject("Accent", typeof(RectTransform), typeof(Image)); // 하단 강조선 생성
            accentObject.transform.SetParent(panelObject.transform, false); // 패널 자식 배치
            RectTransform accentRect = accentObject.GetComponent<RectTransform>(); // 강조선 위치 제어
            SetRect(accentRect, new Vector2(0f, -82f), new Vector2(520f, 5f)); // 중앙 하단 짧은 선 배치
            _accentImage = accentObject.GetComponent<Image>(); // 강조선 이미지 저장
            _accentImage.raycastTarget = false; // 입력 간섭 제거
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle style)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow)); // 공통 알림 Text 생성
            textObject.transform.SetParent(parent, false); // 요청 부모 연결
            Text text = textObject.GetComponent<Text>(); // Text 컴포넌트 조회
            text.font = GetRuntimeFont(); // 한글 대응 폰트 적용
            text.fontSize = fontSize; // 요청 글자 크기 적용
            text.fontStyle = style; // 요청 글자 스타일 적용
            text.alignment = TextAnchor.MiddleCenter; // 중앙 정렬 적용
            text.color = Color.white; // 기본 글자색 적용
            text.raycastTarget = false; // 전투 입력 간섭 제거
            Shadow shadow = textObject.GetComponent<Shadow>(); // 글자 그림자 조회
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f); // 배경 위 대비 강화
            shadow.effectDistance = new Vector2(2f, -2f); // 짧은 그림자 적용
            return text; // 완성 Text 반환
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f); // 화면 중앙 앵커 적용
            rect.anchorMax = new Vector2(0.5f, 0.5f); // 화면 중앙 앵커 고정
            rect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 적용
            rect.anchoredPosition = position; // 요청 위치 반영
            rect.sizeDelta = size; // 요청 크기 반영
        }

        private static Color ResolveAccent(Day67AnnouncementKind kind)
        {
            switch (kind)
            {
                case Day67AnnouncementKind.Victory:
                case Day67AnnouncementKind.RunCompleted:
                    return new Color(0.98f, 0.80f, 0.28f, 1f); // 승리·완료 금색
                case Day67AnnouncementKind.Defeat:
                case Day67AnnouncementKind.RunFailed:
                case Day67AnnouncementKind.BossPhase:
                    return new Color(1f, 0.26f, 0.20f, 1f); // 패배·보스 경고 적색
                case Day67AnnouncementKind.Passive:
                    return new Color(0.48f, 0.82f, 1f, 1f); // 패시브 청색
                case Day67AnnouncementKind.EnemyTurn:
                    return new Color(1f, 0.52f, 0.30f, 1f); // 적 턴 주황색
                default:
                    return new Color(0.74f, 0.92f, 1f, 1f); // 일반 전투 알림 밝은 청색
            }
        }

        private static Color ResolvePanelColor(Day67AnnouncementKind kind)
        {
            if (kind == Day67AnnouncementKind.BossPhase || kind == Day67AnnouncementKind.Defeat || kind == Day67AnnouncementKind.RunFailed)
            {
                return new Color(0.15f, 0.02f, 0.025f, 0.92f); // 위험 알림 짙은 적색 배경
            }

            return new Color(0.025f, 0.035f, 0.052f, 0.92f); // 일반 알림 어두운 청회색 배경
        }

        private static Font GetRuntimeFont()
        {
            if (_runtimeFont != null) return _runtimeFont; // 기존 런타임 폰트 재사용
            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 28); // 시스템 한글 폰트 생성
            if (_runtimeFont == null) _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 fallback
            return _runtimeFont; // 최종 폰트 반환
        }
    }
}
