using System; // 사망 처리 콜백
using System.Collections; // 연결 대기
using System.Collections.Generic; // 고유 기물 집합
using UnityEngine; // 런타임 컴포넌트
using ProjectEta.Battle; // 전투 훅
using ProjectEta.Board; // 보드 입력과 상태
using ProjectEta.Pieces; // 기물 상태 효과

namespace ProjectEta.Boss // 기존 대형 기물 호환 영역
{ // 범위 시작
    public sealed class LargePieceTurnEndStatusBridge : MonoBehaviour // 기존 씬 연결 호환 컴포넌트
    { // 범위 시작
        private BoardInputController _boardInput; // 상태 정산 소유자
        private bool _isBound; // 연결 준비 여부

        private IEnumerator Start() // 기존 부트스트랩 연결 확인
        { // 범위 시작
            const int maxWaitFrames = 180; // 최대 연결 대기
            for (int i = 0; i < maxWaitFrames; i++) // 준비 상태 반복 확인
            { // 범위 시작
                _boardInput = UnityEngine.Object.FindFirstObjectByType<BoardInputController>(); // 보드 입력 탐색
                if (TryBind()) // 정산 소유자 준비 확인
                { // 범위 시작
                    yield break; // 연결 확인 종료
                } // 범위 종료
                yield return null; // 다음 프레임 대기
            } // 범위 종료
            Debug.LogWarning("상태 효과 연결 확인 실패: BoardInputController 연결 누락"); // 연결 누락 기록
        } // 범위 종료

        private bool TryBind() // 기존 초기화 호출 호환
        { // 범위 시작
            _isBound = _boardInput != null && _boardInput.IsBound && _boardInput.BattleHooks != null; // 현재 입력 준비 확인
            return _isBound; // BoardInputController의 단일 정산 구독 유지
        } // 범위 종료

        public static int ProcessUniquePieces(BoardState board, BattleHooks battleHooks, Action<PieceRuntimeState> onDeadPiece = null) // 테스트 가능한 고유 기물 턴 종료 정산
        { // 범위 시작
            if (board == null) return 0; // 보드 누락 시 처리 없음

            var processedPieces = new HashSet<PieceRuntimeState>(); // 2x2 네 칸의 동일 런타임 중복 방지 집합
            int processedCount = 0; // 실제 정산 기물 수

            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 순회
            { // 범위 시작
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 순회
                { // 범위 시작
                    var piece = board.GetTile(new Vector2Int(x, y))?.OccupyingPiece; // 현재 점유 기물 조회
                    if (piece == null || !processedPieces.Add(piece)) continue; // 빈 칸·이미 정산한 대형 기물 제외

                    int damage = StatusEffectTickResolver.ResolveTurnEndDamage(piece, battleHooks); // 독·화상 턴 종료 피해를 한 번만 정산

                    if (damage > 0) // 실제 상태 피해가 있으면
                    { // 범위 시작
                        Debug.Log($"{piece.Definition.DisplayName} 상태 이상 피해 {damage}, 남은 HP {piece.CurrentHp}"); // 상태 피해 결과 로그
                    } // 범위 종료

                    piece.TickStatusEffects(); // 지속 턴 감소와 기절·속박 해제를 한 번만 처리
                    processedCount++; // 고유 기물 처리 수 증가

                    if (piece.IsDead) // 상태 피해로 사망했으면
                    { // 범위 시작
                        onDeadPiece?.Invoke(piece); // 기존 사망·화면·덱 정리 경로 호출
                    } // 범위 종료
                } // 범위 종료
            } // 범위 종료

            return processedCount; // 실제 정산한 고유 기물 수 반환
        } // 범위 종료

    } // 범위 종료
} // 범위 종료
