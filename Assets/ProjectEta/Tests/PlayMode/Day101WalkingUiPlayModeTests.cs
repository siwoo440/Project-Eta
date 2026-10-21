using System.Collections; // 프레임 대기
using NUnit.Framework; // 동작 검사
using UnityEngine; // 실제 씬 객체
using UnityEngine.UI; // 실제 화면 표시
using UnityEngine.InputSystem; // 실제 키 입력
using UnityEngine.InputSystem.LowLevel; // 시험 키 상태
using UnityEngine.TestTools; // 런타임 검사
using ProjectEta.Environment; // 실제 이동 제어

namespace ProjectEta.Tests.PlayMode // 이동 화면 검사 영역
{ // 영역 시작
    public sealed class Day101WalkingUiPlayModeTests // 이동 속도와 표시 회귀 검사
    { // 타입 시작
        private GameObject _root; // 시험 객체 루트
        private RoomWalkingController _walker; // 실제 이동기
        private Keyboard _keyboard; // 독립 키보드
        private float _previousScale; // 기존 시간 배율
        private CursorLockMode _previousLock; // 기존 커서 잠금
        private bool _previousVisible; // 기존 커서 표시
        private InputSettings.UpdateMode _previousUpdate; // 기존 입력 갱신
        private InputSettings.BackgroundBehavior _previousBackground; // 기존 배경 처리
        private InputSettings.EditorInputBehaviorInPlayMode _previousEditor; // 기존 에디터 입력
        [SetUp] // 검사 환경 준비
        public void SetUp() // 실제 이동기 구성
        { // 메서드 시작
            _previousScale = Time.timeScale; // 시간 배율 보관
            _previousLock = Cursor.lockState; // 커서 잠금 보관
            _previousVisible = Cursor.visible; // 커서 표시 보관
            Time.timeScale = 1f; // 기본 시험 배율
            _previousUpdate = InputSystem.settings.updateMode; // 입력 갱신 보관
            _previousBackground = InputSystem.settings.backgroundBehavior; // 배경 처리 보관
            _previousEditor = InputSystem.settings.editorInputBehaviorInPlayMode; // 에디터 입력 보관
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually; // 시험 입력 분리
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 창 포커스 독립
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 게임 입력 경로
            _keyboard = InputSystem.AddDevice<Keyboard>(); // 독립 키보드 생성
            _keyboard.MakeCurrent(); // 시험 키보드 선택
            _root = new GameObject("Day101WalkingUiTest"); // 독립 시험 루트
            Camera camera = _root.AddComponent<Camera>(); // 실제 카메라 생성
            Day41SeatedCameraRig rig = _root.AddComponent<Day41SeatedCameraRig>(); // 실제 좌석 리그
            rig.Configure(camera); // 카메라와 이동기 연결
            _walker = _root.GetComponent<RoomWalkingController>(); // 생성된 이동기 조회
        } // 메서드 종료
        [UnityTearDown] // 시험 자원 복원
        public IEnumerator TearDown() // 다음 검사 상태 보호
        { // 메서드 시작
            if (_walker != null) _walker.ReturnToSeat(); // 표시와 입력 소유 해제
            Object.Destroy(_root); // 시험 객체 제거
            InputSystem.RemoveDevice(_keyboard); // 독립 키보드 제거
            InputSystem.settings.updateMode = _previousUpdate; // 입력 갱신 복원
            InputSystem.settings.backgroundBehavior = _previousBackground; // 배경 처리 복원
            InputSystem.settings.editorInputBehaviorInPlayMode = _previousEditor; // 에디터 입력 복원
            Time.timeScale = _previousScale; // 시간 배율 복원
            Cursor.lockState = _previousLock; // 커서 잠금 복원
            Cursor.visible = _previousVisible; // 커서 표시 복원
            yield return null; // 생성 객체 파괴 대기
            yield return null; // 독립 충돌체 파괴 대기
        } // 메서드 종료
        [UnityTest] // 느린 프레임 이동 회귀
        public IEnumerator QuarterSecondMovement_CoversTwoUnitsWithoutDroppingElapsedTime() // 실제 경과 시간 보존
        { // 메서드 시작
            Assert.That(_walker.TryStand(), Is.True); // 의자 이탈
            Physics.SyncTransforms(); // 실제 충돌체 갱신
            Vector3 start = _walker.FeetPosition; // 이동 시작점
            _walker.Move(Vector2.right, 0.25f, Vector2.zero); // 사분의 일 초 이동
            Vector3 offset = _walker.FeetPosition - start; // 실제 이동 거리
            Assert.That(new Vector2(offset.x, offset.z).magnitude, Is.EqualTo(2f).Within(0.015f)); // 초당 여덟 단위 확인
            yield return null; // 프레임 마무리
        } // 메서드 종료
        [UnityTest] // 저배속 조작 회귀
        public IEnumerator KeyboardMovement_UsesRealTimeAtQuarterGameSpeed() // 실제 입력 시간 기준 이동
        { // 메서드 시작
            Time.timeScale = 0.25f; // 게임 저배속 재현
            Assert.That(_walker.TryStand(), Is.True); // 의자 이탈
            Physics.SyncTransforms(); // 충돌체 갱신
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.D)); // 오른쪽 이동 유지
            InputSystem.Update(); // 시험 키 반영
            yield return null; // 첫 입력 프레임 대기
            Vector3 start = _walker.FeetPosition; // 측정 시작점
            float elapsed = 0f; // 실제 경과 시간
            while (elapsed < 0.2f) // 짧은 실제 시간 측정
            { // 반복 시작
                yield return null; // 실제 이동 프레임
                elapsed += Time.unscaledDeltaTime; // 게임 배율 제외 누적
            } // 반복 종료
            Vector3 offset = _walker.FeetPosition - start; // 실제 이동 결과
            float speed = new Vector2(offset.x, offset.z).magnitude / elapsed; // 실제 초당 이동 거리
            Assert.That(speed, Is.InRange(7.5f, 8.5f)); // 게임 저배속 독립 확인
        } // 메서드 종료
        [UnityTest] // 걷기 표시 범위 회귀
        public IEnumerator Walking_HidesGameplayCanvasesAndPreservesSystemScreens() // 게임 화면만 시각적 숨김
        { // 메서드 시작
            string[] hiddenNames = // 실제 게임 Canvas 이름
            { // 이름 목록 시작
                "BattleHUDCanvas_Day62", "Day66RouteMapCanvas", "HandCanvas", "BossHealthCanvas", "BossPhaseStatusCanvas", // 전투·지도·손패·보스 화면
                "PieceInfoPanelCanvas", "PieceAbilityOverlayCanvas", "Day72RouteMapSeedCanvas", "CombatLogCanvas", "DeckPanelCanvas", "KingCombatHUDCanvas_Day61" // 상세·지도 정보·로그·덱·킹 화면
            }; // 이름 목록 종료
            Image[] hidden = new Image[hiddenNames.Length]; // 게임 화면 이미지
            for (int index = 0; index < hiddenNames.Length; index++) hidden[index] = CreateCanvasImage(hiddenNames[index]); // 실제 화면 구성
            Image pause = CreateCanvasImage("BattlePauseCanvas_Day68"); // 일시정지 화면
            Image tutorial = CreateCanvasImage("FirstRunTutorialCanvas_Day68"); // 튜토리얼 화면
            Image reward = CreateCanvasImage("CardRewardCanvas_Day65"); // 필수 보상 화면
            Image debug = CreateCanvasImage("ProjectEtaDebugCanvas"); // 디버그 화면
            Assert.That(_walker.TryStand(), Is.True); // 의자 이탈
            yield return null; // 표시 반영 대기
            Canvas.ForceUpdateCanvases(); // 실제 화면 투명도 갱신
            foreach (Image image in hidden) // 게임 화면 전체 검사
            { // 반복 시작
                Assert.That(RenderedAlpha(image), Is.LessThan(0.001f), image.transform.parent.name); // 실제 화면 정점 투명도 확인
                Assert.That(image.Raycast(Vector2.zero, null), Is.False); // 숨긴 화면 클릭 통과
                Assert.That(image.raycastTarget, Is.True); // 원래 클릭 소유권 유지
                Assert.That(image.transform.parent.gameObject.activeSelf, Is.True); // Canvas 객체 활성 유지
                Assert.That(image.canvas.enabled, Is.True); // 원본 Canvas 상태 보존
            } // 반복 종료
            Assert.That(RenderedAlpha(pause), Is.GreaterThan(0.99f)); // 일시정지 표시 유지
            Assert.That(RenderedAlpha(tutorial), Is.GreaterThan(0.99f)); // 튜토리얼 표시 유지
            Assert.That(RenderedAlpha(reward), Is.GreaterThan(0.99f)); // 보상 표시 유지
            Assert.That(RenderedAlpha(debug), Is.GreaterThan(0.99f)); // 디버그 표시 유지
            _walker.ReturnToSeat(); // 좌석 복귀
            yield return null; // 표시 복원 대기
            Canvas.ForceUpdateCanvases(); // 실제 투명도 갱신
            foreach (Image image in hidden) Assert.That(RenderedAlpha(image), Is.GreaterThan(0.99f)); // 모든 게임 화면 복원
        } // 메서드 종료
        [UnityTest] // 동적 생성과 소유권 회귀
        public IEnumerator Walking_DynamicHudAndOwnerChangesSurviveWalkerDestruction() // 기존 상태와 생성 화면 수명 보호
        { // 메서드 시작
            Image hand = CreateCanvasImage("HandCanvas"); // 기존 손패 화면
            CanvasGroup original = hand.transform.parent.gameObject.AddComponent<CanvasGroup>(); // 원래 표시 소유자
            original.alpha = 0.4f; // 원래 반투명 상태
            original.interactable = false; // 원래 상호작용 차단
            original.blocksRaycasts = false; // 원래 클릭 통과
            Image disabled = CreateCanvasImage("PieceInfoPanelCanvas"); // 원래 꺼진 정보 화면
            Canvas originallyDisabledCanvas = disabled.transform.parent.GetComponent<Canvas>(); // 실제 Canvas 컴포넌트 참조 보관
            originallyDisabledCanvas.enabled = false; // 원래 Canvas 비활성
            Image inactive = CreateCanvasImage("DeckPanelCanvas"); // 원래 꺼진 덱 화면
            inactive.transform.parent.gameObject.SetActive(false); // 원래 객체 비활성
            Assert.That(_walker.TryStand(), Is.True); // 의자 이탈
            yield return null; // 첫 표시 억제 대기
            Image dynamicHud = CreateCanvasImage("BossHealthCanvas"); // 걷기 중 보스 화면 생성
            original.alpha = 0.25f; // 원래 표시 소유자 상태 변경
            yield return null; // 동적 화면 억제 대기
            Canvas.ForceUpdateCanvases(); // 실제 화면 투명도 갱신
            Assert.That(RenderedAlpha(hand), Is.LessThan(0.001f)); // 원래 표시 갱신 중 숨김 유지
            Assert.That(RenderedAlpha(dynamicHud), Is.LessThan(0.001f)); // 신규 보스 화면 숨김
            Assert.That(original.alpha, Is.EqualTo(0.25f)); // 원래 표시 소유권 유지
            Object.Destroy(_walker); // 이동기 파괴로 씬 종료 재현
            yield return null; // 소유 자원 정리 대기
            yield return null; // 표시 마스크 제거 대기
            Canvas.ForceUpdateCanvases(); // 복원 투명도 갱신
            Assert.That(RenderedAlpha(hand), Is.EqualTo(0.25f).Within(0.005f)); // 변경된 원래 상태 유지
            Assert.That(original.interactable, Is.False); // 원래 상호작용 상태 유지
            Assert.That(original.blocksRaycasts, Is.False); // 원래 클릭 상태 유지
            Assert.That(originallyDisabledCanvas, Is.Not.Null); // 원래 Canvas 컴포넌트 수명 보존
            Assert.That(originallyDisabledCanvas.enabled, Is.False); // 동일한 Canvas의 비활성 상태 유지
            Assert.That(inactive.transform.parent.gameObject.activeSelf, Is.False); // 꺼진 객체 상태 유지
            Assert.That(RenderedAlpha(dynamicHud), Is.GreaterThan(0.99f)); // 신규 화면 표시 복원
            Assert.That(hand.GetComponent<RoomWalkingGraphicMask>(), Is.Null); // 소유자 파괴 후 마스크 제거
            Assert.That(RoomWalkingController.BlocksTableInput, Is.False); // 보드 입력 소유 해제
        } // 메서드 종료
        [UnityTest] // 중첩 화면 우회 회귀
        public IEnumerator Walking_NestedCanvasIgnoringParentGroupsCannotEscapeMask() // 부모 투명도 무시 화면 보호
        { // 메서드 시작
            Image hand = CreateCanvasImage("HandCanvas"); // 숨김 대상 손패 화면
            Image nested = CreateCanvasImage("NestedEscapingCanvas"); // 독립 정렬 중첩 화면
            nested.transform.parent.SetParent(hand.transform.parent, false); // 손패 안쪽 중첩
            nested.canvas.overrideSorting = true; // 별도 정렬 Canvas 재현
            CanvasGroup original = nested.transform.parent.gameObject.AddComponent<CanvasGroup>(); // 원래 중첩 표시 소유자
            original.ignoreParentGroups = true; // 부모 투명도 우회 재현
            original.alpha = 0.6f; // 원래 중첩 투명도
            Assert.That(_walker.TryStand(), Is.True); // 의자 이탈
            yield return null; // 표시 마스크 반영 대기
            Canvas.ForceUpdateCanvases(); // 실제 중첩 표시 갱신
            Assert.That(RenderedAlpha(nested), Is.LessThan(0.001f)); // 중첩 우회 차단
            Assert.That(nested.Raycast(Vector2.zero, null), Is.False); // 부모 무시 화면 클릭 통과
            Assert.That(original.ignoreParentGroups, Is.True); // 원래 표시 정책 보존
            _walker.ReturnToSeat(); // 좌석 복귀
            yield return null; // 마스크 제거 대기
            Canvas.ForceUpdateCanvases(); // 원래 표시 갱신
            Assert.That(RenderedAlpha(nested), Is.EqualTo(0.6f).Within(0.005f)); // 원래 중첩 투명도 복원
            Assert.That(nested.Raycast(Vector2.zero, null), Is.True); // 원래 클릭 판정 복원
        } // 메서드 종료
        [UnityTest] // 같은 프레임 재기립 회귀
        public IEnumerator Walking_SameFrameReseatAndStandReusesMaskAndCatchesNewGraphic() // 재기립과 동적 표시 요소 보호
        { // 메서드 시작
            Image hand = CreateCanvasImage("HandCanvas"); // 원래 손패 표시 요소
            CanvasGroup original = hand.gameObject.AddComponent<CanvasGroup>(); // 표시 요소 자체 원래 그룹
            original.alpha = 0.7f; // 원래 표시 요소 투명도
            Assert.That(_walker.TryStand(), Is.True); // 첫 의자 이탈
            _walker.ReturnToSeat(); // 같은 프레임 좌석 복귀
            Assert.That(_walker.TryStand(), Is.True); // 같은 프레임 재기립
            var child = new GameObject("NewCard", typeof(RectTransform), typeof(Image)); // 기존 Canvas 안쪽 동적 카드
            child.transform.SetParent(hand.transform.parent, false); // 원래 손패 Canvas 연결
            Image dynamicCard = child.GetComponent<Image>(); // 실제 신규 표시 요소
            yield return null; // 동적 표시 반영 대기
            Canvas.ForceUpdateCanvases(); // 최종 표시 정점 갱신
            Assert.That(hand.GetComponents<RoomWalkingGraphicMask>().Length, Is.EqualTo(1)); // 기존 마스크 재사용
            Assert.That(RenderedAlpha(hand), Is.LessThan(0.001f)); // 재기립 화면 숨김 유지
            Assert.That(RenderedAlpha(dynamicCard), Is.LessThan(0.001f)); // 기존 Canvas 신규 카드 숨김
            Assert.That(original.alpha, Is.EqualTo(0.7f)); // 원래 표시 요소 그룹 보존
            _walker.ReturnToSeat(); // 최종 좌석 복귀
            yield return null; // 표시 복원 대기
            Canvas.ForceUpdateCanvases(); // 복원 표시 정점 갱신
            Assert.That(RenderedAlpha(hand), Is.EqualTo(0.7f).Within(0.005f)); // 원래 표시 요소 투명도 복원
            Assert.That(RenderedAlpha(dynamicCard), Is.GreaterThan(0.99f)); // 동적 카드 표시 복원
        } // 메서드 종료
        private static float RenderedAlpha(Image image) // 실제 렌더 정점과 그룹의 합성 투명도
        { // 메서드 시작
            Mesh mesh = image.canvasRenderer.GetMesh(); // CanvasRenderer 소유 최종 화면 정점 조회
            float maximum = 0f; // 표시 가능한 최대 정점 투명도
            if (mesh != null) // 아직 생성되지 않은 렌더 정점 제외
            { // 조건 시작
                foreach (Color32 color in mesh.colors32) maximum = Mathf.Max(maximum, color.a / 255f); // 최종 정점 표시값 계산
            } // 조건 종료
            return maximum * image.canvasRenderer.GetInheritedAlpha(); // 원래 그룹 투명도와 최종 정점 합성
        } // 메서드 종료
        private Image CreateCanvasImage(string canvasName) // 실제 Canvas와 표시 이미지 구성
        { // 메서드 시작
            var canvasRoot = new GameObject(canvasName, typeof(RectTransform), typeof(Canvas)); // 실제 Canvas 생성
            canvasRoot.transform.SetParent(_root.transform, false); // 시험 루트에 연결
            canvasRoot.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; // 화면 고정 표시
            var graphic = new GameObject("VisibleImage", typeof(RectTransform), typeof(Image)); // 실제 표시 요소 생성
            graphic.transform.SetParent(canvasRoot.transform, false); // Canvas에 표시 연결
            graphic.GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 100f); // 화면 영역 확보
            return graphic.GetComponent<Image>(); // 실제 표시 요소 반환
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
