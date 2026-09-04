using System.Collections; // IEnumerator 코루틴을 사용하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour, Resources, GameObject, Debug 등을 사용하기 위한 네임스페이스
using UnityEngine.SceneManagement; // Battle 씬 여부 확인에 사용하는 네임스페이스
using ProjectEta.Board; // BoardInputController와 BoardView를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition과 PieceRuntimeState를 사용하기 위한 네임스페이스
using ProjectEta.Round; // 36일차 시작 적 배치가 끝난 뒤 프로토타입 보스를 추가하기 위한 네임스페이스

namespace ProjectEta.Boss // 37일차 이후 대형 보스 기물 기반을 모아두는 네임스페이스
{
    public sealed class PrototypeBoss37Spawner : MonoBehaviour // 37일차 2x2 점유를 Game View에서 바로 확인할 수 있게 프로토타입 보스를 자동 배치하는 개발용 컴포넌트
    {
        private static readonly Vector2Int PrototypeAnchor = new Vector2Int(0, 8); // 36일차 시작 적·증원과 겹치지 않는 적 진영 좌상단 2x2 기준 좌표

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Battle 씬 로드 직후 자동 실행
        private static void AutoCreateForBattleScene() // 인스펙터 설정 없이 프로토타입 보스 스포너를 만드는 부트스트랩
        {
            if (SceneManager.GetActiveScene().name != "Battle") return; // Battle 씬이 아니면 생성하지 않음
            if (Object.FindFirstObjectByType<PrototypeBoss37Spawner>() != null) return; // 이미 존재하면 중복 생성하지 않음

            var root = new GameObject("PrototypeBoss37Spawner"); // 개발용 보스 스포너 오브젝트 생성
            root.AddComponent<PrototypeBoss37Spawner>(); // Start 코루틴으로 기존 전투에 연결
        }

        private IEnumerator Start() // 36일차 라운드 초기 배치가 끝난 다음 2x2 보스를 추가하는 코루틴
        {
            const int maxWaitFrames = 240; // 약 4초 동안 기존 라운드·보드 초기화를 기다림

            for (int i = 0; i < maxWaitFrames; i++) // 프레임마다 필요한 전투 객체 상태 확인
            {
                var boardInput = Object.FindFirstObjectByType<BoardInputController>(); // 기존 적 스폰 진입점을 가진 보드 입력 컨트롤러 찾기
                var boardView = Object.FindFirstObjectByType<BoardView>(); // 대형 모델 시각 보정에 사용할 보드 뷰 찾기
                var roundController = Object.FindFirstObjectByType<RoundRuntimeController>(); // 36일차 라운드 초기화 상태 확인

                if (boardInput != null && boardInput.IsBound && boardView != null && boardView.IsBound && roundController != null && roundController.Definition != null) // 36일차 시작 적 구성이 끝난 상태면
                {
                    SpawnPrototypeBoss(boardInput, boardView); // 2x2 보스 한 기를 실제 기존 스폰 경로로 생성
                    yield break; // 한 번만 생성하고 종료
                }

                yield return null; // 아직 초기화 중이면 다음 프레임까지 대기
            }

            Debug.LogWarning("37일차 PrototypeBoss37Spawner: 라운드 초기화를 기다리는 동안 보스를 생성하지 못했습니다."); // 대기 실패 안내
        }

        private static void SpawnPrototypeBoss(BoardInputController boardInput, BoardView boardView) // 기존 SpawnTestEnemy를 사용해 보스를 생성한 뒤 2x2로 확장하는 메서드
        {
            var definition = Resources.Load<PieceDefinition>("PrototypeBoss37"); // 37일차 Resources 보스 정의 로드

            if (definition == null) // 보스 에셋이 누락됐으면
            {
                Debug.LogError("Resources/PrototypeBoss37 PieceDefinition을 찾지 못했습니다."); // 정확한 누락 리소스 이름 출력
                return; // 생성 중단
            }

            if (!LargePieceBoardUtility.CanPlace(boardView.State, definition, PrototypeAnchor)) // 2x2 네 칸 전체가 비어 있는지 기존 스폰 전에 먼저 확인
            {
                Debug.LogWarning($"37일차 2x2 보스 배치 실패: {PrototypeAnchor} 기준 {definition.OccupancySize} 영역이 비어 있지 않습니다."); // 기존 기물을 덮어쓰지 않음
                return; // 안전하게 생성 취소
            }

            PieceRuntimeState boss = boardInput.SpawnTestEnemy(definition, PrototypeAnchor); // 기존 BoardInputController 경로를 재사용해 PieceRuntimeState·PieceView·내부 뷰 맵까지 정상 등록

            if (boss == null) // 기존 스폰 경로에서 실패했으면
            {
                Debug.LogWarning("37일차 2x2 보스 기준 칸 스폰에 실패했습니다."); // 실패 원인 확인용 로그
                return; // 추가 점유를 시도하지 않음
            }

            if (!LargePieceBoardUtility.ExpandExistingAnchorOccupancy(boardView.State, boss)) // 기존 1x1 기준 칸을 같은 런타임 상태의 2x2 네 칸으로 확장
            {
                Debug.LogError("37일차 2x2 보스 점유 확장에 실패했습니다."); // 사전 검사와 다른 상태가 됐음을 알림
                return; // 시각 확대를 적용하지 않음
            }

            var pieceView = LargePieceVisualUtility.FindPieceView(boss); // 기존 SpawnTestEnemy가 등록한 실제 PieceView 찾기
            if (pieceView != null) LargePieceVisualUtility.ApplyFootprint(pieceView, boardView.TileSize); // 네 칸 중앙 배치·크기 확대·클릭 콜라이더 확장

            Debug.Log($"37일차 2x2 보스 생성: {definition.DisplayName} / Anchor={PrototypeAnchor} / Size={definition.OccupancySize} / HP={boss.CurrentHp}"); // 수동 확인용 결과 로그
        }
    }
}
