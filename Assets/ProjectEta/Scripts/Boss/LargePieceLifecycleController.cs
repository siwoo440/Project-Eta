using System.Collections; // IEnumerator 코루틴을 사용하기 위한 네임스페이스
using System.Collections.Generic; // Dictionary<T,T>와 HashSet<T>를 사용하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour, GameObject, Debug 등을 사용하기 위한 네임스페이스
using UnityEngine.SceneManagement; // 현재 씬이 Battle인지 확인하기 위한 네임스페이스
using ProjectEta.Battle; // BattleController와 BattleHooks를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState와 BoardView를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceRuntimeState와 PieceView를 사용하기 위한 네임스페이스

namespace ProjectEta.Boss // 37일차 이후 대형 보스 기물 기반을 모아두는 네임스페이스
{
    public sealed class LargePieceLifecycleController : MonoBehaviour // 기존 1x1 전투 코드와 2x2 점유를 연결하는 런타임 호환 브리지
    {
        private BattleController _battleController; // 현재 전투 컨트롤러
        private BattleHooks _battleHooks; // 이동·피해 이벤트를 받는 현재 전투 훅 버스
        private BoardView _boardView; // 대형 기물 화면 위치 보정에 사용할 보드 뷰
        private readonly Dictionary<PieceRuntimeState, Vector2Int> _moveOrigins = new Dictionary<PieceRuntimeState, Vector2Int>(); // 기존 이동 전 대형 기물 기준 좌표 저장
        private float _nextScanTime; // 외부 스폰 대형 기물을 자동 감지할 다음 시간

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Battle 씬 로드가 끝나면 자동 실행
        private static void AutoCreateForBattleScene() // 인스펙터 연결 없이 대형 기물 호환 브리지를 생성하는 부트스트랩
        {
            if (SceneManager.GetActiveScene().name != "Battle") return; // Battle 씬이 아니면 생성하지 않음
            if (Object.FindFirstObjectByType<LargePieceLifecycleController>() != null) return; // 이미 존재하면 중복 생성하지 않음

            var root = new GameObject("LargePieceLifecycleController_Day37"); // 대형 점유 생명주기 전용 오브젝트 생성
            root.AddComponent<LargePieceLifecycleController>(); // 코루틴으로 기존 전투 시스템에 연결
        }

        private IEnumerator Start() // BattleController 자동 생성 순서와 무관하게 연결될 때까지 기다리는 초기화 코루틴
        {
            const int maxWaitFrames = 180; // 약 3초 동안 핵심 전투 객체를 기다림

            for (int i = 0; i < maxWaitFrames; i++) // 프레임 단위로 전투 시스템 탐색
            {
                _battleController = Object.FindFirstObjectByType<BattleController>(); // 현재 BattleController 찾기
                _boardView = Object.FindFirstObjectByType<BoardView>(); // 현재 BoardView 찾기

                if (_battleController != null && _battleController.RunState != null && _battleController.BattleHooks != null && _boardView != null && _boardView.IsBound) // 필요한 상태가 모두 준비됐으면
                {
                    _battleHooks = _battleController.BattleHooks; // 훅 버스 저장
                    BindHooks(); // 이동·피해 이벤트 구독
                    ScanAndRepairLargePieces(); // 이미 생성된 대형 기물이 있다면 즉시 점유 확장
                    yield break; // 초기화 완료
                }

                yield return null; // 아직 준비 전이면 다음 프레임까지 대기
            }

            Debug.LogError("37일차 LargePieceLifecycleController 초기화 실패: BattleController/BoardView 연결을 찾지 못했습니다."); // 대기 한도 초과 원인 출력
        }

        private void Update() // SpawnTestEnemy 등 기존 경로로 새 대형 기물이 추가된 경우를 자동으로 감지하는 가벼운 검사
        {
            if (_battleController?.RunState == null || _boardView == null) return; // 아직 연결되지 않았으면 처리하지 않음
            if (Time.unscaledTime < _nextScanTime) return; // 짧은 간격마다 한 번만 검사

            _nextScanTime = Time.unscaledTime + 0.25f; // 다음 검사를 0.25초 뒤로 예약
            ScanAndRepairLargePieces(); // 보드의 대형 기물 점유와 시각 중심을 확인
        }

        private void BindHooks() // 기존 BattleHooks에 대형 점유 보정 이벤트를 중복 없이 연결하는 메서드
        {
            if (_battleHooks == null) return; // 훅 버스가 없으면 연결할 수 없음

            _battleHooks.BeforeMove -= HandleBeforeMove; // 재연결 시 중복 구독 방지
            _battleHooks.AfterMove -= HandleAfterMove; // 재연결 시 중복 구독 방지
            _battleHooks.AfterDamage -= HandleAfterDamage; // 재연결 시 중복 구독 방지

            _battleHooks.BeforeMove += HandleBeforeMove; // 대형 기물 이동 전 원점 저장
            _battleHooks.AfterMove += HandleAfterMove; // 기존 1x1 이동 뒤 2x2 전체 점유 복구
            _battleHooks.AfterDamage += HandleAfterDamage; // 대형 기물 사망 즉시 전체 점유 해제
        }

        private void HandleBeforeMove(PieceRuntimeState piece, Vector2Int origin, Vector2Int destination) // 기존 이동이 대형 기물을 건드리기 직전 호출
        {
            if (!LargePieceBoardUtility.IsLarge(piece)) return; // 일반 1x1 기물은 무시
            _moveOrigins[piece] = origin; // 이동 실패 시 롤백할 원래 기준점 저장
        }

        private void HandleAfterMove(PieceRuntimeState piece, Vector2Int origin, Vector2Int destination) // 기존 1x1 이동 처리가 끝난 뒤 2x2 점유를 다시 맞추는 메서드
        {
            if (!LargePieceBoardUtility.IsLarge(piece)) return; // 일반 기물은 기존 코드 그대로 유지
            if (_battleController?.RunState?.Board == null) return; // 실제 보드가 없으면 복구할 수 없음

            Vector2Int fallbackOrigin = _moveOrigins.TryGetValue(piece, out var savedOrigin) ? savedOrigin : origin; // 저장된 이동 전 기준점을 우선 사용
            bool repaired = LargePieceBoardUtility.RepairAfterExistingMove(_battleController.RunState.Board, piece, fallbackOrigin, destination); // 새 위치 전체 점유 또는 원위치 롤백
            _moveOrigins.Remove(piece); // 이번 이동 캐시 정리

            var view = LargePieceVisualUtility.FindPieceView(piece); // 실제 대형 기물 뷰 탐색
            if (view != null) LargePieceVisualUtility.ApplyFootprint(view, _boardView.TileSize); // 현재 기준점의 2x2 중앙으로 화면 위치·콜라이더 재보정

            if (!repaired) Debug.LogError($"{piece.Definition.DisplayName} 대형 점유 이동 복구 실패: {fallbackOrigin} -> {destination}"); // 두 위치 모두 복구 실패 시 원인 출력
        }

        private void HandleAfterDamage(PieceRuntimeState target, PieceRuntimeState source, int appliedAmount) // 피해가 적용된 직후 대형 기물 사망 점유를 정리하는 메서드
        {
            if (target == null || !target.IsDead || !LargePieceBoardUtility.IsLarge(target)) return; // 살아 있거나 일반 기물이면 별도 처리 없음
            if (_battleController?.RunState?.Board == null) return; // 실제 보드가 없으면 처리하지 않음

            int cleared = LargePieceBoardUtility.ClearAllOccupiedCells(_battleController.RunState.Board, target); // 네 칸 어디를 공격했든 같은 런타임 기물의 전체 점유를 해제
            Debug.Log($"대형 기물 사망 점유 해제: {target.Definition.DisplayName} / 해제 {cleared}칸"); // 개발 확인용 로그
        }

        private void ScanAndRepairLargePieces() // 현재 보드에서 대형 기물을 한 번씩 찾아 점유·시각 상태를 보정하는 메서드
        {
            BoardState board = _battleController?.RunState?.Board; // 실제 보드 참조
            if (board == null) return; // 보드가 없으면 검사 종료

            var visited = new HashSet<PieceRuntimeState>(); // 2x2 네 칸의 같은 기물을 중복 처리하지 않기 위한 집합

            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 전체 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 전체 순회
                {
                    var piece = board.GetTile(new Vector2Int(x, y))?.OccupyingPiece; // 현재 칸 점유 기물 조회
                    if (piece == null || piece.IsDead || !visited.Add(piece)) continue; // 빈 칸·사망·이미 처리한 기물 제외
                    if (!LargePieceBoardUtility.IsLarge(piece)) continue; // 일반 1x1 기물 제외

                    if (!LargePieceBoardUtility.IsFootprintComplete(board, piece)) // 기준점만 점유된 등 불완전한 상태라면
                    {
                        if (!LargePieceBoardUtility.ExpandExistingAnchorOccupancy(board, piece)) // 기존 기준점 스폰을 전체 점유로 확장 시도
                        {
                            Debug.LogWarning($"대형 기물 점유 확장 실패: {piece.Definition.DisplayName} @ {piece.BoardPosition}"); // 충돌 영역을 덮어쓰지 않고 경고만 출력
                            continue; // 해당 기물 시각 보정도 현재 기준점 상태로 유지
                        }
                    }

                    var view = LargePieceVisualUtility.FindPieceView(piece); // 해당 대형 기물 화면 뷰 탐색
                    if (view != null) LargePieceVisualUtility.ApplyFootprint(view, _boardView.TileSize); // 점유 영역 중앙·확대·클릭 콜라이더 적용
                }
            }
        }

        private void OnDestroy() // 씬 종료 시 BattleHooks 이벤트 구독을 정리하는 메서드
        {
            if (_battleHooks == null) return; // 연결된 훅이 없으면 정리할 내용 없음
            _battleHooks.BeforeMove -= HandleBeforeMove; // 이동 전 이벤트 구독 해제
            _battleHooks.AfterMove -= HandleAfterMove; // 이동 후 이벤트 구독 해제
            _battleHooks.AfterDamage -= HandleAfterDamage; // 피해 이벤트 구독 해제
        }
    }
}
