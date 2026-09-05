using System; // Action과 Delegate를 사용하기 위한 네임스페이스
using System.Collections; // IEnumerator 코루틴을 사용하기 위한 네임스페이스
using System.Collections.Generic; // HashSet<T>를 사용하기 위한 네임스페이스
using System.Reflection; // 기존 BoardInputController private 상태 정산 경로와 호환하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour, Debug, GameObject, Vector2Int를 사용하기 위한 네임스페이스
using UnityEngine.SceneManagement; // Battle 씬 여부 확인에 사용하는 네임스페이스
using ProjectEta.Battle; // BattleHooks, TurnState를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardInputController, BoardState를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceRuntimeState와 상태 효과 정산기를 사용하기 위한 네임스페이스

namespace ProjectEta.Boss // 대형 기물 통합 호환 타입을 모아두는 네임스페이스
{
    public sealed class LargePieceTurnEndStatusBridge : MonoBehaviour // 2x2 보스 상태 효과가 점유 4칸 때문에 네 번 정산되지 않도록 기존 턴 종료 경로를 교체하는 브리지
    {
        private BoardInputController _boardInput; // 기존 상태 효과·사망 처리 소유자
        private BattleHooks _battleHooks; // 턴 종료와 피해 훅 버스
        private Action<TurnState, int> _legacyTurnEndHandler; // 기존 BoardInputController 턴 종료 핸들러 델리게이트
        private MethodInfo _removePieceMethod; // 기존 사망 처리 private 메서드
        private bool _isBound; // 교체 연결 완료 여부

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Battle 씬 로드 직후 자동 실행
        private static void AutoCreateForBattleScene() // Inspector 설정 없이 호환 브리지 생성
        {
            if (SceneManager.GetActiveScene().name != "Battle") return; // Battle 씬 외 생성 방지
            if (UnityEngine.Object.FindFirstObjectByType<LargePieceTurnEndStatusBridge>() != null) return; // 중복 생성 방지

            var root = new GameObject("LargePieceTurnEndStatusBridge_Day40"); // 상태 효과 통합 브리지 오브젝트 생성
            root.AddComponent<LargePieceTurnEndStatusBridge>(); // Start 코루틴 연결
        }

        private IEnumerator Start() // BoardInputController의 BattleHooks 구독이 끝날 때까지 대기
        {
            const int maxWaitFrames = 180; // 최대 대기 프레임

            for (int i = 0; i < maxWaitFrames; i++) // 런타임 연결 상태 반복 확인
            {
                _boardInput = UnityEngine.Object.FindFirstObjectByType<BoardInputController>(); // BoardInputController 탐색

                if (_boardInput != null && _boardInput.IsBound && _boardInput.BattleHooks != null) // 기존 상태 효과 경로가 준비됐으면
                {
                    if (TryBind()) // 기존 반복 정산 핸들러를 안전하게 교체했으면
                    {
                        yield break; // 초기화 종료
                    }

                    yield break; // reflection 구조가 달라졌으면 기존 경로를 유지하고 종료
                }

                yield return null; // 아직 준비 전이면 다음 프레임 대기
            }

            Debug.LogWarning("40일차 LargePieceTurnEndStatusBridge 초기화 실패: BoardInputController/BattleHooks 연결을 찾지 못했습니다."); // 대기 실패 로그
        }

        private bool TryBind() // 기존 BoardInputController 턴 종료 핸들러를 고유 기물 기준 핸들러로 교체
        {
            if (_isBound || _boardInput == null) return _isBound; // 중복 연결 방지

            _battleHooks = _boardInput.BattleHooks; // 현재 전투 훅 저장
            var turnEndMethod = typeof(BoardInputController).GetMethod("HandleBattleHooksTurnEnd", BindingFlags.Instance | BindingFlags.NonPublic); // 기존 private 턴 종료 핸들러 탐색
            _removePieceMethod = typeof(BoardInputController).GetMethod("RemovePieceFromBoard", BindingFlags.Instance | BindingFlags.NonPublic); // 기존 private 사망 처리 메서드 탐색

            if (_battleHooks == null || turnEndMethod == null || _removePieceMethod == null) // 기존 구조가 예상과 다르면
            {
                Debug.LogWarning("40일차 상태 효과 중복 방지 연결 실패: BoardInputController 내부 메서드 구조가 변경됐습니다. 기존 정산 경로를 유지합니다."); // 안전 fallback 로그
                return false; // 기존 구독을 건드리지 않음
            }

            try // private 메서드 델리게이트 생성 보호
            {
                _legacyTurnEndHandler = (Action<TurnState, int>)Delegate.CreateDelegate(typeof(Action<TurnState, int>), _boardInput, turnEndMethod); // 기존 핸들러와 동일한 델리게이트 생성
            }
            catch (Exception exception) // reflection 델리게이트 생성 실패
            {
                Debug.LogWarning($"40일차 상태 효과 중복 방지 델리게이트 생성 실패: {exception.Message}"); // 실패 사유 로그
                return false; // 기존 경로 유지
            }

            _battleHooks.TurnEnd -= _legacyTurnEndHandler; // 타일 단위 기존 상태 효과 정산 구독 제거
            _battleHooks.TurnEnd -= HandleTurnEnd; // 중복 자체 구독 제거
            _battleHooks.TurnEnd += HandleTurnEnd; // 런타임 기물 단위 상태 효과 정산 구독
            _isBound = true; // 연결 완료 기록

            Debug.Log("40일차 상태 효과 통합: TurnEnd 정산을 점유 칸 기준에서 고유 PieceRuntimeState 기준으로 교체했습니다."); // 적용 로그
            return true; // 연결 성공
        }

        private void HandleTurnEnd(TurnState state, int turnNumber) // 한 턴 종료 시 각 런타임 기물을 정확히 한 번씩 정산
        {
            if (_boardInput?.RunState?.Board == null || _battleHooks == null) return; // 필수 상태 검사

            ProcessUniquePieces(_boardInput.RunState.Board, _battleHooks, HandleDeadPiece); // 고유 기물 기준 상태 효과 정산 실행
        }

        public static int ProcessUniquePieces(BoardState board, BattleHooks battleHooks, Action<PieceRuntimeState> onDeadPiece = null) // 테스트 가능한 고유 기물 턴 종료 정산
        {
            if (board == null) return 0; // 보드 누락 시 처리 없음

            var processedPieces = new HashSet<PieceRuntimeState>(); // 2x2 네 칸의 동일 런타임 중복 방지 집합
            int processedCount = 0; // 실제 정산 기물 수

            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 순회
                {
                    var piece = board.GetTile(new Vector2Int(x, y))?.OccupyingPiece; // 현재 점유 기물 조회
                    if (piece == null || !processedPieces.Add(piece)) continue; // 빈 칸·이미 정산한 대형 기물 제외

                    int damage = StatusEffectTickResolver.ResolveTurnEndDamage(piece, battleHooks); // 독·화상 턴 종료 피해를 한 번만 정산

                    if (damage > 0) // 실제 상태 피해가 있으면
                    {
                        Debug.Log($"{piece.Definition.DisplayName} 상태 이상 피해 {damage}, 남은 HP {piece.CurrentHp}"); // 상태 피해 결과 로그
                    }

                    piece.TickStatusEffects(); // 지속 턴 감소와 기절·속박 해제를 한 번만 처리
                    processedCount++; // 고유 기물 처리 수 증가

                    if (piece.IsDead) // 상태 피해로 사망했으면
                    {
                        onDeadPiece?.Invoke(piece); // 기존 사망·화면·덱 정리 경로 호출
                    }
                }
            }

            return processedCount; // 실제 정산한 고유 기물 수 반환
        }

        private void HandleDeadPiece(PieceRuntimeState piece) // 기존 BoardInputController 사망 처리 파이프라인을 재사용
        {
            if (piece == null || _boardInput == null || _removePieceMethod == null) return; // 필수 데이터 검사

            try // 기존 private 사망 처리 호출 보호
            {
                _removePieceMethod.Invoke(_boardInput, new object[] { piece }); // 보드·PieceView·DeadPile 정리 기존 경로 재사용
            }
            catch (Exception exception) // reflection 호출 실패
            {
                _boardInput.RunState?.Board?.ClearPiece(piece); // 최소한 보드 점유는 전체 해제
                Debug.LogError($"40일차 상태 이상 사망 처리 fallback: {piece.Definition?.DisplayName} / {exception.Message}"); // 실패 추적 로그
            }
        }

        private void OnDestroy() // 브리지 파괴 시 이벤트 구독 복원
        {
            if (_battleHooks == null || !_isBound) return; // 연결되지 않았으면 정리 없음

            _battleHooks.TurnEnd -= HandleTurnEnd; // 자체 정산 구독 제거

            if (_legacyTurnEndHandler != null && _boardInput != null) // 기존 BoardInputController가 아직 살아 있으면
            {
                _battleHooks.TurnEnd -= _legacyTurnEndHandler; // 중복 가능성 제거
                _battleHooks.TurnEnd += _legacyTurnEndHandler; // 기존 정산 경로 복원
            }

            _isBound = false; // 연결 상태 초기화
        }
    }
}
