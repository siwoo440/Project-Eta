using System.Text; // Unity Console에 여러 점수 로그를 한 번에 출력하기 위한 StringBuilder 네임스페이스
using UnityEngine; // MonoBehaviour, GUI, GUILayout, Rect 등을 사용하기 위한 네임스페이스
using UnityEngine.InputSystem; // F1 키 입력을 새 Input System으로 확인하기 위한 네임스페이스
using UnityEngine.SceneManagement; // 현재 씬이 Battle인지 확인하기 위한 네임스페이스
using ProjectEta.AI; // AI 점수 스냅샷 빌더와 로그 데이터를 사용하기 위한 네임스페이스
using ProjectEta.Battle; // BattleController와 TurnState를 읽기 위한 네임스페이스

namespace ProjectEta.Debugging // 런타임 디버그 도구를 모아두는 별도 네임스페이스
{
    public sealed class ProjectEtaDebugWindow : MonoBehaviour // F1로 여닫는 프로젝트 η 공통 런타임 디버그 창
    {
        private const int AiScorePage = 0; // 첫 번째 페이지 인덱스는 AI 점수 로그
        private const float RefreshInterval = 0.20f; // 창이 열려 있을 때 점수 목록을 갱신하는 간격
        private static ProjectEtaDebugWindow _instance; // 씬 전환 후에도 하나만 유지할 싱글 인스턴스

        private readonly AIDebugScoreSnapshotBuilder _snapshotBuilder = new AIDebugScoreSnapshotBuilder(); // AI 점수 로그 생성기
        private AIDebugScoreSnapshot _snapshot = AIDebugScoreSnapshot.Empty(); // 현재 화면에 표시할 최신 AI 점수 스냅샷
        private BattleController _battleController; // 현재 Battle 씬의 전투 상태 소유자
        private Rect _windowRect = new Rect(20f, 20f, 980f, 680f); // 최초 디버그 창 위치와 크기
        private Vector2 _scrollPosition; // 첫 페이지 후보 로그 스크롤 위치
        private bool _isOpen; // F1 디버그 창 열림 여부
        private int _currentPage = AiScorePage; // 현재 페이지 번호
        private float _nextRefreshTime; // 다음 점수 스냅샷을 갱신할 시간

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // 게임 시작 첫 씬 로드 직후 자동 실행
        private static void AutoCreate() // 인스펙터 설정 없이 전역 디버그 창 오브젝트를 자동 생성하는 메서드
        {
            if (_instance != null) return; // 이미 존재하면 중복 생성하지 않음

            var debugObject = new GameObject("ProjectEtaDebugWindow"); // 디버그 창 전용 GameObject 생성
            _instance = debugObject.AddComponent<ProjectEtaDebugWindow>(); // 컴포넌트를 추가하고 싱글 인스턴스 저장
            DontDestroyOnLoad(debugObject); // 메인 메뉴와 Battle 씬 사이를 이동해도 디버그 도구 유지
        }

        private void Awake() // 씬에 수동으로 들어간 경우까지 포함해 중복 인스턴스를 방지하는 초기화
        {
            if (_instance != null && _instance != this) // 이미 다른 인스턴스가 존재하면
            {
                Destroy(gameObject); // 현재 중복 오브젝트 제거
                return; // 더 이상 초기화하지 않음
            }

            _instance = this; // 현재 컴포넌트를 전역 인스턴스로 등록
            DontDestroyOnLoad(gameObject); // 직접 배치된 경우에도 씬 전환 시 유지
        }

        private void Update() // F1 입력과 열린 창의 실시간 점수 갱신을 처리하는 메서드
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame) // 이번 프레임에 F1을 눌렀으면
            {
                _isOpen = !_isOpen; // 디버그 창 열림 상태 반전

                if (_isOpen) // 새로 창을 연 순간이면
                {
                    RefreshSnapshot(); // 즉시 현재 AI 점수를 한 번 읽어 표시
                    _nextRefreshTime = Time.unscaledTime + RefreshInterval; // 다음 갱신 시간 예약
                }
            }

            if (!_isOpen) return; // 창이 닫혀 있으면 추가 계산을 하지 않음
            if (Time.unscaledTime < _nextRefreshTime) return; // 아직 갱신 간격이 지나지 않았으면 기존 스냅샷 유지

            RefreshSnapshot(); // 현재 보드의 최신 AI 후보 점수를 다시 계산
            _nextRefreshTime = Time.unscaledTime + RefreshInterval; // 다음 갱신 시간 예약
        }

        private void RefreshSnapshot() // 현재 Battle 씬과 RunState를 찾아 AI 점수 스냅샷을 갱신하는 메서드
        {
            if (SceneManager.GetActiveScene().name != "Battle") // 현재 씬이 Battle이 아니면
            {
                _battleController = null; // 이전 BattleController 참조 제거
                _snapshot = AIDebugScoreSnapshot.Empty(); // 빈 로그 표시
                return; // 전투 데이터가 없으므로 종료
            }

            if (_battleController == null) // 아직 현재 BattleController 참조가 없으면
            {
                _battleController = Object.FindFirstObjectByType<BattleController>(); // 씬에서 실제 전투 컨트롤러 탐색
            }

            if (_battleController == null || _battleController.RunState == null) // 전투 상태가 아직 생성되지 않았다면
            {
                _snapshot = AIDebugScoreSnapshot.Empty(); // 빈 로그 유지
                return; // 다음 갱신 주기에 다시 탐색
            }

            _snapshot = _snapshotBuilder.Build(_battleController.RunState.Board); // 실제 현재 보드로 점수 로그 생성
        }

        private void OnGUI() // Unity IMGUI를 사용해 별도 Canvas 설정 없이 디버그 창을 그리는 메서드
        {
            if (!_isOpen) return; // F1 창이 닫혀 있으면 아무 것도 그리지 않음

            _windowRect = GUI.Window( // 이동 가능한 독립 디버그 창 생성
                GetInstanceID(), // 이 컴포넌트만의 고유 창 ID
                _windowRect, // 현재 위치와 크기
                DrawWindow, // 창 내부를 그릴 콜백
                "Project η Debug"); // 창 제목
        }

        private void DrawWindow(int windowId) // 디버그 창 내부의 페이지 헤더와 내용을 그리는 메서드
        {
            GUILayout.BeginHorizontal(); // 상단 페이지 메뉴를 가로로 시작
            GUILayout.Label("F1 : 열기/닫기", GUILayout.Width(120f)); // 단축키 안내 표시
            GUILayout.Space(10f); // 메뉴 사이 간격
            GUILayout.Label("Page 1 / 1", GUILayout.Width(90f)); // 현재는 첫 번째 페이지만 존재함을 표시

            if (GUILayout.Toggle(_currentPage == AiScorePage, "1. AI 점수 로그", GUI.skin.button, GUILayout.Width(140f))) // 첫 번째 페이지 버튼
            {
                _currentPage = AiScorePage; // 버튼 클릭 시 AI 점수 페이지 선택
            }

            GUILayout.FlexibleSpace(); // 우측 버튼을 끝으로 밀기

            if (GUILayout.Button("지금 갱신", GUILayout.Width(90f))) // 수동 새로고침 버튼
            {
                RefreshSnapshot(); // 현재 보드 상태 즉시 다시 계산
            }

            if (GUILayout.Button("Console 출력", GUILayout.Width(100f))) // 현재 점수를 Unity Console에도 남기는 버튼
            {
                DumpCurrentScoresToConsole(); // 현재 스냅샷 전체를 Debug.Log로 출력
            }

            if (GUILayout.Button("닫기", GUILayout.Width(70f))) // 마우스로도 창을 닫을 수 있는 버튼
            {
                _isOpen = false; // 창 닫기
            }

            GUILayout.EndHorizontal(); // 상단 메뉴 종료
            GUILayout.Space(6f); // 본문과 약간의 간격

            if (_currentPage == AiScorePage) // 현재 첫 번째 AI 점수 페이지라면
            {
                DrawAiScorePage(); // AI 점수 로그 본문 그리기
            }

            GUI.DragWindow(new Rect(0f, 0f, _windowRect.width, 24f)); // 제목 영역을 드래그해 창 위치를 이동할 수 있게 함
        }

        private void DrawAiScorePage() // 첫 번째 페이지의 AI 점수 로그를 그리는 메서드
        {
            string sceneName = SceneManager.GetActiveScene().name; // 현재 씬 이름 읽기
            string turnText = _battleController?.TurnManager != null ? _battleController.TurnManager.CurrentState.ToString() : "-"; // 현재 턴 상태를 안전하게 읽기

            GUILayout.Label($"Scene: {sceneName}    Turn: {turnText}    후보 수: {_snapshot.CandidateCount}"); // 현재 디버그 대상의 기본 상태 표시
            GUILayout.Label("표시값: Base = 33일차 공통 점수 / Role = 34일차 역할 보너스 / Final = Base + Role"); // 점수 열 의미 안내
            GUILayout.Label("정렬: 실제 선택 행동 → Final 높은 순"); // 로그 정렬 규칙 안내

            if (_snapshot.SelectedEntry != null) // 실제 선택 행동이 존재하면
            {
                GUILayout.Space(4f); // 선택 행동 강조 영역 전 간격
                GUILayout.Label($"[SELECT] {FormatEntry(_snapshot.SelectedEntry)}"); // 현재 AI가 선택할 행동을 별도로 한 줄 표시
            }
            else // 행동 후보가 없으면
            {
                GUILayout.Space(4f); // 안내 전 간격
                GUILayout.Label("[SELECT] 선택 가능한 AI 행동 없음"); // 기절·봉쇄·적 없음 상태 안내
            }

            GUILayout.Space(8f); // 전체 후보 로그와 간격
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUI.skin.box); // 후보가 많아도 볼 수 있는 스크롤 로그 영역 시작

            for (int i = 0; i < _snapshot.Entries.Count; i++) // 모든 점수 로그 항목 순회
            {
                var entry = _snapshot.Entries[i]; // 현재 로그 항목
                string prefix = entry.IsSelected ? "▶ " : "   "; // 실제 선택 행동에는 화살표 표시
                GUILayout.Label(prefix + FormatEntry(entry)); // 로그 한 줄 출력
            }

            GUILayout.EndScrollView(); // 스크롤 로그 영역 종료
        }

        private static string FormatEntry(AIDebugScoreEntry entry) // AI 점수 항목을 읽기 쉬운 한 줄 로그 문자열로 만드는 메서드
        {
            if (entry == null) return "-"; // 잘못된 항목이면 빈 표시 반환

            string actorName = entry.Actor?.Definition != null // 행동 주체 정의가 있으면
                ? entry.Actor.Definition.DisplayName // 실제 표시 이름 사용
                : "Unknown"; // 없으면 안전한 기본 이름 사용

            string roleName = entry.Role.ToString(); // 기본 AI 역할을 문자열로 변환
            string roleScore = entry.RoleBonus >= 0 ? $"+{entry.RoleBonus}" : entry.RoleBonus.ToString(); // 양수 역할 보너스에는 + 기호를 붙여 구분

            return $"[{roleName}] {actorName} {entry.Origin} -> {entry.Target} {entry.ActionType} | Base {entry.BaseScore} | Role {roleScore} | Final {entry.FinalScore}"; // 최종 로그 한 줄 반환
        }

        private void DumpCurrentScoresToConsole() // 현재 페이지의 AI 점수 로그를 Unity Console에 한 번에 출력하는 메서드
        {
            var builder = new StringBuilder(); // 여러 후보를 하나의 로그 문자열로 합칠 빌더 생성
            builder.AppendLine($"[AI DEBUG] 후보 수: {_snapshot.CandidateCount}"); // 첫 줄에 후보 수 출력

            for (int i = 0; i < _snapshot.Entries.Count; i++) // 현재 모든 로그 항목 순회
            {
                var entry = _snapshot.Entries[i]; // 현재 항목 참조
                builder.Append(entry.IsSelected ? "SELECT > " : "         "); // 선택 행동이면 별도 접두어 추가
                builder.AppendLine(FormatEntry(entry)); // 같은 화면 표시 형식으로 한 줄 추가
            }

            Debug.Log(builder.ToString()); // 완성된 전체 점수 로그를 Unity Console에 출력
        }
    }
}
