using System; // 콜백 보관
using System.Collections; // 프레임 대기
using System.Collections.Generic; // 화면 옵션
using System.Linq; // 실제 버튼 조회
using System.Reflection; // 실제 생명주기·설정 입력 재현
using NUnit.Framework; // 결과 검사
using UnityEngine; // 실제 화면과 시간
using UnityEngine.UI; // 실제 버튼과 Canvas
using UnityEngine.InputSystem; // 실제 키 입력
using UnityEngine.InputSystem.LowLevel; // 시험 입력 상태
using UnityEngine.TestTools; // PlayMode 검사
using ProjectEta.Board; // 실제 보드 입력
using ProjectEta.Battle; // 실제 턴 상태
using ProjectEta.Run; // 독립 런
using ProjectEta.UI; // 실제 이벤트 화면
using ProjectEta.Settings; // 실제 일시정지·설정
using ProjectEta.Environment; // 실제 걷기

namespace ProjectEta.Tests.PlayMode // 화면 전환 회귀 검사
{ // 영역 시작
    public sealed class Day103ScreenFlowPlayModeTests // 입력 소유와 복원 검사
    { // 타입 시작
        private GameObject _root; // 시험 루트
        private StageBoardOverlayUI _activity; // 실제 선택 화면
        private BattleSettingsOverlayController _pause; // 실제 설정 화면
        private BoardInputController _boardInput; // 실제 보드 권한
        private RoomWalkingController _walker; // 실제 걷기
        private Keyboard _keyboard; // 독립 키보드
        private InputSettings.UpdateMode _oldMode; // 원래 입력 갱신
        private InputSettings.BackgroundBehavior _oldBackground; // 원래 포커스 정책
        private InputSettings.EditorInputBehaviorInPlayMode _oldEditor; // 원래 에디터 입력
        private float _oldTime; // 원래 시간 배율
        private CursorLockMode _oldLock; // 원래 커서 잠금
        private bool _oldVisible; // 원래 커서 표시
        [UnitySetUp] // 시험 환경 준비
        public IEnumerator SetUp() // 실제 화면 구성
        { // 메서드 시작
            _oldTime = Time.timeScale; // 배율 보관
            _oldLock = Cursor.lockState; // 잠금 보관
            _oldVisible = Cursor.visible; // 표시 보관
            Time.timeScale = 1f; // 독립 시험 시간
            _oldMode = InputSystem.settings.updateMode; // 입력 갱신 보관
            _oldBackground = InputSystem.settings.backgroundBehavior; // 배경 입력 보관
            _oldEditor = InputSystem.settings.editorInputBehaviorInPlayMode; // 에디터 입력 보관
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually; // 명시적 키 상태 갱신
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 창 포커스 독립
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 게임 입력 경로
            _keyboard = InputSystem.AddDevice<Keyboard>(); // 시험 장치
            _keyboard.MakeCurrent(); // 현재 장치 선택
            _root = new GameObject("Day103ScreenFlow"); // 독립 객체 루트
            Day41SeatedCameraRig rig = _root.AddComponent<Day41SeatedCameraRig>(); // 실제 좌석 리그
            rig.Configure(_root.AddComponent<Camera>()); // 실제 카메라·이동 연결
            _walker = _root.GetComponent<RoomWalkingController>(); // 생성된 이동기
            BoardView board = _root.AddComponent<BoardView>(); // 실제 보드
            var run = new RunState(3); // 독립 런
            board.Bind(run.Board); // 실제 보드 연결
            _boardInput = _root.AddComponent<BoardInputController>(); // 실제 입력기
            _boardInput.Bind(run, board, new TurnManager(), new BattleHooks()); // 실제 턴 권한
            _activity = _root.AddComponent<StageBoardOverlayUI>(); // 실제 이벤트 UI
            _activity.Initialize(board); // 실제 버튼 생성
            _pause = _root.AddComponent<BattleSettingsOverlayController>(); // 실제 일시정지
            yield return null; // Start와 Canvas 갱신
            GameInputBindingService.Apply(GameSettingsData.CreateDefault()); // 기본 Esc·Space로 고정
        } // 메서드 종료
        [UnityTearDown] // 시험 환경 복원
        public IEnumerator TearDown() // 소유 자원 정리
        { // 메서드 시작
            _pause.ClosePanel(); // 일시정지 소유 해제
            _walker.ReturnToSeat(); // 걷기 소유 해제
            UnityEngine.Object.Destroy(_root); // 실제 화면 제거
            InputSystem.RemoveDevice(_keyboard); // 시험 키보드 제거
            InputSystem.settings.updateMode = _oldMode; // 원래 입력 갱신
            InputSystem.settings.backgroundBehavior = _oldBackground; // 원래 배경 처리
            InputSystem.settings.editorInputBehaviorInPlayMode = _oldEditor; // 원래 에디터 처리
            Time.timeScale = _oldTime; // 원래 시간
            Cursor.lockState = _oldLock; // 원래 커서 잠금
            Cursor.visible = _oldVisible; // 원래 표시
            GameSettingsService.ReapplyUiScale(); // 미리보기 배율 복원
            GameInputBindingService.Apply(GameSettingsService.Current); // 실제 저장 키 복원
            yield return null; // 객체 제거 대기
            yield return null; // 이동 충돌체 제거 대기
        } // 메서드 종료
        [UnityTest] // 모달 아래 보드 권한 검사
        public IEnumerator Activity_BlocksBoardAndCompleteAction() // 선택 화면 입력 관통 재현
        { // 메서드 시작
            Show(new StageOverlayOption("선택", "효과", true, null)); // 실제 모달 진입
            Press(Key.Space); // 행동 완료 키 입력
            Assert.That(_boardInput.CanReceivePlayerInput, Is.False); // 모달 아래 보드 차단
            Assert.That(_boardInput.CanUseCardSummonInput, Is.False); // 카드 드래그 권한 차단
            Assert.That(GameInputBindingService.WasPressedThisFrame(GameInputAction.CompleteAction), Is.False); // 턴 종료 관통 차단
            yield return null; // 입력 프레임 종료
        } // 메서드 종료
        [UnityTest] // 모달 위 모달 입력 검사
        public IEnumerator Pause_BlocksActivityCallbackUntilResumed() // 가려진 이벤트 선택 차단
        { // 메서드 시작
            int calls = 0; // 실제 효과 횟수
            Show(new StageOverlayOption("선택", "효과", true, () => calls++)); // 실제 선택 버튼
            Button choice = Button("선택"); // 현재 버튼 보관
            _pause.OpenPanel(); // 위에 일시정지 표시
            choice.onClick.Invoke(); // 오래된 포인터 콜백 재현
            Assert.That(calls, Is.Zero); // 숨은 보상 적용 금지
            _pause.ClosePanel(); // 선택 화면 복귀
            Press(); // 키 해제
            yield return null; // 닫기 프레임과 분리
            choice.onClick.Invoke(); // 복귀 후 정상 입력
            Assert.That(calls, Is.EqualTo(1)); // 복원된 화면만 처리
        } // 메서드 종료
        [UnityTest] // 이전 페이지 콜백 검사
        public IEnumerator Activity_HiddenAndRebuiltCallbacksCannotExecute() // 오래된 페이지 입력 차단
        { // 메서드 시작
            int calls = 0; // 효과 횟수
            Show(new StageOverlayOption("이전 선택", "효과", true, () => calls++)); // 첫 페이지
            var oldCallback = Button("이전 선택").onClick; // 이전 이벤트 객체 보관
            _activity.Hide(); // 화면 숨김
            oldCallback.Invoke(); // 숨은 버튼 콜백
            Assert.That(calls, Is.Zero); // 숨은 입력 차단
            Show(new StageOverlayOption("새 선택", "효과", true, () => calls += 10)); // 동일 뷰 재사용
            oldCallback.Invoke(); // 지난 페이지 이벤트 재입력
            Assert.That(calls, Is.Zero); // 새 페이지 콜백까지 우회 실행 금지
            yield return null; // 프레임 정리
        } // 메서드 종료
        [UnityTest] // 닫기 생명주기 검사
        public IEnumerator DisabledPause_RestoresTimeInputAndHidesChildren() // 비활성화 후 정지 잔존 재현
        { // 메서드 시작
            Time.timeScale = 0.25f; // 원래 전투 배속
            _pause.OpenPanel(); // 시간 중단
            _pause.enabled = false; // 관리자만 비활성화
            Assert.That(Time.timeScale, Is.EqualTo(0.25f)); // 원래 배속 복원
            Assert.That(_boardInput.enabled, Is.True); // 보드 컴포넌트 복원
            Assert.That(_pause.IsOpen, Is.False); // 모달 소유 해제
            Assert.That(_root.GetComponentsInChildren<Transform>(true).Any(item => item.name == "PauseRoot_Day68" && item.gameObject.activeSelf), Is.False); // 자식 화면도 닫힘
            yield return null; // 프레임 정리
        } // 메서드 종료
        [UnityTest] // 동시 입력 순서 검사
        public IEnumerator EscapeAndWalk_SameFrameOpensPauseWithoutStanding() // 늦은 일시정지 처리 재현
        { // 메서드 시작
            Press(Key.Escape, Key.W); // 같은 입력 갱신에서 두 키 누름
            yield return null; // 실제 Update 순서 실행
            Assert.That(_pause.IsOpen, Is.True); // 일시정지 우선
            Assert.That(_walker.IsWalking, Is.False); // 같은 프레임 의자 이탈 금지
        } // 메서드 종료
        [UnityTest] // 커서 즉시 복원 검사
        public IEnumerator PauseWhileWalking_ReleasesCursorAndResumesSameWalkingState() // 모달 표시 순간 커서 확보
        { // 메서드 시작
            Assert.That(_walker.TryStand(), Is.True); // 의자 이탈
            _pause.OpenPanel(); // 직접 UI 열기
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None)); // 다음 프레임 전 즉시 클릭 가능
            Assert.That(Cursor.visible, Is.True); // 모달 포인터 표시
            _pause.ClosePanel(); // 걷기로 복귀
            Press(); // 입력 해제
            yield return null; // 이동 상태 복원
            Assert.That(_walker.IsWalking, Is.True); // 좌석으로 강제 복귀 없음
            Assert.That(RoomWalkingController.BlocksTableInput, Is.True); // 보드 차단 유지
            _walker.ReturnToSeat(); // 원래 HUD 복원
            Assert.That(RoomWalkingController.BlocksTableInput, Is.False); // 좌석 입력 복원
        } // 메서드 종료
        [UnityTest] // 실제 키보드 선택 검사
        public IEnumerator Activity_TabSkipsDisabledAndEnterRunsFocusedChoiceOnce() // 마우스 없는 선택·확정
        { // 메서드 시작
            int first = 0; // 첫 선택 횟수
            int second = 0; // 두 번째 선택 횟수
            Show(new StageOverlayOption("첫 선택", "첫 효과", true, () => first++), new StageOverlayOption("불가", "불가 이유", false, null), new StageOverlayOption("끝 선택", "끝 효과", true, () => second++)); // 실제 세 버튼
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(Button("첫 선택").gameObject); // 이전 포인터 선택의 기본 Submit 중복 재현
            Press(Key.Tab); // 첫 키보드 포커스
            yield return null; // 실제 키 처리
            Press(); // 첫 입력 해제
            yield return null; // 해제 반영
            Press(Key.Tab); // 다음 활성 선택으로 이동
            yield return null; // 실제 키 처리
            Press(); // 이동 입력 해제
            yield return null; // 해제 반영
            Press(Key.Enter); // 포커스 선택 확정
            yield return null; // 실제 확정 처리
            Assert.That(first, Is.Zero); // 이전 선택 미실행
            Assert.That(second, Is.EqualTo(1)); // 비활성 건너뛰기와 한 번 확정
        } // 메서드 종료
        [UnityTest] // 설정 취소와 화면 범위 검사
        public IEnumerator SettingsAtMaximumScale_FitsViewportAndCancelRestoresSavedScale() // 큰 배율의 잘림과 미리보기 복원
        { // 메서드 시작
            GameSettingsData saved = GameSettingsService.Current; // 원래 저장 설정
            _pause.OpenPanel(); // 설정 진입 준비
            _pause.ShowSettingsPanel(); // 실제 설정 화면
            GameSettingsService.PreviewUiScale(GameSettingsData.MaximumUiScale); // 최대 배율 적용
            yield return null; // Canvas와 맞춤 배치 갱신
            Canvas.ForceUpdateCanvases(); // 실제 화면 좌표 갱신
            RectTransform panel = _root.GetComponentsInChildren<RectTransform>(true).First(item => item.name == "SettingsPanel_Day57"); // 실제 설정 패널
            Vector3[] corners = new Vector3[4]; // 화면 모서리 좌표
            panel.GetWorldCorners(corners); // 실제 렌더 영역
            Assert.That(corners.All(point => point.x >= 0f && point.y >= 0f && point.x <= Screen.width && point.y <= Screen.height), Is.True, "화면 " + Screen.width + "x" + Screen.height + " / 모서리 " + string.Join(",", corners.Select(point => point.ToString())) + " / 패널 " + panel.rect + " / 부모 " + (panel.parent as RectTransform).rect + " / 배율 " + panel.localScale); // 모든 설정 버튼 화면 안
            _pause.ShowPausePanel(); // 미적용 편집 취소
            Assert.That(GameSettingsService.Current.ContentEquals(saved), Is.True); // 저장값 변경 없음
            CanvasScaler scaler = _root.GetComponentsInChildren<CanvasScaler>(true).First(item => item.name == "BattlePauseCanvas_Day68"); // 실제 설정 Canvas
            Assert.That(scaler.referenceResolution.x, Is.EqualTo(1920f / saved.UiScale).Within(0.01f)); // 원래 미리보기 배율 복원
        } // 메서드 종료
        [UnityTest] // 메인 메뉴의 실제 큰 배율 검사
        public IEnumerator MainMenuAtMaximumScale_KeepsBothPanelsInsideViewport() // 타이틀 버튼과 안내의 접근 보장
        { // 메서드 시작
            var host = new GameObject("Day103MainMenu"); // 실제 타이틀 호스트
            host.transform.SetParent(_root.transform); // 시험 소유
            host.AddComponent<MainMenuController>(); // 실제 메뉴 생성 경로
            yield return null; // 메뉴 Start 실행
            GameSettingsService.PreviewUiScale(GameSettingsData.MaximumUiScale); // 최대 배율
            yield return null; // 배율 반영
            Canvas.ForceUpdateCanvases(); // 최종 모서리 갱신
            foreach (RectTransform panel in host.GetComponentsInChildren<RectTransform>(true).Where(item => item.name == "BrandPanel" || item.name == "MenuPanel")) // 좌우 실제 패널
            { // 반복 시작
                Vector3[] corners = new Vector3[4]; // 실제 화면 영역
                panel.GetWorldCorners(corners); // 최종 배치
                Assert.That(corners.All(point => point.x >= 0f && point.y >= 0f && point.x <= Screen.width && point.y <= Screen.height), Is.True, panel.name); // 두 패널 모두 화면 안
            } // 반복 종료
        } // 메서드 종료
        [UnityTest] // 일시정지 아래 실제 보상 검사
        public IEnumerator RewardWhilePaused_CannotSelectOrConfirmUntilResumed() // 보상 버튼의 입력 소유 보호
        { // 메서드 시작
            int rewards = 0; // 실제 지급 횟수
            CardRewardUI reward = _root.AddComponent<CardRewardUI>(); // 실제 보상 UI
            var cards = Resources.Load<ProjectEta.Cards.PlayerStartingDeckCatalog>("PlayerStartingDeck26").Cards.Take(1).ToArray(); // 실제 카드 후보
            reward.Show(cards, ProjectEta.Run.CardRewardSource.RewardNode, card => rewards++); // 독립 보상 콜백
            Button cardButton = _root.GetComponentsInChildren<Button>(true).First(button => button.name.StartsWith("RewardCard_")); // 실제 카드 버튼
            _pause.OpenPanel(); // 보상 위 일시정지
            cardButton.onClick.Invoke(); // 가려진 카드 입력
            Button confirm = _root.GetComponentsInChildren<Button>(true).First(button => button.name == "ConfirmRewardButton"); // 실제 확정 버튼
            Assert.That(confirm.interactable, Is.False); // 가려진 카드 선택 금지
            _pause.ClosePanel(); // 보상 화면 복귀
            Press(); // 시스템 키 해제
            yield return null; // 전환 프레임 종료
            cardButton.onClick.Invoke(); // 정상 카드 선택
            _pause.OpenPanel(); // 선택 후 다시 일시정지
            confirm.onClick.Invoke(); // 가려진 확정 입력
            Assert.That(rewards, Is.Zero); // 가려진 보상 확정 금지
            _pause.ClosePanel(); // 보상 화면 복귀
            yield return null; // 전환 프레임 종료
            confirm.onClick.Invoke(); // 정상 확정
            Assert.That(rewards, Is.EqualTo(1)); // 정상 상태에서만 보상
            reward.Hide(); // 시험 보상 종료
        } // 메서드 종료
        [UnityTest] // 일시정지 전 실제 능력 버튼 포커스 검사
        public IEnumerator Pause_PreservesSelectedAbilityButtonAndKeyboardFocus() // 일시적 입력 차단의 버튼 파괴 방지
        { // 메서드 시작
            var host = new GameObject("Day103AbilityUi"); // 실제 능력 화면 호스트
            host.transform.SetParent(_root.transform); // 시험 소유
            host.AddComponent<PieceAbilityOverlayUI>(); // 실제 능력 화면
            var catalog = Resources.Load<RunContentCatalog>("RunContent"); // 실제 게임 카드
            var sage = new ProjectEta.Pieces.PieceRuntimeState(catalog.PieceDatabase.FindById("grand_sage"), new Vector2Int(3, 3), true); // 대현자 배치
            _boardInput.RunState.Board.GetTile(sage.BoardPosition).OccupyingPiece = sage; // 실제 점유
            _boardInput.TurnManager.MarkInitialKingPlaced(); // 배치 종료 조건
            _boardInput.TurnManager.TryEndDeploymentTurn(); // 실제 전투 행동 턴
            Assert.That(_boardInput.TrySelectPieceAt(sage.BoardPosition), Is.True); // 능력 기물 선택
            yield return null; // 능력 UI 생성
            yield return null; // 상태 갱신 완료
            Button selected = host.GetComponentsInChildren<Button>(true).First(button => button.name == "Ability_회복"); // 실제 회복 버튼
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(selected.gameObject); // 키보드 선택 보관
            _pause.OpenPanel(); // 능력 화면 위 일시정지
            yield return null; // 모달 중 상태 갱신
            yield return null; // 이전 버튼 파괴 시점까지 대기
            Assert.That(selected != null, Is.True, "일시정지 중 능력 버튼 파괴"); // 일시적 권한 변경으로 재생성 금지
            _pause.ClosePanel(); // 전투 복귀
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.EqualTo(selected.gameObject)); // 이전 능력 포커스 복원
            Assert.That(_boardInput.SelectedPiece, Is.SameAs(sage)); // 기물 선택 유지
        } // 메서드 종료
        [UnityTest] // 방향키와 Shift 조합 검사
        public IEnumerator ShiftRightArrow_StillMovesToNextChoice() // Shift는 Tab 역방향에만 사용
        { // 메서드 시작
            int selected = -1; // 실행한 선택 번호
            Show(new StageOverlayOption("첫째", "효과", true, () => selected = 0), new StageOverlayOption("둘째", "효과", true, () => selected = 1), new StageOverlayOption("셋째", "효과", true, () => selected = 2)); // 실제 활성 선택 세 개
            Press(Key.Tab); // 첫 선택 포커스
            yield return null; // 키 처리
            Press(); // 키 해제
            yield return null; // 해제 반영
            Press(Key.LeftShift, Key.RightArrow); // 다음 방향 입력
            yield return null; // 방향 처리
            Press(); // 방향 해제
            yield return null; // 해제 반영
            Press(Key.Enter); // 선택 확정
            yield return null; // 실제 콜백
            Assert.That(selected, Is.EqualTo(1)); // 오른쪽은 항상 다음 선택
        } // 메서드 종료
        [UnityTest] // 일시정지 전 합성 모드 보존
        public IEnumerator Pause_PreservesActiveFusionModeUntilResumed() // 일시적 권한 차단과 실제 턴 변경 구분
        { // 메서드 시작
            var host = new GameObject("Day103FusionUi"); // 실제 합성 UI 호스트
            host.transform.SetParent(_root.transform); // 시험 소유
            host.AddComponent<Day64FusionUI>(); // 실제 합성 UI
            yield return null; // 실제 입력 연결
            Assert.That(_boardInput.SetFusionModeActive(true), Is.True); // 배치 중 합성 진입
            yield return null; // 실제 모드 반영
            _pause.OpenPanel(); // 합성 위 일시정지
            yield return null; // 합성 UI의 모달 상태 처리
            Assert.That(_boardInput.IsFusionModeActive, Is.True, "일시정지로 합성 모드 해제"); // 일시적 입력 차단 중 모드 보존
            _pause.ClosePanel(); // 배치 화면 복귀
            Press(); // 키 입력 해제
            yield return null; // 정상 입력 복원
            Assert.That(_boardInput.IsFusionModeActive, Is.True); // 원래 합성 상태 유지
        } // 메서드 종료
        [UnityTest] // 실제 배율 문구 검사
        public IEnumerator SettingsMaximumScale_Displays135Percent() // 100퍼센트를 넘는 배율 안내
        { // 메서드 시작
            _pause.OpenPanel(); // 설정 진입 준비
            _pause.ShowSettingsPanel(); // 실제 편집 UI
            Slider slider = _root.GetComponentsInChildren<Slider>(true).First(item => item.name == "UiScaleSlider"); // 실제 배율 입력
            slider.value = GameSettingsData.MaximumUiScale; // 최대 배율 편집
            Text label = slider.transform.parent.GetComponentsInChildren<Text>(true).First(item => item.name == "Value"); // 실제 배율 표시
            Assert.That(label.text, Is.EqualTo("135%")); // 볼륨 상한과 다른 UI 배율 표시
            yield return null; // 실제 프레임 마무리
        } // 메서드 종료
        private void Show(params StageOverlayOption[] options) // 실제 선택 페이지 표시
        { // 메서드 시작
            _activity.ShowPage(StageOverlayMode.Event, "103일차 화면 검사", "비용·효과 안내", options); // 실제 UI 연결
        } // 메서드 종료
        private Button Button(string title) // 실제 제목의 버튼 조회
        { // 메서드 시작
            return _root.GetComponentsInChildren<Button>(true).First(button => button.gameObject.activeInHierarchy && button.GetComponentsInChildren<Text>(true).Any(label => label.text == title)); // 표시된 실제 버튼
        } // 메서드 종료
        private void Press(params Key[] keys) // 시험 입력 프레임 갱신
        { // 메서드 시작
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys)); // 실제 키 상태 주입
            InputSystem.Update(); // 입력 시스템 갱신
            _keyboard.MakeCurrent(); // 시험 장치 유지
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
