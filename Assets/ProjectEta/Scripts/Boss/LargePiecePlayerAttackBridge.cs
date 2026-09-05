using System.Collections; // IEnumerator를 사용하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour, Camera, Physics, RaycastHit 등을 사용하기 위한 네임스페이스
using UnityEngine.InputSystem; // 새 Input System의 Mouse를 사용하기 위한 네임스페이스
using UnityEngine.SceneManagement; // Battle 씬에서만 자동 생성하기 위한 네임스페이스
using ProjectEta.Board; // BoardInputController를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceView와 PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.Boss // 대형 보스 관련 런타임 호환 브리지를 모아두는 네임스페이스
{
    [DefaultExecutionOrder(-500)] // 기존 BoardInputController의 기본 Update보다 먼저 클릭을 보정하도록 실행 순서를 앞당김
    public sealed class LargePiecePlayerAttackBridge : MonoBehaviour // 2x2 보스 모델 클릭을 기존 플레이어 공격 흐름으로 안전하게 연결하는 컴포넌트
    {
        private BoardInputController _boardInput; // 현재 선택 기물과 공격 진입점을 가진 기존 입력 컨트롤러
        private Camera _camera; // 보스 모델 클릭 판정을 위한 현재 메인 카메라
        private bool _isReady; // 실제 Battle 입력 시스템과 연결됐는지 여부

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Battle 씬 로드 직후 자동 실행
        private static void AutoCreateForBattleScene() // 인스펙터 설정 없이 보스 피격 입력 브리지를 자동 생성하는 부트스트랩
        {
            if (SceneManager.GetActiveScene().name != "Battle") return; // Battle 씬이 아니면 생성하지 않음
            if (Object.FindFirstObjectByType<LargePiecePlayerAttackBridge>() != null) return; // 이미 존재하면 중복 생성 금지

            var bridgeObject = new GameObject("LargePiecePlayerAttackBridge_Day39"); // 보스 클릭 보정 전용 오브젝트 생성
            bridgeObject.AddComponent<LargePiecePlayerAttackBridge>(); // 실제 입력 브리지 컴포넌트 추가
        }

        private IEnumerator Start() // BattleController/BoardInputController 자동 생성 순서와 무관하게 안전하게 연결하는 코루틴
        {
            const int maxWaitFrames = 180; // 약 3초 동안 기존 입력 시스템 준비를 기다릴 최대 프레임

            for (int i = 0; i < maxWaitFrames; i++) // 프레임 단위로 입력 객체 탐색
            {
                _boardInput = Object.FindFirstObjectByType<BoardInputController>(); // 현재 Battle 씬의 기존 보드 입력 컨트롤러 찾기
                _camera = Camera.main; // 현재 메인 카메라 참조 갱신

                if (_boardInput != null && _boardInput.IsBound && _camera != null) // 입력 시스템과 카메라가 모두 준비됐으면
                {
                    _isReady = true; // 클릭 보정 활성화
                    yield break; // 초기화 완료
                }

                yield return null; // 아직 준비 전이면 다음 프레임까지 대기
            }

            Debug.LogError("39일차 보스 피격 브리지 초기화 실패: BoardInputController 또는 Main Camera를 찾지 못했습니다."); // 대기 한도 초과 원인 출력
        }

        private void Update() // 왼쪽 클릭 순간 2x2 적 모델을 먼저 확인해 기존 공격 API로 변환하는 메서드
        {
            if (!_isReady || _boardInput == null || _camera == null) return; // 아직 연결되지 않았으면 처리하지 않음
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return; // 이번 프레임 왼쪽 클릭이 아니면 종료
            if (!_boardInput.CanUseCombatInput) return; // 일반 플레이어 행동 턴이 아니면 보스 공격 보정 금지
            if (_boardInput.SelectedPiece == null || _boardInput.PendingMovement == null) return; // 아군 기물을 먼저 선택한 상태가 아니면 기존 입력에 맡김
            if (_boardInput.RunState?.Board == null) return; // 실제 보드 상태가 없으면 처리 불가

            Vector2 screenPosition = Mouse.current.position.ReadValue(); // 현재 마우스 화면 좌표 읽기
            if (float.IsNaN(screenPosition.x) || float.IsNaN(screenPosition.y)) return; // 유효하지 않은 입력 좌표는 무시

            Ray ray = _camera.ScreenPointToRay(screenPosition); // 화면 클릭 지점에서 월드 광선 생성
            RaycastHit[] hits = Physics.RaycastAll(ray); // 보스 모델·보드가 겹쳐 있어도 모든 충돌 결과를 확보
            if (hits == null || hits.Length == 0) return; // 아무 것도 맞지 않으면 기존 입력에 맡김

            System.Array.Sort(hits, CompareHitDistance); // 카메라에 가까운 실제 클릭 대상부터 검사

            for (int i = 0; i < hits.Length; i++) // 광선에 맞은 모든 오브젝트를 가까운 순서로 순회
            {
                Collider collider = hits[i].collider; // 현재 충돌체 조회
                if (collider == null) continue; // 이미 파괴됐거나 잘못된 충돌체 제외

                PieceView pieceView = collider.GetComponentInParent<PieceView>(); // 보스 루트 또는 자식 파츠 충돌체에서 PieceView 탐색
                if (pieceView?.RuntimeState == null) continue; // 기물 뷰가 아니면 다음 hit 검사

                PieceRuntimeState targetPiece = pieceView.RuntimeState; // 클릭한 실제 런타임 기물 참조
                if (targetPiece.IsPlayerPiece || targetPiece.IsDead) return; // 내 기물·사망 기물 클릭은 기존 입력 로직에 맡김
                if (!LargePieceBoardUtility.IsLarge(targetPiece)) return; // 1x1 일반 적은 기존 클릭 처리 방식을 그대로 유지

                bool resolved = LargePiecePlayerAttackTargetResolver.TryResolveAttackCell( // 클릭 위치가 아닌 같은 보스의 실제 공격 가능 점유 칸을 탐색
                    _boardInput.RunState.Board, // 현재 전투 보드
                    targetPiece, // 클릭한 2x2 보스 런타임 상태
                    _boardInput.PendingMovement.AttackTiles, // 선택된 아군의 실제 공격 후보
                    out Vector2Int attackCell); // 기존 공격 API에 전달할 해결된 칸

                if (!resolved) return; // 이 보스가 현재 공격 범위 밖이면 공격을 강제로 만들지 않고 종료

                bool attacked = _boardInput.TryAttackSelectedPieceTarget(attackCell); // 기존 CombatResolver·BattleHooks·턴 종료 경로 그대로 사용
                if (attacked) // 실제 공격 진입이 성공했다면
                {
                    Debug.Log($"대형 보스 클릭 공격 보정: {targetPiece.Definition?.DisplayName} / 실제 공격 칸 {attackCell} / 남은 HP {targetPiece.CurrentHp}"); // 피격 여부 확인용 로그 출력
                }

                return; // 한 번의 마우스 클릭으로 두 번 공격되지 않도록 즉시 종료
            }
        }

        private static int CompareHitDistance(RaycastHit a, RaycastHit b) // RaycastAll 결과를 카메라에서 가까운 순서로 정렬하는 비교 함수
        {
            return a.distance.CompareTo(b.distance); // 작은 거리의 충돌 결과가 먼저 오도록 비교
        }
    }
}
