using System.Collections; // IEnumerator 코루틴을 사용하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour와 화면 좌표 계산을 사용하기 위한 네임스페이스
using UnityEngine.SceneManagement; // 현재 Scene 이름 확인을 사용하기 위한 네임스페이스
using UnityEngine.UI; // Canvas와 Text UI를 사용하기 위한 네임스페이스
using ProjectEta.Battle; // BattleController와 BattleHooks를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardView와 BoardState를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.UI // 프로젝트 η 런타임 UI 타입을 모아두는 네임스페이스
{
    public class CombatFloatingTextUI : MonoBehaviour // 피해 발생 위치 위에 숫자를 잠깐 표시하는 전투 피드백 컴포넌트
    {
        private const float Lifetime = 0.82f; // 피해 숫자가 화면에 남아 있는 총 시간
        private const float FloatDistance = 72f; // 표시 시간 동안 위로 이동할 UI 거리
        private BattleHooks _battleHooks; // 피해 완료 이벤트를 제공하는 전투 훅 버스
        private BoardView _boardView; // 보드 좌표를 월드 위치로 변환할 보드 뷰
        private Canvas _canvas; // Floating Text 전용 Screen Space Overlay Canvas
        private RectTransform _canvasRect; // ScreenPoint를 Canvas 로컬 좌표로 변환할 RectTransform
        private int _spawnSerial; // 같은 위치 연속 피해 숫자를 조금 분리하기 위한 순번
        private static Font _runtimeFont; // 피해 숫자용 런타임 폰트 캐시

        public int ActiveTextCount { get; private set; } // 현재 화면에 살아 있는 피해 숫자 개수

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Battle Scene 로드 직후 자동 생성을 등록하는 특성
        private static void AutoCreateForBattleScene() // Battle Scene에서 Floating Text UI를 자동 생성하는 메서드
        {
            if (SceneManager.GetActiveScene().name != "Battle") // 현재 Scene이 Battle이 아니면
            {
                return; // 다른 Scene에는 전투 피해 숫자를 생성하지 않음
            }

            if (Object.FindFirstObjectByType<CombatFloatingTextUI>() != null) // 이미 같은 UI가 존재하면
            {
                return; // 중복 생성 차단
            }

            var host = new GameObject("CombatFloatingTextUI_Day63"); // 63일차 Floating Text 호스트 생성
            host.AddComponent<CombatFloatingTextUI>(); // 자동 Bind를 수행할 컴포넌트 추가
        }

        public static string FormatDamage(int amount) // 적용 피해량을 화면용 문자열로 변환하는 메서드
        {
            return amount > 0 ? $"-{amount}" : string.Empty; // 실제 양수 피해만 음수 형태의 표시 문자열로 반환
        }

        private void Start() // 런타임 생성 직후 호출되는 초기화 메서드
        {
            StartCoroutine(BindWhenBattleReady()); // BattleController와 BoardView 준비 후 연결하도록 대기 시작
        }

        private IEnumerator BindWhenBattleReady() // 피해 훅과 보드 뷰가 준비될 때까지 기다리는 코루틴
        {
            const int maxFrames = 180; // Scene 초기화 지연을 허용할 최대 프레임 수

            for (int frame = 0; frame < maxFrames; frame++) // 제한 프레임 동안 전투 객체를 탐색하며
            {
                var battleController = Object.FindFirstObjectByType<BattleController>(); // 현재 BattleController 탐색
                var boardView = Object.FindFirstObjectByType<BoardView>(); // 현재 BoardView 탐색

                if (battleController != null && battleController.BattleHooks != null && boardView != null) // 필요한 피해 훅과 보드 뷰가 준비됐으면
                {
                    Bind(battleController.BattleHooks, boardView); // 실제 전투 훅과 보드 뷰 연결
                    yield break; // 연결 완료 후 대기 코루틴 종료
                }

                yield return null; // 아직 준비되지 않았으면 다음 프레임까지 대기
            }

            Debug.LogWarning("Day63 CombatFloatingTextUI: BattleHooks 또는 BoardView 연결을 찾지 못했습니다."); // 자동 연결 실패 원인 로그 출력
        }

        public void Bind(BattleHooks battleHooks, BoardView boardView) // 실제 피해 이벤트와 보드 위치를 Floating Text에 연결하는 메서드
        {
            if (_battleHooks != null) // 이전 훅 버스가 연결돼 있었다면
            {
                _battleHooks.AfterDamage -= HandleAfterDamage; // 이전 피해 이벤트 구독 해제
            }

            _battleHooks = battleHooks; // 새 전투 훅 버스 저장
            _boardView = boardView; // 새 보드 뷰 저장
            EnsureCanvas(); // 피해 숫자 전용 Canvas 생성 보장

            if (_battleHooks != null) // 정상 훅 버스가 연결됐으면
            {
                _battleHooks.AfterDamage += HandleAfterDamage; // 실제 피해 완료 이벤트 구독
            }
        }

        private void HandleAfterDamage(PieceRuntimeState target, PieceRuntimeState source, int appliedAmount) // 실제 HP 감소 직후 호출되는 피해 이벤트 처리 메서드
        {
            if (target == null || appliedAmount <= 0) // 표시 대상이 없거나 실제 피해량이 0 이하이면
            {
                return; // 피해 숫자 생성을 생략
            }

            ShowDamage(target, appliedAmount); // 대상 보드 위치에 실제 피해 숫자 표시
        }

        public void ShowDamage(PieceRuntimeState target, int amount) // 지정한 기물 위치 위에 피해 숫자를 생성하는 메서드
        {
            string label = FormatDamage(amount); // 화면에 표시할 최종 피해 문자열 계산

            if (target == null || string.IsNullOrEmpty(label) || _boardView == null) // 표시 조건 또는 보드 연결이 누락됐으면
            {
                return; // Floating Text 생성을 생략
            }

            EnsureCanvas(); // 외부 직접 호출에서도 Canvas 생성을 보장
            var camera = Camera.main; // 현재 Battle Scene 메인 카메라 확보

            if (camera == null || _canvasRect == null) // 카메라 또는 Canvas 좌표계가 준비되지 않았으면
            {
                return; // 화면 좌표 계산이 불가능하므로 표시 생략
            }

            Vector3 worldPosition = GetPieceWorldPosition(target); // 대상 보드 셀 중앙 위쪽의 월드 위치 계산
            Vector3 screenPosition = camera.WorldToScreenPoint(worldPosition); // 월드 위치를 화면 픽셀 좌표로 변환

            if (screenPosition.z <= 0f) // 대상이 카메라 뒤쪽에 있으면
            {
                return; // 화면 밖 잘못된 숫자 생성을 차단
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPosition, null, out Vector2 localPoint)) // 화면 좌표를 Canvas 로컬 좌표로 변환하지 못하면
            {
                return; // 위치 계산 실패 시 표시 생략
            }

            _spawnSerial++; // 연속 생성 숫자 순번 증가
            localPoint += new Vector2(((_spawnSerial % 3) - 1) * 12f, (_spawnSerial % 2) * 10f); // 같은 기물 연속 피해 숫자의 완전한 겹침 방지
            var text = CreateDamageText(target.IsPlayerPiece); // 아군·적 대상에 맞는 피해 숫자 Text 생성
            text.text = label; // 최종 피해량 문구 적용
            text.rectTransform.anchoredPosition = localPoint; // 대상 기물의 화면 위치에 숫자 배치
            ActiveTextCount++; // 현재 활성 피해 숫자 개수 증가
            StartCoroutine(AnimateAndDestroy(text)); // 위로 떠오르며 사라지는 표시 연출 시작
        }

        private Vector3 GetPieceWorldPosition(PieceRuntimeState target) // 보드 좌표를 피해 숫자용 월드 위치로 변환하는 메서드
        {
            float offsetX = (BoardState.Width - 1) * 0.5f; // 보드 가로 중앙 정렬 오프셋 계산
            float offsetY = (BoardState.Height - 1) * 0.5f; // 보드 세로 중앙 정렬 오프셋 계산
            Vector2Int cell = target.BoardPosition; // 대상 기물의 현재 보드 좌표 확보
            Vector3 localPosition = new Vector3((cell.x - offsetX) * _boardView.TileSize, 1.25f, (cell.y - offsetY) * _boardView.TileSize); // 보드 메시 기준 대상 셀 중앙 위쪽 로컬 위치 계산
            return _boardView.transform.TransformPoint(localPosition); // 보드 Transform을 적용한 실제 월드 위치 반환
        }

        private Text CreateDamageText(bool targetIsPlayerPiece) // 대상 진영에 따라 피해 숫자 Text를 생성하는 메서드
        {
            var textObject = new GameObject("DamageFloatingText", typeof(RectTransform), typeof(Text), typeof(Outline)); // 피해 숫자 UI 오브젝트 생성
            textObject.transform.SetParent(_canvas.transform, false); // Floating Text Canvas 자식으로 연결
            var text = textObject.GetComponent<Text>(); // Text 컴포넌트 확보
            text.font = GetRuntimeFont(); // 한글·숫자 표시용 런타임 폰트 적용
            text.fontSize = 34; // 전투 중 즉시 읽을 수 있는 피해 숫자 크기 적용
            text.fontStyle = FontStyle.Bold; // 피해량 강조를 위한 굵은 글씨 적용
            text.alignment = TextAnchor.MiddleCenter; // 대상 위치를 기준으로 중앙 정렬
            text.color = targetIsPlayerPiece ? new Color(1f, 0.28f, 0.28f, 1f) : new Color(1f, 0.72f, 0.18f, 1f); // 아군 피해는 붉게, 적 피해는 주황색으로 구분
            text.raycastTarget = false; // Floating Text가 보드 클릭을 방해하지 않도록 설정
            var rect = text.rectTransform; // Text RectTransform 확보
            rect.anchorMin = new Vector2(0.5f, 0.5f); // 화면 중앙 기준 앵커 적용
            rect.anchorMax = new Vector2(0.5f, 0.5f); // 화면 중앙 기준 앵커 고정
            rect.pivot = new Vector2(0.5f, 0.5f); // 숫자 중앙을 위치 기준점으로 적용
            rect.sizeDelta = new Vector2(140f, 54f); // 최대 두세 자리 피해량을 담을 크기 적용
            var outline = textObject.GetComponent<Outline>(); // 외곽선 컴포넌트 확보
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f); // 밝은 보드에서도 읽히는 검은 외곽선 적용
            outline.effectDistance = new Vector2(1.8f, -1.8f); // 피해 숫자 외곽선 거리 적용
            outline.useGraphicAlpha = true; // Fade 알파값을 외곽선에도 반영
            return text; // 구성 완료 피해 Text 반환
        }

        private IEnumerator AnimateAndDestroy(Text text) // 피해 숫자를 위로 이동시키며 서서히 지우는 코루틴
        {
            Vector2 startPosition = text.rectTransform.anchoredPosition; // 생성 직후 시작 위치 저장
            Color startColor = text.color; // Fade 기준 원본 글자색 저장
            float elapsed = 0f; // 연출 경과 시간 초기화

            while (elapsed < Lifetime && text != null) // Text가 살아 있고 표시 시간이 남아 있는 동안
            {
                elapsed += Time.unscaledDeltaTime; // 게임 속도와 무관한 UI 경과 시간 누적
                float progress = Mathf.Clamp01(elapsed / Lifetime); // 0~1 범위 연출 진행률 계산
                text.rectTransform.anchoredPosition = startPosition + Vector2.up * FloatDistance * progress; // 피해 숫자를 위쪽으로 부드럽게 이동
                float alpha = 1f - Mathf.SmoothStep(0f, 1f, progress); // 후반으로 갈수록 자연스럽게 사라지는 알파값 계산
                text.color = new Color(startColor.r, startColor.g, startColor.b, alpha); // 계산된 알파값을 피해 숫자에 적용
                yield return null; // 다음 프레임까지 대기
            }

            if (text != null) // 연출 종료 시 Text 오브젝트가 아직 존재하면
            {
                Destroy(text.gameObject); // 사용이 끝난 피해 숫자 오브젝트 제거
            }

            ActiveTextCount = Mathf.Max(0, ActiveTextCount - 1); // 활성 피해 숫자 개수를 안전하게 감소
        }

        private void EnsureCanvas() // Floating Text Canvas를 한 번만 생성하는 메서드
        {
            if (_canvas != null) // 이미 Canvas가 생성되어 있으면
            {
                return; // 중복 생성 생략
            }

            var canvasObject = new GameObject("CombatFloatingTextCanvas_Day63", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)); // 피해 숫자 전용 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 63일차 Floating Text 호스트 자식으로 연결
            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 컴포넌트 확보
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 위에 직접 표시
            _canvas.sortingOrder = 188; // BattleHUD 아래이면서 일반 전투 패널보다 위쪽에 충분히 보이는 계층 적용
            _canvasRect = canvasObject.GetComponent<RectTransform>(); // 화면 좌표 변환용 RectTransform 캐시 저장
            var scaler = canvasObject.GetComponent<CanvasScaler>(); // 해상도 대응용 CanvasScaler 확보
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 기준 해상도 기반 크기 보정 사용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 현재 프로젝트 공통 기준 해상도 적용
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 다양한 화면 비율 대응 방식 적용
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 동일 비중 보정 적용
        }

        private static Font GetRuntimeFont() // 피해 숫자용 런타임 폰트를 확보하는 메서드
        {
            if (_runtimeFont != null) // 이미 생성한 폰트가 있으면
            {
                return _runtimeFont; // 기존 폰트 재사용
            }

            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 34); // 운영체제 기본 UI 폰트 후보로 동적 폰트 생성

            if (_runtimeFont == null) // 시스템 폰트 생성에 실패하면
            {
                _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 런타임 폰트로 대체
            }

            return _runtimeFont; // 최종 폰트 반환
        }

        private void OnDestroy() // Floating Text 호스트 제거 시 이벤트 구독을 정리하는 메서드
        {
            if (_battleHooks != null) // 전투 훅 버스 연결이 남아 있으면
            {
                _battleHooks.AfterDamage -= HandleAfterDamage; // 피해 완료 이벤트 구독 해제
            }
        }
    }
}
