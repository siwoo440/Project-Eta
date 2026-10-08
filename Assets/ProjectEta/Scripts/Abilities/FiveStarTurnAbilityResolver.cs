using ProjectEta.Battle; // 턴 상태 참조
using ProjectEta.Run; // 회복 후 런 체력 동기화
using ProjectEta.Pieces; // 기물 상태 참조

namespace ProjectEta.Abilities // 5성 턴 능력 영역
{ // 범위 시작
    public static class FiveStarTurnAbilityResolver // 수호 갱신과 배치 회복 처리
    { // 범위 시작
        public static void ProcessTurnStart(TurnState state, int turnNumber, RunState runState = null) // 턴 진입 처리
        { // 범위 시작
            if (state != TurnState.PlayerTurn && state != TurnState.DeploymentTurn) // 관련 턴 여부 확인
            { // 범위 시작
                return; // 적 행동 중 중복 처리 제외
            } // 범위 종료
            if (state == TurnState.DeploymentTurn && turnNumber == 1) // 초기 자유 배치 이벤트 확인
            { // 초기 배치 예외 범위
                return; // 첫 일반 턴까지 황제 회복 대기
            } // 초기 배치 예외 범위 종료
            foreach (var board in AbilityBoardRegistry.SnapshotBoards()) // 활성 보드 순회
            { // 범위 시작
                if (runState != null && board != runState.Board) // 다른 런의 보드 확인
                { // 다른 보드 제외 범위
                    continue; // 현재 전투 보드만 처리
                } // 다른 보드 제외 범위 종료
                var pieces = AbilityBoardRegistry.GetUniquePieces(board); // 중복 없는 기물 조회
                foreach (var piece in pieces) // 생존 기물 순회
                { // 범위 시작
                    if (piece.IsDead) // 사망 여부 확인
                    { // 범위 시작
                        continue; // 사망 기물 제외
                    } // 범위 종료
                    if (state == TurnState.PlayerTurn && piece.LastPaladinResetTurn != turnNumber) // 같은 턴 중복 초기화 방지
                    { // 범위 시작
                        piece.PaladinGuardSpent = false; // 새 일반 턴 보호 재충전
                        piece.LastPaladinResetTurn = turnNumber; // 초기화 턴 기록
                    } // 범위 종료
                    if (state != TurnState.DeploymentTurn && turnNumber != 1) // 배치 진입과 첫 일반 턴만 회복 허용
                    { // 범위 시작
                        continue; // 일반 턴 회복 제외
                    } // 범위 종료
                    var ability = FiveStarCombatAbilityResolver.FindAbility(piece, FiveStarAbilityIds.EmperorHeal); // 황제 회복 능력 조회
                    if (ability == null || piece.LastEmperorDeploymentTurn == turnNumber) // 중복 발동 확인
                    { // 범위 시작
                        continue; // 이미 회복한 황제 제외
                    } // 범위 종료
                    PieceRuntimeState best = null; // 회복 대상 후보
                    foreach (var target in pieces) // 인접 아군 순회
                    { // 범위 시작
                        if (target == piece || target.IsDead || target.IsPlayerPiece != piece.IsPlayerPiece || target.CurrentHp >= target.Definition.BaseHp) // 부상 아군 조건 확인
                        { // 범위 시작
                            continue; // 회복 불가 기물 제외
                        } // 범위 종료
                        if (FiveStarCombatAbilityResolver.Distance(piece.BoardPosition, target.BoardPosition) > 1) // 인접 8칸 확인
                        { // 범위 시작
                            continue; // 범위 밖 제외
                        } // 범위 종료
                        if (best == null || target.CurrentHp < best.CurrentHp) // 가장 낮은 체력 우선
                        { // 범위 시작
                            best = target; // 회복 대상 갱신
                        } // 범위 종료
                    } // 범위 종료
                    piece.LastEmperorDeploymentTurn = turnNumber; // 이번 배치 발동 기록
                    if (best != null) // 부상 아군 존재 확인
                    { // 범위 시작
                        new HealAbilityExecutor().Execute(ability.Effects[0], new AbilityExecutionContext(piece, best, board: board, runState: runState)); // 공통 회복 처리
                        if (runState != null && best.IsPlayerPiece && best.Definition.MovementType == PieceMovementType.King) // 왕 회복 확인
                        { // 왕 체력 동기화 범위
                            runState.KingHp = best.CurrentHp; // HUD와 이후 스테이지 체력 갱신
                        } // 왕 체력 동기화 범위 종료
                    } // 범위 종료
                } // 범위 종료
            } // 범위 종료
        } // 범위 종료
    } // 범위 종료
} // 범위 종료
