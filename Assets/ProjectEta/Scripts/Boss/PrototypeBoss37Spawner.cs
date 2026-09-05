using System.Collections; // IEnumerator 코루틴을 사용하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour, Resources, GameObject, Debug 등을 사용하기 위한 네임스페이스
using UnityEngine.SceneManagement; // Battle 씬 여부 확인에 사용하는 네임스페이스
using ProjectEta.Board; // BoardInputController와 BoardView를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition과 PieceRuntimeState를 사용하기 위한 네임스페이스
using ProjectEta.Round; // RoundRuntimeController의 보스 라운드 여부를 확인하기 위한 네임스페이스

namespace ProjectEta.Boss // 대형 보스 기물 기반을 모아두는 네임스페이스
{
    public sealed class PrototypeBoss37Spawner : MonoBehaviour // 일반 개발 전투에서만 37일차 프로토타입 보스를 자동 배치하는 호환 컴포넌트
    {
        private static readonly Vector2Int PrototypeAnchor = new Vector2Int(0, 8); // 기존 프로토타입 보스 기준 좌표

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Battle 씬 로드 직후 자동 실행
        private static void AutoCreateForBattleScene() // 인스펙터 설정 없이 프로토타입 보스 스포너 생성
        {
            if (SceneManager.GetActiveScene().name != "Battle") return; // Battle 씬 외 생성 방지
            if (Object.FindFirstObjectByType<PrototypeBoss37Spawner>() != null) return; // 중복 생성 방지

            var root = new GameObject("PrototypeBoss37Spawner"); // 호환용 보스 스포너 오브젝트 생성
            root.AddComponent<PrototypeBoss37Spawner>(); // Start 코루틴 연결
        }

        private IEnumerator Start() // 라운드 초기화가 끝난 뒤 일반 테스트 라운드에만 기존 보스를 추가
        {
            const int maxWaitFrames = 240; // 최대 초기화 대기 프레임

            for (int i = 0; i < maxWaitFrames; i++) // 필요한 런타임 객체 대기
            {
                var boardInput = Object.FindFirstObjectByType<BoardInputController>(); // 적 스폰 진입점 탐색
                var boardView = Object.FindFirstObjectByType<BoardView>(); // 대형 모델 시각 보정용 보드 뷰 탐색
                var roundController = Object.FindFirstObjectByType<RoundRuntimeController>(); // 현재 라운드 관리자 탐색

                if (boardInput != null &&
                    boardInput.IsBound &&
                    boardView != null &&
                    boardView.IsBound &&
                    roundController != null &&
                    roundController.Definition != null) // 라운드·보드 초기화 완료 여부
                {
                    if (roundController.Definition.IsBossRound) // RoundDefinition이 보스를 직접 관리하는 5·10라운드면
                    {
                        Debug.Log("40일차 보스 통합: 보스 라운드는 RoundRuntimeController가 생성하므로 PrototypeBoss37Spawner 자동 생성을 건너뜁니다."); // 중복 스폰 방지 로그
                        yield break; // 레거시 자동 스폰 종료
                    }

                    SpawnPrototypeBoss(boardInput, boardView); // 일반 테스트 라운드에서는 기존 개발용 보스 유지
                    yield break; // 한 번만 생성
                }

                yield return null; // 아직 초기화 중이면 다음 프레임 대기
            }

            Debug.LogWarning("PrototypeBoss37Spawner: 라운드 초기화를 기다리는 동안 보스를 생성하지 못했습니다."); // 대기 실패 로그
        }

        private static void SpawnPrototypeBoss(BoardInputController boardInput, BoardView boardView) // 기존 SpawnTestEnemy를 사용한 개발용 보스 생성
        {
            var definition = Resources.Load<PieceDefinition>("PrototypeBoss37"); // 프로토타입 보스 정의 로드

            if (definition == null) // 보스 에셋 누락 검사
            {
                Debug.LogError("Resources/PrototypeBoss37 PieceDefinition을 찾지 못했습니다."); // 리소스 누락 로그
                return; // 생성 중단
            }

            PieceRuntimeState existingBoss = FindExistingBoss(boardView.State, definition.PieceId); // RoundRuntimeController 또는 저장 복원이 만든 동일 보스 탐색

            if (existingBoss != null) // 이미 같은 보스가 있으면
            {
                Debug.Log("PrototypeBoss37Spawner: 동일 보스가 이미 존재해 중복 생성을 건너뜁니다."); // 중복 방지 로그
                return; // 생성 종료
            }

            if (!LargePieceBoardUtility.CanPlace(boardView.State, definition, PrototypeAnchor)) // 2x2 전체 배치 가능 여부 검사
            {
                Debug.LogWarning($"2x2 보스 배치 실패: {PrototypeAnchor} 기준 {definition.OccupancySize} 영역이 비어 있지 않습니다."); // 점유 충돌 로그
                return; // 생성 취소
            }

            PieceRuntimeState boss = boardInput.SpawnTestEnemy(definition, PrototypeAnchor); // 기존 스폰 경로로 보스 기준 칸 생성

            if (boss == null) // 기존 스폰 실패 검사
            {
                Debug.LogWarning("2x2 보스 기준 칸 스폰에 실패했습니다."); // 스폰 실패 로그
                return; // 점유 확장 중단
            }

            if (!LargePieceBoardUtility.ExpandExistingAnchorOccupancy(boardView.State, boss)) // 기준 칸을 2x2 전체 점유로 확장
            {
                boardView.State.ClearPiece(boss); // 부분 점유 잔여 상태 제거
                Debug.LogError("2x2 보스 점유 확장에 실패했습니다."); // 점유 실패 로그
                return; // 시각 확대 중단
            }

            var pieceView = LargePieceVisualUtility.FindPieceView(boss); // 실제 PieceView 탐색
            if (pieceView != null) LargePieceVisualUtility.ApplyFootprint(pieceView, boardView.TileSize); // 점유 중앙·콜라이더 적용

            Debug.Log($"2x2 보스 생성: {definition.DisplayName} / Anchor={PrototypeAnchor} / Size={definition.OccupancySize} / HP={boss.CurrentHp}"); // 생성 결과 로그
        }

        private static PieceRuntimeState FindExistingBoss(BoardState board, string pieceId) // 동일 PieceId 대형 보스 존재 여부 탐색
        {
            if (board == null || string.IsNullOrWhiteSpace(pieceId)) return null; // 필수 데이터 검사

            var visited = new System.Collections.Generic.HashSet<PieceRuntimeState>(); // 2x2 중복 순회 방지

            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 순회
                {
                    var piece = board.GetTile(new Vector2Int(x, y))?.OccupyingPiece; // 현재 기물 조회
                    if (piece == null || !visited.Add(piece)) continue; // 빈 칸·중복 제외
                    if (piece.IsPlayerPiece || piece.IsDead || piece.Definition == null) continue; // 아군·사망·정의 누락 제외
                    if (string.Equals(piece.Definition.PieceId, pieceId, System.StringComparison.OrdinalIgnoreCase)) return piece; // 동일 보스 반환
                }
            }

            return null; // 동일 보스 없음
        }
    }
}
