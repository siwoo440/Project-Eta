using System.Text; // Console 출력 문자열 조립
using UnityEngine; // MonoBehaviour·IMGUI·화면 좌표 사용
using UnityEngine.InputSystem; // F1·마우스 입력 사용
using UnityEngine.SceneManagement; // 현재 씬 확인
using ProjectEta.AI; // AI 점수·성능 정보 사용
using ProjectEta.Battle; // 전투 상태·결과·배속 사용
using ProjectEta.Run; // 런 흐름·경제 상태 사용
using ProjectEta.SceneFlow; // Battle 초기화 진단 사용
using ProjectEta.UI; // 개발 빌드 표시 정책 사용

namespace ProjectEta.Debugging // 런타임 디버그 도구 네임스페이스
{ // 네임스페이스 범위
    public sealed class ProjectEtaDebugWindow : MonoBehaviour // F1 공통 디버그 패널
    { // 클래스 범위
        private const int StatusPage = 0; // 상태 페이지 번호
        private const int BattlePage = 1; // 전투 페이지 번호
        private const int AiScorePage = 2; // AI 페이지 번호
        private const float PanelMargin = 12f; // 화면 가장자리 여백
        private const float MinimumPanelWidth = 320f; // 작은 화면 최소 패널 폭
        private const float MaximumPanelWidth = 420f; // 큰 화면 최대 패널 폭
        private const float MaximumPanelHeight = 700f; // 최대 패널 높이
        private const float RefreshInterval = 0.50f; // 상태 자동 갱신 간격
        private static readonly Color ActiveTabColor = new Color(0.22f, 0.58f, 0.86f, 1f); // 선택 탭 파란색
        private static readonly Color VictoryColor = new Color(0.20f, 0.62f, 0.32f, 1f); // 승리 버튼 녹색
        private static readonly Color DefeatColor = new Color(0.76f, 0.24f, 0.22f, 1f); // 패배 버튼 붉은색
        private static readonly Color ActionColor = new Color(0.92f, 0.66f, 0.18f, 1f); // 개발 조작 노란색
        private static ProjectEtaDebugWindow _instance; // 전역 단일 패널 참조

        private readonly AIDebugScoreSnapshotBuilder _snapshotBuilder = new AIDebugScoreSnapshotBuilder(); // AI 점수 스냅샷 생성기
        private AIDebugScoreSnapshot _snapshot = AIDebugScoreSnapshot.Empty(); // 최신 AI 점수 정보
        private BattleRuntimeDiagnostics _runtimeDiagnostics = BattleRuntimeDiagnostics.Empty; // 최신 Battle 초기화 진단
        private BattleController _battleController; // 현재 전투 관리자
        private Rect _windowRect; // 왼쪽 패널 영역
        private Vector2 _statusScrollPosition; // 상태 페이지 스크롤 위치
        private Vector2 _battleScrollPosition; // 전투 페이지 스크롤 위치
        private Vector2 _aiScrollPosition; // AI 페이지 스크롤 위치
        private GUIStyle _sectionStyle; // 구역 상자 스타일
        private GUIStyle _sectionTitleStyle; // 구역 제목 스타일
        private GUIStyle _mutedLabelStyle; // 보조 문구 스타일
        private GUIStyle _statusValueStyle; // 상태 값 스타일
        private bool _isOpen; // F1 패널 열림 상태
        private int _currentPage = StatusPage; // 현재 페이지 번호
        private float _nextRefreshTime; // 다음 자동 갱신 시각

        public static int PageCount => 3; // 상태·전투·AI 페이지 수
        public static bool IsOpen => _instance != null && _instance._isOpen; // 패널 열림 여부

        public static bool IsPointerOverPanel // 패널 클릭 관통 차단 상태
        { // 프로퍼티 범위
            get // 값 반환 범위
            { // 접근자 범위
                if (!IsOpen || Mouse.current == null) return false; // 닫힌 패널·마우스 없음 처리
                return IsScreenPointInsidePanel(_instance._windowRect, Screen.height, Mouse.current.position.ReadValue()); // 현재 포인터 포함 여부 반환
            } // 접근자 종료
        } // 프로퍼티 종료

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // Play 세션 정적 상태 초기화
        private static void ResetStaticState() // 정적 인스턴스 초기화
        { // 메서드 범위
            _instance = null; // 이전 세션 참조 제거
        } // 메서드 종료

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // 첫 씬 로드 뒤 개발 패널 생성
        private static void AutoCreate() // 개발 환경 자동 생성
        { // 메서드 범위
            if (!ShouldCreateDebugPanel(Application.isEditor, Debug.isDebugBuild)) return; // Release Build 생성 차단
            if (_instance != null) return; // 중복 생성 차단

            var debugObject = new GameObject("ProjectEtaDebugWindow_Day82"); // 디버그 패널 호스트 생성
            _instance = debugObject.AddComponent<ProjectEtaDebugWindow>(); // 패널 컴포넌트 연결
            DontDestroyOnLoad(debugObject); // 씬 전환 유지
        } // 메서드 종료

        public static bool ShouldCreateDebugPanel(bool isEditor, bool isDebugBuild) // 개발 패널 생성 정책
        { // 메서드 범위
            return Day69UiPresentationRules.ShouldCreateDevelopmentUi(isEditor, isDebugBuild); // 공통 개발 UI 정책 반환
        } // 메서드 종료

        public static Rect CalculateWindowRect(int screenWidth, int screenHeight) // 화면 크기별 왼쪽 패널 영역 계산
        { // 메서드 범위
            float availableWidth = Mathf.Max(1f, screenWidth - PanelMargin * 2f); // 가로 사용 가능 영역 계산
            float availableHeight = Mathf.Max(1f, screenHeight - PanelMargin * 2f); // 세로 사용 가능 영역 계산
            float preferredWidth = Mathf.Clamp(screenWidth * 0.22f, MinimumPanelWidth, MaximumPanelWidth); // 화면 비율 기반 패널 폭 계산
            float width = Mathf.Min(preferredWidth, availableWidth); // 가로 경계 안쪽 폭 선택
            float height = Mathf.Min(MaximumPanelHeight, availableHeight); // 세로 경계 안쪽 높이 선택
            return new Rect(PanelMargin, PanelMargin, width, height); // 왼쪽 위 고정 영역 반환
        } // 메서드 종료

        public static bool IsScreenPointInsidePanel(Rect panelRect, int screenHeight, Vector2 screenPosition) // Input System 좌표의 패널 포함 여부
        { // 메서드 범위
            Vector2 guiPosition = new Vector2(screenPosition.x, screenHeight - screenPosition.y); // 왼쪽 아래 좌표를 왼쪽 위 좌표로 변환
            return panelRect.Contains(guiPosition); // 패널 내부 여부 반환
        } // 메서드 종료

        public static bool CanUseBattleResult(TurnManager turnManager, RunFlowPhase flowPhase) // 강제 승패 사용 규칙
        { // 메서드 범위
            if (turnManager == null) return false; // 턴 상태 누락 차단
            if (turnManager.CurrentState == TurnState.BattleEnded) return false; // 종료 전투 중복 입력 차단
            return flowPhase == RunFlowPhase.Battle; // 실제 전투 흐름만 허용
        } // 메서드 종료

        public static bool CanChangeCombatSpeed(RunFlowPhase flowPhase, float timeScale) // 전투 배속 변경 규칙
        { // 메서드 범위
            if (flowPhase != RunFlowPhase.Battle) return false; // 전투 외 흐름 변경 차단
            return timeScale > 0f; // 일시정지·튜토리얼 정지 중 변경 차단
        } // 메서드 종료

        private void Awake() // 직접 배치·자동 생성 공통 초기화
        { // 메서드 범위
            if (!ShouldCreateDebugPanel(Application.isEditor, Debug.isDebugBuild)) // 출시 환경 생성 여부 확인
            { // 조건 범위
                Destroy(gameObject); // 출시 환경 패널 호스트 제거
                return; // 초기화 종료
            } // 조건 종료

            if (_instance != null && _instance != this) // 중복 인스턴스 확인
            { // 조건 범위
                Destroy(gameObject); // 중복 호스트 제거
                return; // 초기화 종료
            } // 조건 종료

            _instance = this; // 현재 인스턴스 등록
            _windowRect = CalculateWindowRect(Screen.width, Screen.height); // 최초 왼쪽 패널 영역 계산
            DontDestroyOnLoad(gameObject); // 씬 전환 유지
        } // 메서드 종료

        private void Update() // F1 입력·상태 자동 갱신
        { // 메서드 범위
            _windowRect = CalculateWindowRect(Screen.width, Screen.height); // 해상도 변화 즉시 반영

            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame) // F1 입력 확인
            { // 조건 범위
                _isOpen = !_isOpen; // 열림 상태 전환

                if (_isOpen) // 새로 열린 상태 확인
                { // 조건 범위
                    RefreshPanelData(); // 현재 게임 상태 즉시 갱신
                    _nextRefreshTime = Time.unscaledTime + RefreshInterval; // 다음 갱신 예약
                } // 조건 종료
            } // 조건 종료

            if (!_isOpen) return; // 닫힌 패널 갱신 생략
            if (Time.unscaledTime < _nextRefreshTime) return; // 갱신 간격 이전 처리 생략

            RefreshPanelData(); // 열린 페이지 데이터 갱신
            _nextRefreshTime = Time.unscaledTime + RefreshInterval; // 다음 갱신 예약
        } // 메서드 종료

        private void RefreshPanelData() // 현재 씬의 디버그 데이터 갱신
        { // 메서드 범위
            if (SceneManager.GetActiveScene().name != "Battle") // Battle 씬 여부 확인
            { // 조건 범위
                _battleController = null; // 이전 전투 참조 제거
                _snapshot = AIDebugScoreSnapshot.Empty(); // AI 정보 초기화
                _runtimeDiagnostics = BattleRuntimeDiagnostics.Empty; // Battle 진단 초기화
                return; // 갱신 종료
            } // 조건 종료

            _runtimeDiagnostics = SceneRuntimeBootstrap.RefreshBattleDiagnostics(); // 현재 관리자 누락·중복 갱신
            if (_battleController == null) _battleController = Object.FindFirstObjectByType<BattleController>(); // 전투 관리자 최초 탐색

            if (_currentPage != AiScorePage) return; // AI 페이지 외 평가 생략
            if (_battleController == null || _battleController.RunState == null) // AI 평가 필수 상태 확인
            { // 조건 범위
                _snapshot = AIDebugScoreSnapshot.Empty(); // 빈 AI 정보 적용
                return; // 평가 종료
            } // 조건 종료

            _snapshot = _snapshotBuilder.Build(_battleController.RunState.Board); // 현재 보드 AI 점수 한 번 계산
        } // 메서드 종료

        private void OnGUI() // IMGUI 패널 그리기
        { // 메서드 범위
            if (!_isOpen) return; // 닫힌 상태 그리기 생략

            EnsureStyles(); // 패널 전용 스타일 준비
            _windowRect = CalculateWindowRect(Screen.width, Screen.height); // 왼쪽 고정 위치 적용
            GUI.Window(GetInstanceID(), _windowRect, DrawWindow, "PROJECT η  ·  DEBUG PANEL"); // 왼쪽 디버그 창 출력
        } // 메서드 종료

        private void EnsureStyles() // IMGUI 스타일 한 번 생성
        { // 메서드 범위
            if (_sectionStyle != null) return; // 기존 스타일 재사용

            _sectionStyle = new GUIStyle(GUI.skin.box); // 기본 상자 스타일 복사
            _sectionStyle.padding = new RectOffset(10, 10, 8, 9); // 구역 내부 여백 적용
            _sectionStyle.margin = new RectOffset(2, 2, 5, 5); // 구역 외부 여백 적용
            _sectionStyle.alignment = TextAnchor.UpperLeft; // 내용 왼쪽 정렬
            _sectionTitleStyle = new GUIStyle(GUI.skin.label); // 기본 라벨 스타일 복사
            _sectionTitleStyle.fontStyle = FontStyle.Bold; // 구역 제목 굵게 표시
            _sectionTitleStyle.fontSize = 13; // 구역 제목 크기 적용
            _sectionTitleStyle.normal.textColor = new Color(0.46f, 0.82f, 1f, 1f); // 구역 제목 청색 적용
            _mutedLabelStyle = new GUIStyle(GUI.skin.label); // 보조 라벨 스타일 복사
            _mutedLabelStyle.fontSize = 10; // 보조 글자 크기 적용
            _mutedLabelStyle.normal.textColor = new Color(0.68f, 0.72f, 0.76f, 1f); // 보조 글자 회색 적용
            _mutedLabelStyle.wordWrap = true; // 긴 안내 자동 줄바꿈
            _statusValueStyle = new GUIStyle(GUI.skin.label); // 상태 값 스타일 복사
            _statusValueStyle.alignment = TextAnchor.MiddleRight; // 상태 값 오른쪽 정렬
            _statusValueStyle.fontStyle = FontStyle.Bold; // 상태 값 굵게 표시
        } // 메서드 종료

        private void DrawWindow(int windowId) // 패널 전체 내용 출력
        { // 메서드 범위
            DrawHeader(); // 상단 상태·닫기 영역 출력
            DrawTabs(); // 페이지 탭 출력
            GUILayout.Space(4f); // 탭 아래 간격

            if (_currentPage == StatusPage) DrawStatusPage(); // 상태 페이지 출력
            else if (_currentPage == BattlePage) DrawBattlePage(); // 전투 페이지 출력
            else DrawAiScorePage(); // AI 페이지 출력

            GUILayout.FlexibleSpace(); // 하단 상태줄 밀어내기
            DrawFooter(); // 성능·빌드 상태 출력
        } // 메서드 종료

        private void DrawHeader() // 패널 상단 요약 출력
        { // 메서드 범위
            GUILayout.BeginHorizontal(); // 상단 가로 영역 시작
            GUILayout.Label("F1 열기 / 닫기", _mutedLabelStyle); // 단축키 안내
            GUILayout.FlexibleSpace(); // 닫기 버튼 오른쪽 정렬

            if (GUILayout.Button("닫기", GUILayout.Width(54f), GUILayout.Height(22f))) _isOpen = false; // 패널 닫기 처리

            GUILayout.EndHorizontal(); // 상단 가로 영역 종료
            GUILayout.Label(BuildHeaderSummary(), _mutedLabelStyle); // 현재 씬·흐름 요약 출력
        } // 메서드 종료

        private string BuildHeaderSummary() // 공통 상태 한 줄 생성
        { // 메서드 범위
            string sceneName = SceneManager.GetActiveScene().name; // 현재 씬 이름 읽기
            string flow = _battleController?.RunState != null ? _battleController.RunState.CurrentFlowPhase.ToString() : "-"; // 현재 런 흐름 읽기
            string turn = _battleController?.TurnManager != null ? _battleController.TurnManager.CurrentState.ToString() : "-"; // 현재 턴 읽기
            return $"Scene  {sceneName}   /   Flow  {flow}   /   Turn  {turn}"; // 요약 문자열 반환
        } // 메서드 종료

        private void DrawTabs() // 상태·전투·AI 탭 출력
        { // 메서드 범위
            GUILayout.BeginHorizontal(); // 탭 가로 영역 시작
            DrawTab(StatusPage, "상태"); // 상태 탭 출력
            DrawTab(BattlePage, "전투 도구"); // 전투 탭 출력
            DrawTab(AiScorePage, "AI 분석"); // AI 탭 출력
            GUILayout.EndHorizontal(); // 탭 가로 영역 종료
        } // 메서드 종료

        private void DrawTab(int page, string label) // 단일 탭 출력
        { // 메서드 범위
            Color previousColor = GUI.backgroundColor; // 기존 버튼 색 저장
            if (_currentPage == page) GUI.backgroundColor = ActiveTabColor; // 선택 탭 파란색 적용

            if (GUILayout.Button(label, GUILayout.Height(28f))) // 탭 클릭 확인
            { // 조건 범위
                bool changed = _currentPage != page; // 페이지 변경 여부 계산
                _currentPage = page; // 선택 페이지 적용
                if (changed) RefreshPanelData(); // 새 페이지 데이터 즉시 갱신
            } // 조건 종료

            GUI.backgroundColor = previousColor; // 기존 버튼 색 복원
        } // 메서드 종료

        private void DrawStatusPage() // 읽기 전용 현재 상태 출력
        { // 메서드 범위
            _statusScrollPosition = GUILayout.BeginScrollView(_statusScrollPosition); // 상태 스크롤 시작
            BeginSection("실행 상태"); // 실행 상태 구역 시작
            DrawKeyValue("Scene", SceneManager.GetActiveScene().name); // 씬 이름 출력
            DrawKeyValue("Build", GetBuildLabel()); // 빌드 종류 출력
            DrawKeyValue("Debug Panel", "F1 / 왼쪽 고정"); // 패널 위치 출력
            EndSection(); // 실행 상태 구역 종료

            if (_runtimeDiagnostics.RequiredCount > 0) // Battle 초기화 진단 존재 확인
            { // 조건 범위
                BeginSection("런타임 초기화"); // 초기화 진단 구역 시작
                DrawKeyValue("상태", _runtimeDiagnostics.IsHealthy ? "정상" : "확인 필요"); // 전체 상태 출력
                DrawKeyValue("관리자", $"{_runtimeDiagnostics.ReadyCount} / {_runtimeDiagnostics.RequiredCount}"); // 관리자 준비 수 출력
                DrawKeyValue("누락", _runtimeDiagnostics.MissingNames.Count.ToString()); // 누락 수 출력
                DrawKeyValue("중복", _runtimeDiagnostics.DuplicateNames.Count.ToString()); // 중복 수 출력
                DrawKeyValue("초기화", $"{_runtimeDiagnostics.ElapsedMilliseconds:0.0} ms"); // 초기화 소요 시간 출력

                for (int index = 0; index < _runtimeDiagnostics.MissingNames.Count; index++) // 누락 관리자 순회
                { // 반복 범위
                    GUILayout.Label($"누락 · {_runtimeDiagnostics.MissingNames[index]}", _mutedLabelStyle); // 누락 관리자 출력
                } // 반복 종료

                for (int index = 0; index < _runtimeDiagnostics.DuplicateNames.Count; index++) // 중복 관리자 순회
                { // 반복 범위
                    GUILayout.Label($"중복 · {_runtimeDiagnostics.DuplicateNames[index]}", _mutedLabelStyle); // 중복 관리자 출력
                } // 반복 종료

                EndSection(); // 초기화 진단 구역 종료
            } // 조건 종료

            if (_battleController == null || _battleController.RunState == null) // 전투 상태 누락 확인
            { // 조건 범위
                BeginSection("전투 연결"); // 연결 안내 구역 시작
                GUILayout.Label("Battle 씬에서 전투 상태가 준비되면 상세 정보가 표시됩니다.", _mutedLabelStyle); // 연결 대기 안내
                EndSection(); // 연결 안내 구역 종료
                GUILayout.EndScrollView(); // 상태 스크롤 종료
                return; // 상태 페이지 종료
            } // 조건 종료

            RunState runState = _battleController.RunState; // 현재 런 상태 저장
            TurnManager turnManager = _battleController.TurnManager; // 현재 턴 상태 저장
            BeginSection("런 진행"); // 런 진행 구역 시작
            DrawKeyValue("Flow", runState.CurrentFlowPhase.ToString()); // 런 흐름 출력
            DrawKeyValue("Board Mode", runState.CurrentBoardMode.ToString()); // 보드 모드 출력
            DrawKeyValue("Stage", runState.CurrentRound.ToString()); // 현재 스테이지 출력
            DrawKeyValue("Route Depth", runState.RouteMap.CurrentDepth.ToString()); // 지도 깊이 출력
            DrawKeyValue("Route Seed", runState.RouteMap.MapSeed.ToString()); // 지도 Seed 출력
            EndSection(); // 런 진행 구역 종료

            BeginSection("전투 상태"); // 전투 상태 구역 시작
            DrawKeyValue("Turn", turnManager != null ? turnManager.CurrentState.ToString() : "-"); // 턴 상태 출력
            DrawKeyValue("Turn Number", turnManager != null ? turnManager.TurnNumber.ToString() : "-"); // 턴 번호 출력
            DrawKeyValue("Result", runState.LastBattleOutcome.ToString()); // 전투 결과 출력
            DrawKeyValue("King HP", runState.KingHp.ToString()); // 킹 체력 출력
            DrawKeyValue("Player / Enemy", $"{runState.Board.CountPieces(true)} / {runState.Board.CountPieces(false)}"); // 양측 기물 수 출력
            EndSection(); // 전투 상태 구역 종료

            BeginSection("카드·재화"); // 카드·재화 구역 시작
            DrawKeyValue("Owned Cards", runState.Deck.OwnedCardPool.Count.ToString()); // 보유 카드 수 출력
            DrawKeyValue("Dead Cards", runState.Deck.DeadCardPile.Count.ToString()); // 죽은 카드 수 출력
            DrawKeyValue("Hand", runState.Hand.Hand.Count.ToString()); // 손패 수 출력
            DrawKeyValue("Gold", GetCurrencyText(runState)); // 런 재화 출력
            EndSection(); // 카드·재화 구역 종료
            GUILayout.EndScrollView(); // 상태 스크롤 종료
        } // 메서드 종료

        private void DrawBattlePage() // 전투 개발 도구 출력
        { // 메서드 범위
            _battleScrollPosition = GUILayout.BeginScrollView(_battleScrollPosition); // 전투 스크롤 시작
            RunState runState = _battleController != null ? _battleController.RunState : null; // 현재 런 상태 읽기
            TurnManager turnManager = _battleController != null ? _battleController.TurnManager : null; // 현재 턴 상태 읽기
            bool canUseResult = runState != null && CanUseBattleResult(turnManager, runState.CurrentFlowPhase); // 강제 결과 사용 가능 여부 계산
            bool canChangeSpeed = runState != null && CanChangeCombatSpeed(runState.CurrentFlowPhase, Time.timeScale); // 전투 배속 변경 가능 여부 계산

            BeginSection("현재 전투"); // 현재 전투 구역 시작
            DrawKeyValue("State", turnManager != null ? turnManager.CurrentState.ToString() : "-"); // 턴 상태 출력
            DrawKeyValue("Turn", turnManager != null ? turnManager.TurnNumber.ToString() : "-"); // 턴 번호 출력
            DrawKeyValue("Result", turnManager != null ? turnManager.Outcome.ToString() : "-"); // 결과 상태 출력
            EndSection(); // 현재 전투 구역 종료

            BeginSection("강제 전투 결과"); // 강제 결과 구역 시작
            bool previousEnabled = GUI.enabled; // 기존 입력 활성 상태 저장
            GUI.enabled = canUseResult; // 현재 전투에서만 결과 버튼 활성화
            GUILayout.BeginHorizontal(); // 승패 버튼 가로 영역 시작
            DrawColoredActionButton("강제 승리", VictoryColor, ForceVictory); // 강제 승리 버튼 출력
            DrawColoredActionButton("강제 패배", DefeatColor, ForceDefeat); // 강제 패배 버튼 출력
            GUILayout.EndHorizontal(); // 승패 버튼 가로 영역 종료
            GUI.enabled = previousEnabled; // 기존 입력 활성 상태 복원
            GUILayout.Label(canUseResult ? "실제 BattleController.EndBattle 흐름을 실행합니다." : "전투 진행 중에만 사용할 수 있습니다.", _mutedLabelStyle); // 결과 도구 설명
            EndSection(); // 강제 결과 구역 종료

            BeginSection("전투 속도"); // 전투 속도 구역 시작
            DrawKeyValue("현재 속도", $"×{CombatSpeedSettings.CurrentSpeed}"); // 현재 배속 출력
            previousEnabled = GUI.enabled; // 기존 입력 활성 상태 다시 저장
            GUI.enabled = canChangeSpeed; // 진행 중인 비정지 전투에서만 배속 활성화
            GUILayout.BeginHorizontal(); // 배속 버튼 가로 영역 시작
            DrawSpeedButton(1); // 1배속 버튼 출력
            DrawSpeedButton(2); // 2배속 버튼 출력
            DrawSpeedButton(3); // 3배속 버튼 출력
            GUILayout.EndHorizontal(); // 배속 버튼 가로 영역 종료
            GUI.enabled = previousEnabled; // 기존 입력 활성 상태 다시 복원
            GUILayout.Label(canChangeSpeed ? "기존 전투 속도를 기준으로 1·2·3단계를 선택합니다." : "일시정지·튜토리얼 또는 전투 외 상태에서는 변경할 수 없습니다.", _mutedLabelStyle); // 배속 도구 설명
            EndSection(); // 전투 속도 구역 종료
            GUILayout.EndScrollView(); // 전투 스크롤 종료
        } // 메서드 종료

        private void DrawColoredActionButton(string label, Color color, System.Action action) // 색상 개발 버튼 출력
        { // 메서드 범위
            Color previousColor = GUI.backgroundColor; // 기존 버튼 색 저장
            GUI.backgroundColor = color; // 개발 버튼 색 적용
            if (GUILayout.Button(label, GUILayout.Height(38f))) action?.Invoke(); // 클릭 시 개발 명령 실행
            GUI.backgroundColor = previousColor; // 기존 버튼 색 복원
        } // 메서드 종료

        private void DrawSpeedButton(int speed) // 단일 배속 버튼 출력
        { // 메서드 범위
            Color previousColor = GUI.backgroundColor; // 기존 버튼 색 저장
            if (CombatSpeedSettings.CurrentSpeed == speed) GUI.backgroundColor = ActionColor; // 선택 배속 노란색 적용
            if (GUILayout.Button($"×{speed}", GUILayout.Height(30f))) CombatSpeedSettings.TrySetSpeed(speed); // 선택 배속 적용
            GUI.backgroundColor = previousColor; // 기존 버튼 색 복원
        } // 메서드 종료

        private void ForceVictory() // 강제 승리 실행
        { // 메서드 범위
            if (!CanTriggerBattleResult()) return; // 잘못된 상태 입력 차단
            Debug.Log("82일차 F1 디버그 패널: 강제 승리 처리"); // 개발 결과 로그 출력
            _battleController.EndBattle(BattleOutcome.Victory); // 실제 승리 종료 흐름 실행
            RefreshPanelData(); // 종료 뒤 패널 상태 갱신
        } // 메서드 종료

        private void ForceDefeat() // 강제 패배 실행
        { // 메서드 범위
            if (!CanTriggerBattleResult()) return; // 잘못된 상태 입력 차단
            Debug.Log("82일차 F1 디버그 패널: 강제 패배 처리"); // 개발 결과 로그 출력
            _battleController.EndBattle(BattleOutcome.Defeat); // 실제 패배 종료 흐름 실행
            RefreshPanelData(); // 종료 뒤 패널 상태 갱신
        } // 메서드 종료

        private bool CanTriggerBattleResult() // 현재 실제 전투 결과 입력 가능 여부
        { // 메서드 범위
            if (_battleController == null || _battleController.RunState == null) return false; // 전투·런 상태 누락 차단
            return CanUseBattleResult(_battleController.TurnManager, _battleController.RunState.CurrentFlowPhase); // 공통 규칙 반환
        } // 메서드 종료

        private void DrawAiScorePage() // AI 분석 페이지 출력
        { // 메서드 범위
            GUILayout.BeginHorizontal(); // AI 도구 가로 영역 시작
            if (GUILayout.Button("갱신", GUILayout.Width(66f))) RefreshPanelData(); // AI 수동 갱신
            if (GUILayout.Button("Console 출력", GUILayout.Width(104f))) DumpCurrentScoresToConsole(); // AI 정보 Console 출력
            GUILayout.FlexibleSpace(); // 페이지 표시 오른쪽 정렬
            GUILayout.Label("Page 3 / 3", _mutedLabelStyle); // 현재 페이지 번호 출력
            GUILayout.EndHorizontal(); // AI 도구 가로 영역 종료

            var preview = _snapshot.PerformanceStats ?? EnemyAIPerformanceStats.Empty; // Preview 성능 정보 읽기
            var actual = EnemyAITurnDriver.LastTurnPerformanceStats ?? EnemyAIPerformanceStats.Empty; // 실제 적 턴 성능 정보 읽기
            BeginSection("AI 성능"); // AI 성능 구역 시작
            DrawKeyValue("후보", $"{_snapshot.CandidateCount} / {_snapshot.TotalCandidateCount}"); // 후보 수 출력
            DrawKeyValue("Preview", $"{preview.ElapsedMilliseconds:0.###} ms"); // Preview 시간 출력
            DrawKeyValue("Last EnemyTurn", $"{actual.ElapsedMilliseconds:0.###} ms"); // 실제 적 턴 시간 출력
            DrawKeyValue("Budget / Fallback", $"{BoolMark(preview.BudgetCapped)} / {BoolMark(preview.UsedFallback)}"); // 예산 제한·대체 처리 출력
            EndSection(); // AI 성능 구역 종료

            BeginSection("선택 행동"); // 선택 행동 구역 시작
            GUILayout.Label(_snapshot.SelectedEntry != null ? FormatEntry(_snapshot.SelectedEntry) : "선택 가능한 AI 행동 없음", _mutedLabelStyle); // 선택 행동 출력
            EndSection(); // 선택 행동 구역 종료

            GUILayout.Label("행동 후보", _sectionTitleStyle); // 후보 목록 제목 출력
            _aiScrollPosition = GUILayout.BeginScrollView(_aiScrollPosition, GUI.skin.box); // AI 후보 스크롤 시작

            for (int i = 0; i < _snapshot.Entries.Count; i++) // AI 후보 전체 순회
            { // 반복 범위
                AIDebugScoreEntry entry = _snapshot.Entries[i]; // 현재 후보 읽기
                string prefix = entry.IsSelected ? "▶ " : "   "; // 선택 후보 표시 계산
                GUILayout.Label(prefix + FormatEntry(entry), _mutedLabelStyle); // 후보 점수 출력
            } // 반복 종료

            GUILayout.EndScrollView(); // AI 후보 스크롤 종료
        } // 메서드 종료

        private void BeginSection(string title) // 공통 정보 구역 시작
        { // 메서드 범위
            GUILayout.BeginVertical(_sectionStyle); // 상자 영역 시작
            GUILayout.Label(title, _sectionTitleStyle); // 구역 제목 출력
        } // 메서드 종료

        private static void EndSection() // 공통 정보 구역 종료
        { // 메서드 범위
            GUILayout.EndVertical(); // 상자 영역 종료
        } // 메서드 종료

        private void DrawKeyValue(string key, string value) // 이름·값 한 줄 출력
        { // 메서드 범위
            GUILayout.BeginHorizontal(); // 상태 가로 영역 시작
            GUILayout.Label(key, GUILayout.Width(132f)); // 상태 이름 출력
            GUILayout.Label(value ?? "-", _statusValueStyle); // 상태 값 출력
            GUILayout.EndHorizontal(); // 상태 가로 영역 종료
        } // 메서드 종료

        private void DrawFooter() // 하단 빌드·성능 표시
        { // 메서드 범위
            float frameMilliseconds = Time.unscaledDeltaTime > 0f ? Time.unscaledDeltaTime * 1000f : 0f; // 현재 프레임 시간 계산
            float framesPerSecond = Time.unscaledDeltaTime > 0f ? 1f / Time.unscaledDeltaTime : 0f; // 현재 FPS 계산
            GUILayout.Space(3f); // 하단 구분 간격
            GUILayout.Label($"{GetBuildLabel()}   ·   FPS {framesPerSecond:0}   ·   Frame {frameMilliseconds:0.0} ms   ·   F1", _mutedLabelStyle); // 하단 상태 출력
        } // 메서드 종료

        private static string GetBuildLabel() // 현재 빌드 종류 이름 반환
        { // 메서드 범위
            if (Application.isEditor) return "EDITOR"; // 에디터 표시 반환
            return Debug.isDebugBuild ? "DEVELOPMENT" : "RELEASE"; // 플레이어 빌드 종류 반환
        } // 메서드 종료

        private static string GetCurrencyText(RunState runState) // 현재 런 재화 문자열 반환
        { // 메서드 범위
            return RunEconomyService.TryGet(runState, out RunEconomyState economy) ? economy.Currency.ToString() : "-"; // 기존 경제 상태만 읽기
        } // 메서드 종료

        private static string FormatEntry(AIDebugScoreEntry entry) // AI 점수 한 줄 생성
        { // 메서드 범위
            if (entry == null) return "-"; // 빈 후보 처리

            string actorName = entry.Actor?.Definition != null ? entry.Actor.Definition.DisplayName : "Unknown"; // 행동 기물 이름 계산
            return $"[{entry.Role}] {actorName} {entry.Origin}>{entry.Target} {entry.ActionType} | B{entry.BaseScore} R{Signed(entry.RoleBonus)} T{Signed(entry.ThreatScore)} S{Signed(entry.SpecialBonus)} F{entry.FinalScore}"; // 점수 문자열 반환
        } // 메서드 종료

        private static string Signed(int value) // 점수 부호 문자열 생성
        { // 메서드 범위
            return value >= 0 ? $"+{value}" : value.ToString(); // 양수 부호 포함 값 반환
        } // 메서드 종료

        private static string BoolMark(bool value) // bool 축약 표시 생성
        { // 메서드 범위
            return value ? "Y" : "N"; // Y·N 표시 반환
        } // 메서드 종료

        private void DumpCurrentScoresToConsole() // 현재 AI 점수 Console 출력
        { // 메서드 범위
            var builder = new StringBuilder(); // 로그 문자열 빌더 생성
            var preview = _snapshot.PerformanceStats ?? EnemyAIPerformanceStats.Empty; // Preview 성능 읽기
            var actual = EnemyAITurnDriver.LastTurnPerformanceStats ?? EnemyAIPerformanceStats.Empty; // 실제 적 턴 성능 읽기
            builder.AppendLine($"[AI DEBUG] 평가 후보: {_snapshot.CandidateCount}/{_snapshot.TotalCandidateCount}"); // 후보 수 추가
            builder.AppendLine($"Preview: {preview}"); // Preview 통계 추가
            builder.AppendLine($"Last EnemyTurn: {actual}"); // 실제 적 턴 통계 추가
            builder.AppendLine("B=Base / R=Role / T=Threat / S=Special / F=Final"); // 점수 약어 추가

            for (int i = 0; i < _snapshot.Entries.Count; i++) // 현재 후보 전체 순회
            { // 반복 범위
                AIDebugScoreEntry entry = _snapshot.Entries[i]; // 현재 후보 읽기
                builder.Append(entry.IsSelected ? "SELECT > " : "         "); // 선택 표시 추가
                builder.AppendLine(FormatEntry(entry)); // 후보 한 줄 추가
            } // 반복 종료

            Debug.Log(builder.ToString()); // 완성 로그 출력
        } // 메서드 종료

        private void OnDestroy() // 패널 제거 시 정적 참조 정리
        { // 메서드 범위
            if (_instance == this) _instance = null; // 현재 인스턴스 참조 제거
        } // 메서드 종료
    } // 클래스 종료
} // 네임스페이스 종료
