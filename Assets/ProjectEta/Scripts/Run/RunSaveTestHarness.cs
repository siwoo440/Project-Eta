using UnityEngine; // MonoBehaviour, GUI 등을 사용하기 위한 네임스페이스
using UnityEngine.InputSystem; // 새 Input System(Keyboard)을 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDatabase, PieceDefinition, PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.Run // 런(플레이 세션) 관련 타입을 모아두는 네임스페이스
{
    public class RunSaveTestHarness : MonoBehaviour // Test 씬에서 저장/불러오기를 수동으로 확인하는 컴포넌트
    {
        [SerializeField] private PieceDatabase _pieceDatabase; // 불러오기 시 id 조회에 쓸 데이터베이스
        [SerializeField] private PieceDefinition _kingDefinition; // 테스트용 킹 데이터
        [SerializeField] private PieceDefinition _pawnDefinition; // 테스트용 폰 데이터

        private RunState _runState; // 현재 테스트 중인 런 상태
        private string _statusMessage = "대기 중"; // 화면에 표시할 상태 메시지

        private void Awake() // 씬 시작 시 자동 호출되는 초기화 메서드
        {
            CreateTestRun(); // 시작하자마자 테스트용 런 생성
        }

        private void Update() // 매 프레임 자동 호출되는 메서드
        {
            if (Keyboard.current == null) // 키보드가 없으면
            {
                return; // 입력을 처리할 수 없으므로 종료
            }

            if (Keyboard.current.rKey.wasPressedThisFrame) // 이번 프레임에 R 키를 눌렀으면
            {
                CreateTestRun(); // 테스트 런 새로 생성
            }

            if (Keyboard.current.sKey.wasPressedThisFrame) // 이번 프레임에 S 키를 눌렀으면
            {
                SaveRun(); // 현재 런 저장
            }

            if (Keyboard.current.lKey.wasPressedThisFrame) // 이번 프레임에 L 키를 눌렀으면
            {
                LoadRun(); // 저장된 런 불러오기
            }
        }

        private void CreateTestRun() // 검증용 샘플 런 상태를 만드는 메서드
        {
            _runState = new RunState(startingKingHp: 2) // 킹 체력 2로 새 런 생성
            {
                CurrentRound = 3, // 테스트용 라운드 값 지정
                MetaCurrency = 10 // 테스트용 메타 재화 값 지정
            };
            _runState.Hand.TryAddCard(_pawnDefinition); // 손패에 폰 카드 추가
            _runState.Deck.AddToOwnedPool(_kingDefinition); // 보유 카드 풀에 킹 카드 추가

            var kingPosition = new Vector2Int(4, 1); // 테스트용 킹 배치 좌표
            var kingPiece = new PieceRuntimeState(_kingDefinition, kingPosition, isPlayerPiece: true) { CurrentHp = 2 }; // 테스트용 킹 런타임 상태 생성
            _runState.Board.GetTile(kingPosition).OccupyingPiece = kingPiece; // 보드에 킹 배치

            _statusMessage = "테스트 런 생성 완료 (킹 HP2, 라운드3, 손패 폰 1장, 보드에 킹 1기)"; // 상태 메시지 갱신
        }

        private void SaveRun() // 현재 런 상태를 파일로 저장하는 메서드
        {
            RunSaveSystem.Save(_runState); // 저장 시스템에 위임해 저장
            _statusMessage = "저장 완료"; // 상태 메시지 갱신
        }

        private void LoadRun() // 저장된 런 상태를 불러오는 메서드
        {
            if (!RunSaveSystem.TryLoad(_pieceDatabase, out var loadedRunState)) // 불러오기를 시도해 실패하면
            {
                _statusMessage = "저장 파일이 없습니다. 먼저 S로 저장하세요."; // 실패 메시지로 갱신
                return; // 종료
            }

            _runState = loadedRunState; // 불러온 런 상태로 교체
            _statusMessage = $"불러오기 완료 - 킹 HP:{_runState.KingHp}, 라운드:{_runState.CurrentRound}, 손패:{_runState.Hand.Hand.Count}장, 보드 기물:{CountBoardPieces()}개"; // 결과 요약 메시지로 갱신
        }

        private int CountBoardPieces() // 보드에 배치된 기물 수를 세는 메서드
        {
            int count = 0; // 카운트 초기값
            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 방향으로 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 방향으로 순회
                {
                    if (_runState.Board.GetTile(new Vector2Int(x, y)).OccupyingPiece != null) // 해당 칸에 기물이 있으면
                    {
                        count++; // 카운트 증가
                    }
                }
            }

            return count; // 최종 카운트 반환
        }

        private void OnGUI() // 화면에 조작 안내와 상태를 그리는 메서드
        {
            GUI.Label(new Rect(10, 10, 500, 20), "[R] 테스트 런 생성   [S] 저장   [L] 불러오기"); // 조작 안내 라벨 표시
            GUI.Label(new Rect(10, 30, 500, 20), _statusMessage); // 현재 상태 메시지 표시
        }
    }
}
