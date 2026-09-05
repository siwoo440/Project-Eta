using System; // StringComparison을 사용하기 위한 네임스페이스
using System.Collections.Generic; // HashSet<T>와 IReadOnlyList<T>를 사용하기 위한 네임스페이스
using UnityEngine; // Debug, Object, Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Battle; // CombatResolver, CombatResult, TurnManager, BattleHooks를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState와 BoardView를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceRuntimeState와 PieceView를 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스

namespace ProjectEta.Boss // 2x2 이상 보스 전투 관련 타입을 모아두는 네임스페이스
{
    public static class BossActionExecutor // BossActionPlanner가 고른 2x2 이동 또는 근접 공격을 실제 전투 상태에 적용하는 38일차 실행기
    {
        public static bool TryExecute(BossActionCandidate action, RunState runState, TurnManager turnManager, BattleHooks battleHooks, BoardView boardView, out CombatResult combatResult) // 보스 행동 하나를 실행하고 공격 결과를 반환하는 메서드
        {
            combatResult = null; // 이동 또는 실행 실패의 기본 공격 결과는 null

            if (action == null || runState == null || turnManager == null) return false; // 필수 데이터가 없으면 실행 불가
            if (turnManager.CurrentState != TurnState.EnemyTurn) return false; // 실제 적 턴에서만 보스 행동 허용
            if (action.Actor?.Definition == null || action.Actor.IsPlayerPiece || action.Actor.IsDead) return false; // 살아 있는 적 보스만 행동 가능
            if (action.Actor.Definition.Category != PieceCategory.Boss) return false; // 일반 적이 보스 실행기를 사용하지 못하게 제한

            BoardState board = runState.Board; // 현재 전투 보드 참조
            if (!LargePieceBoardUtility.IsFootprintComplete(board, action.Actor)) return false; // 실행 직전에 2x2 점유가 정상인지 확인

            if (action.ActionType == BossActionType.Move) // 이동 후보라면
            {
                if (!TryExecuteMove(action, board, boardView, battleHooks)) return false; // 전체 점유 이동에 실패하면 행동 실패
                Debug.Log($"Boss 이동: {action.Actor.Definition.DisplayName} {action.Origin} -> {action.Target} / Score={action.Score}"); // 실행 결과 개발 로그
                CompleteEnemyTurn(turnManager, battleHooks); // 보스 행동 1회 후 EnemyTurn 종료
                return true; // 이동 실행 성공
            }

            if (action.ActionType == BossActionType.Attack) // 공격 후보라면
            {
                if (!TryExecuteAttack(action, runState, board, boardView, battleHooks, out combatResult)) return false; // 공격 실행 또는 재검증 실패 시 종료

                Debug.Log($"Boss 공격: {action.Actor.Definition.DisplayName} -> {action.TargetPiece?.Definition?.DisplayName} / Damage={combatResult?.DamageDealt ?? 0} / Score={action.Score}"); // 공격 결과 로그

                if (turnManager.CurrentState == TurnState.EnemyTurn) // King 사망 등으로 이미 전투가 끝난 게 아니라면
                {
                    SynchronizeKingAndDefeat(action.TargetPiece, runState, turnManager); // King HP 및 패배 상태 동기화
                }

                if (turnManager.CurrentState == TurnState.EnemyTurn) CompleteEnemyTurn(turnManager, battleHooks); // 전투가 계속되면 보스 행동 후 정상 턴 종료
                return true; // 공격 실행 성공
            }

            return false; // 지원하지 않는 보스 행동 종류
        }

        public static bool TryExecuteTelegraphedAreaAttack(PieceRuntimeState boss, IReadOnlyList<Vector2Int> targetCells, RunState runState, TurnManager turnManager, BattleHooks battleHooks, BoardView boardView, out int hitCount) // 39일차 텔레그래프가 보여 준 동일 칸에 남아 있는 플레이어들을 한 번씩 공격하는 범위 실행 API
        {
            hitCount = 0; // 실제 적중 기물 수 초기화

            if (boss?.Definition == null || targetCells == null || runState == null || turnManager == null) return false; // 필수 데이터가 없으면 실행 불가
            if (turnManager.CurrentState != TurnState.EnemyTurn) return false; // 실제 EnemyTurn에서만 예고 공격 실행
            if (boss.IsPlayerPiece || boss.IsDead || boss.Definition.Category != PieceCategory.Boss || !boss.CanAttack) return false; // 살아 있고 공격 가능한 적 Boss만 사용

            BoardState board = runState.Board; // 현재 실제 전투 보드 참조
            if (board == null || !LargePieceBoardUtility.IsFootprintComplete(board, boss)) return false; // 보스 점유가 깨진 상태에서는 범위 공격 금지

            var targets = new List<PieceRuntimeState>(); // 예고 칸에 현재 남아 있는 고유 플레이어 대상 목록
            var visitedTargets = new HashSet<PieceRuntimeState>(); // 같은 대형 플레이어가 여러 위험 칸을 차지해도 한 번만 공격하기 위한 집합

            for (int i = 0; i < targetCells.Count; i++) // 예고 순간 저장된 모든 위험 칸 순회
            {
                var tile = board.GetTile(targetCells[i]); // 현재 실행 시점의 실제 타일 상태 조회
                var defender = tile?.OccupyingPiece; // 해당 칸에 지금 남아 있는 기물 확인
                if (defender == null || defender.IsDead || !defender.IsPlayerPiece || !visitedTargets.Add(defender)) continue; // 살아 있는 플레이어만 한 번 대상화
                targets.Add(defender); // 실제 적중 대상 목록에 추가
            }

            for (int i = 0; i < targets.Count; i++) // 확정된 플레이어 대상들을 순서대로 공격
            {
                if (turnManager.CurrentState != TurnState.EnemyTurn) break; // 앞선 King 사망 등으로 전투가 끝났으면 추가 공격 중단

                PieceRuntimeState defender = targets[i]; // 현재 공격 대상
                if (defender == null || defender.IsDead) continue; // 앞선 효과로 이미 사망했다면 건너뜀

                battleHooks?.RaiseBeforeAttack(boss, defender); // 기존 공격 전 훅 발행
                CombatResult result = CombatResolver.ResolveAttack(boss, defender, battleHooks); // 일반 보스 공격과 동일한 고정 ATK·Shield·상태 효과 피해 파이프라인 재사용
                hitCount++; // 실제 공격 판정을 받은 플레이어 수 증가

                if (result.DefenderDied) RemovePlayerPiece(defender, runState, board, boardView); // 죽은 플레이어의 점유·뷰·DeadPile 정리
                battleHooks?.RaiseAfterAttack(result); // 기존 전투 로그·공격 후 훅 발행
                SynchronizeKingAndDefeat(defender, runState, turnManager); // King이면 HP 동기화 및 패배 처리
            }

            if (turnManager.CurrentState == TurnState.EnemyTurn) CompleteEnemyTurn(turnManager, battleHooks); // 범위 공격 전체가 끝나면 정상적으로 적 턴 종료
            return true; // 위험 칸에 아무도 없어도 공격 행동 자체는 정상 소비된 것으로 처리
        }

        private static bool TryExecuteMove(BossActionCandidate action, BoardState board, BoardView boardView, BattleHooks battleHooks) // 2x2 점유 전체를 새 Anchor로 이동하는 메서드
        {
            PieceRuntimeState boss = action.Actor; // 이동 주체 보스
            Vector2Int footprint = LargePieceBoardUtility.GetFootprint(boss.Definition); // 보스 점유 크기
            Vector2Int origin = boss.BoardPosition; // 실행 시점 실제 원점
            Vector2Int destination = action.Target; // 후보가 지정한 새 Anchor

            int manhattan = Mathf.Abs(destination.x - origin.x) + Mathf.Abs(destination.y - origin.y); // 한 칸 직교 이동인지 확인하기 위한 거리
            if (manhattan != 1) return false; // 38일차 기본형은 상하좌우 1칸 이동만 허용
            if (!board.CanOccupyArea(destination, footprint, boss)) return false; // 새 영역에 다른 기물·장애물·보드 밖이 있으면 실행 거부

            battleHooks?.RaiseBeforeMove(boss, origin, destination); // 기존 이동 전 훅으로 로그·상태 시스템 연결
            board.ClearPiece(boss); // 현재 2x2 점유 전체 해제
            boss.BoardPosition = destination; // 런타임 기준 Anchor 변경

            if (!board.TryOccupyArea(destination, footprint, boss)) // 사전 검사 뒤 예상치 못하게 점유가 실패하면
            {
                boss.BoardPosition = origin; // 런타임 위치 원복
                board.TryOccupyArea(origin, footprint, boss); // 기존 2x2 영역 복구 시도
                return false; // 행동 실행 실패 반환
            }

            PieceView pieceView = LargePieceVisualUtility.FindPieceView(boss); // 현재 보스 화면 뷰 탐색
            if (pieceView != null && boardView != null) LargePieceVisualUtility.ApplyFootprint(pieceView, boardView.TileSize); // 새 2x2 중앙 위치와 콜라이더를 즉시 반영

            battleHooks?.RaiseAfterMove(boss, origin, destination); // 기존 이동 후 훅 발행
            return true; // 전체 점유 이동 성공
        }

        private static bool TryExecuteAttack(BossActionCandidate action, RunState runState, BoardState board, BoardView boardView, BattleHooks battleHooks, out CombatResult combatResult) // 보스 외곽 인접 공격을 기존 CombatResolver로 실행하는 메서드
        {
            combatResult = null; // 기본 공격 결과 초기화
            PieceRuntimeState boss = action.Actor; // 공격 주체 보스
            PieceRuntimeState defender = action.TargetPiece; // 후보가 가리키는 플레이어 대상

            if (!boss.CanAttack || defender == null || !defender.IsPlayerPiece || defender.IsDead) return false; // 공격권과 대상 상태 재검증
            if (board.GetTile(defender.BoardPosition)?.OccupyingPiece != defender) return false; // 후보 생성 뒤 대상이 이동·제거됐다면 실행하지 않음

            Vector2Int footprint = LargePieceBoardUtility.GetFootprint(boss.Definition); // 현재 보스 점유 크기
            if (!BossActionPlanner.IsCellAdjacentToFootprint(boss.BoardPosition, footprint, defender.BoardPosition)) return false; // 실행 시점에도 2x2 외곽 인접 상태인지 확인

            battleHooks?.RaiseBeforeAttack(boss, defender); // 기존 공격 전 훅 발행
            combatResult = CombatResolver.ResolveAttack(boss, defender, battleHooks); // 기존 고정 ATK·Shield·피해 훅 전투 판정 재사용

            if (combatResult.DefenderDied) // 플레이어 대상이 사망했다면
            {
                RemovePlayerPiece(defender, runState, board, boardView); // 보드·뷰·죽은 카드 더미 정리
            }

            battleHooks?.RaiseAfterAttack(combatResult); // 전투 로그·피격 연출 등 기존 공격 후 훅 발행
            return true; // 공격 처리 성공
        }

        private static void RemovePlayerPiece(PieceRuntimeState defender, RunState runState, BoardState board, BoardView boardView) // 보스에게 죽은 플레이어 기물을 기존 생명주기에 맞춰 제거하는 메서드
        {
            board.ClearPiece(defender); // 향후 대형 플레이어까지 포함해 같은 런타임 기물의 모든 점유 해제

            if (boardView != null) // 실제 Battle Game View가 있으면
            {
                PieceView pieceView = FindPieceView(defender); // 사망 플레이어의 화면 뷰 탐색
                if (pieceView != null) pieceView.PlayDeathTogglingThenDestroy(() => DestroyObject(pieceView.gameObject)); // 기존 30일차 사망 전도 연출 재사용
            }

            runState.Deck.MoveToDeadPile(defender.Definition); // 플레이어 덱 생명주기 규칙대로 죽은 카드 더미에 이동
            RefreshDeckPanelIfPresent(); // 덱 패널의 현재 보유/사망 수를 즉시 갱신
        }

        private static void SynchronizeKingAndDefeat(PieceRuntimeState defender, RunState runState, TurnManager turnManager) // 보스가 King을 공격했을 때 런 상태 HP와 전투 패배를 동기화하는 메서드
        {
            if (!IsKing(defender)) return; // King이 아니면 별도 처리 없음
            runState.KingHp = defender.CurrentHp; // 실제 King 기물 HP를 RunState에 반영
            if (runState.IsDefeated) turnManager.EndBattle(BattleOutcome.Defeat); // HP가 0이면 즉시 패배 처리
        }

        private static void CompleteEnemyTurn(TurnManager turnManager, BattleHooks battleHooks) // 보스 행동 1회 후 기존 턴 종료 흐름을 이어주는 메서드
        {
            if (turnManager.CurrentState != TurnState.EnemyTurn) return; // 이미 전투 종료 또는 다른 상태면 중복 종료 금지

            if (turnManager.CompleteEnemyTurn()) // 기존 배치 주기·다음 PlayerTurn 규칙 그대로 사용
            {
                battleHooks?.RaiseTurnEnd(turnManager.CurrentState, turnManager.TurnNumber); // 기존 일반 AI와 같은 TurnEnd 훅 발행
            }
        }

        private static PieceView FindPieceView(PieceRuntimeState state) // 런타임 상태와 연결된 화면 PieceView를 찾는 메서드
        {
            var views = UnityEngine.Object.FindObjectsByType<PieceView>(FindObjectsSortMode.None); // 현재 씬의 모든 PieceView 조회

            for (int i = 0; i < views.Length; i++) // 뷰 순회
            {
                if (views[i] != null && views[i].RuntimeState == state) return views[i]; // 같은 런타임 상태의 뷰 반환
            }

            return null; // 화면 뷰가 없으면 null
        }

        private static void RefreshDeckPanelIfPresent() // 보스 공격으로 플레이어 카드가 죽었을 때 덱 패널을 즉시 갱신하는 메서드
        {
            var battleController = UnityEngine.Object.FindFirstObjectByType<BattleController>(); // 현재 BattleController 탐색
            var boardInput = UnityEngine.Object.FindFirstObjectByType<BoardInputController>(); // 현재 BoardInputController 탐색

            if (battleController?.DeckPanelUI != null && boardInput != null) battleController.DeckPanelUI.Bind(boardInput); // 기존 Bind를 재사용해 패널 최신화
        }

        private static void DestroyObject(UnityEngine.Object target) // Play/EditMode에 맞춰 Unity 오브젝트를 안전하게 제거하는 메서드
        {
            if (target == null) return; // 이미 제거됐으면 종료
            if (Application.isPlaying) UnityEngine.Object.Destroy(target); // Play Mode에서는 프레임 종료 시 제거
            else UnityEngine.Object.DestroyImmediate(target); // EditMode 테스트에서는 즉시 제거
        }

        private static bool IsKing(PieceRuntimeState piece) // King 여부를 PieceId와 기존 이동 타입 양쪽으로 판별하는 메서드
        {
            if (piece?.Definition == null) return false; // 정의가 없으면 King 아님
            if (string.Equals(piece.Definition.PieceId, "king", StringComparison.OrdinalIgnoreCase)) return true; // PieceId 우선 판별
            return piece.Definition.MovementType == PieceMovementType.King; // Legacy 데이터 호환
        }
    }
}
