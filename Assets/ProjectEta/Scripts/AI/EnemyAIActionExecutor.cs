using System; // StringComparison을 사용하기 위한 네임스페이스
using UnityEngine; // Object, Vector2Int 등을 사용하기 위한 네임스페이스
using ProjectEta.Battle; // CombatResolver, CombatResult, BattleHooks, TurnManager를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState와 BoardView를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceRuntimeState와 PieceView를 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스

namespace ProjectEta.AI // 적 AI 관련 타입을 모아두는 네임스페이스
{
    public static class EnemyAIActionExecutor // 선택된 AI 후보를 실제 보드·전투·턴 흐름에 적용하는 실행기
    {
        public static bool TryExecute(AIActionCandidate action, RunState runState, TurnManager turnManager, BattleHooks battleHooks, BoardView boardView, out CombatResult combatResult) // 행동 하나를 실행하고 공격이면 결과를 반환하는 메서드
        {
            combatResult = null; // 기본값은 공격 결과 없음

            if (action == null || runState == null || turnManager == null) return false; // 필수 객체가 없으면 실행 불가
            if (turnManager.CurrentState != TurnState.EnemyTurn) return false; // 실제 EnemyTurn이 아니면 AI 실행 금지
            if (action.Actor == null || action.Actor.IsPlayerPiece || action.Actor.IsDead) return false; // 살아 있는 적 기물만 행동 가능

            var board = runState.Board; // 이번 전투의 실제 단일 BoardState 참조
            var originTile = board.GetTile(action.Actor.BoardPosition); // 현재 행동 주체의 실제 원점 타일 조회
            if (originTile == null || originTile.OccupyingPiece != action.Actor) return false; // 후보 생성 뒤 상태가 바뀌었다면 실행하지 않음

            var legalMovement = MovementResolver.GetReachableTiles(action.Actor, board); // 실행 직전에 MovementResolver로 합법성 재검증

            if (action.ActionType == AIActionType.Move) // 이동 행동이면
            {
                if (!legalMovement.MoveTiles.Contains(action.Target)) return false; // 현재 시점에도 합법 이동 칸인지 확인
                var destinationTile = board.GetTile(action.Target); // 실제 목표 타일 조회
                if (destinationTile == null || destinationTile.IsOccupied || destinationTile.IsBlockedByObstacle) return false; // 점유·장애물·범위 오류 방지

                MovePiece(action.Actor, action.Target, board, boardView, battleHooks); // 기존 보드 규칙과 이동 훅을 사용해 실제 이동
                Debug.Log($"Enemy AI 이동: {action.Actor.Definition.DisplayName} {action.Origin} -> {action.Target} / Score={action.Score}"); // 선택 결과 개발 로그
                CompleteEnemyTurn(turnManager, battleHooks); // 적 행동 1회를 소비하고 다음 상태로 전환
                return true; // 이동 실행 성공
            }

            if (action.ActionType == AIActionType.Attack) // 공격 행동이면
            {
                if (!legalMovement.AttackTiles.Contains(action.Target)) return false; // 현재 시점에도 합법 공격 칸인지 확인
                var targetTile = board.GetTile(action.Target); // 실제 공격 대상 타일 조회
                var defender = targetTile?.OccupyingPiece; // 실제 공격 대상 기물 조회
                if (defender == null || !defender.IsPlayerPiece || defender.IsDead) return false; // 살아 있는 플레이어 기물만 공격 가능

                battleHooks?.RaiseBeforeAttack(action.Actor, defender); // 29일차 공통 공격 전 훅 발행
                combatResult = CombatResolver.ResolveAttack(action.Actor, defender, battleHooks); // 기존 고정 ATK 전투 판정과 피해 훅을 그대로 사용

                if (combatResult.DefenderDied) // 대상이 이번 공격으로 사망했으면
                {
                    RemovePlayerPiece(defender, runState, board, boardView); // 보드 점유·화면·죽은 카드 생명주기를 정리

                    if (CombatMovementPolicy.ShouldOccupyDefenderTileAfterKill(action.Actor.Definition)) // 근접 공격자가 처치한 칸을 점유하는 기존 정책이면
                    {
                        MovePiece(action.Actor, action.Target, board, boardView, battleHooks); // 적 공격자도 같은 이동 훅을 거쳐 대상 칸 점유
                    }
                }

                battleHooks?.RaiseAfterAttack(combatResult); // 29·30일차 공격 후 훅으로 전투 로그와 생존 피격 연출을 기존 시스템에 전달
                SynchronizeKingAndDefeat(defender, runState, turnManager); // 킹 공격이었다면 RunState HP와 패배 상태 즉시 동기화

                Debug.Log($"Enemy AI 공격: {action.Actor.Definition.DisplayName} -> {defender.Definition.DisplayName} / Damage={combatResult.DamageDealt}, HP={defender.CurrentHp}, Score={action.Score}"); // 공격 결과 로그

                if (turnManager.CurrentState == TurnState.EnemyTurn) // 킹 사망 등으로 전투가 끝나지 않았다면
                {
                    CompleteEnemyTurn(turnManager, battleHooks); // 적 턴 행동을 정상 완료
                }

                return true; // 공격 실행 성공
            }

            return false; // 지원하지 않는 행동 종류면 실행 실패
        }

        private static void MovePiece(PieceRuntimeState piece, Vector2Int destination, BoardState board, BoardView boardView, BattleHooks battleHooks) // 적 AI 이동의 보드·화면·훅 동기화 공통 메서드
        {
            var origin = piece.BoardPosition; // 이동 전 원점 좌표 저장
            battleHooks?.RaiseBeforeMove(piece, origin, destination); // 29일차 이동 전 훅 발행

            var originTile = board.GetTile(origin); // 원점 타일 조회
            if (originTile != null && originTile.OccupyingPiece == piece) originTile.OccupyingPiece = null; // 현재 기물의 기존 점유 해제

            piece.BoardPosition = destination; // 런타임 기물 좌표 변경(Chameleon이면 기존 규칙대로 이동 순환 단계도 진행)
            var destinationTile = board.GetTile(destination); // 목표 타일 조회
            if (destinationTile != null) destinationTile.OccupyingPiece = piece; // 새 위치 점유 등록

            if (boardView != null) // 실제 Battle 씬에서 화면 보드가 전달됐다면
            {
                var pieceView = FindPieceView(piece); // 같은 RuntimeState를 표시하는 PieceView 탐색
                if (pieceView != null) pieceView.MoveTo(destination, boardView.TileSize); // 기존 30일차 부양 이동 연출을 그대로 사용
            }

            battleHooks?.RaiseAfterMove(piece, origin, destination); // 29일차 이동 후 훅 발행
        }

        private static void RemovePlayerPiece(PieceRuntimeState defender, RunState runState, BoardState board, BoardView boardView) // AI 공격으로 사망한 플레이어 기물을 제거하는 메서드
        {
            var tile = board.GetTile(defender.BoardPosition); // 사망 기물이 있던 기준 타일 조회
            if (tile != null && tile.OccupyingPiece == defender) tile.OccupyingPiece = null; // 1x1 현재 단계의 점유 해제

            if (boardView != null) // 실제 화면이 존재하는 런타임이면
            {
                var pieceView = FindPieceView(defender); // 사망 기물의 PieceView 탐색
                if (pieceView != null) // 화면 기물을 찾았으면
                {
                    pieceView.PlayDeathTogglingThenDestroy(() => DestroyObject(pieceView.gameObject)); // 30일차 랜덤 방향 전도 후 제거 연출 재사용
                }
            }

            runState.Deck.MoveToDeadPile(defender.Definition); // 플레이어 카드 생명주기 규칙대로 죽은 카드 더미에 이동
            RefreshDeckPanelIfPresent(); // 죽은 카드 수 UI가 즉시 바뀌도록 기존 덱 패널을 재바인딩해 갱신
        }

        private static void SynchronizeKingAndDefeat(PieceRuntimeState defender, RunState runState, TurnManager turnManager) // 킹 피격 결과를 전투 종료 상태와 동기화하는 메서드
        {
            if (!IsKing(defender)) return; // 플레이어 킹이 아니면 별도 RunState.KingHp 처리가 필요 없음

            runState.KingHp = defender.CurrentHp; // 실제 킹 기물 HP를 런 상태의 킹 체력과 동기화
            if (runState.IsDefeated) turnManager.EndBattle(BattleOutcome.Defeat); // HP가 0이면 즉시 전투 패배 상태로 전환
        }

        private static void CompleteEnemyTurn(TurnManager turnManager, BattleHooks battleHooks) // AI 행동 뒤 기존 턴 종료·상태 효과 정산 흐름을 이어주는 메서드
        {
            if (turnManager.CurrentState != TurnState.EnemyTurn) return; // 이미 전투 종료 또는 다른 상태면 중복 처리하지 않음

            if (turnManager.CompleteEnemyTurn()) // 기존 TurnManager를 통해 배치 턴 또는 다음 플레이어 턴으로 정상 전환
            {
                battleHooks?.RaiseTurnEnd(turnManager.CurrentState, turnManager.TurnNumber); // 기존 더미 적 턴과 같은 TurnEnd 훅을 발행해 독·화상 지속 턴 정산 유지
            }
        }

        private static PieceView FindPieceView(PieceRuntimeState state) // 런타임 상태와 연결된 화면 기물 뷰를 찾는 메서드
        {
            var views = UnityEngine.Object.FindObjectsByType<PieceView>(FindObjectsSortMode.None); // 현재 씬의 모든 PieceView 조회
            for (int i = 0; i < views.Length; i++) // 조회된 뷰를 순회
            {
                if (views[i] != null && views[i].RuntimeState == state) return views[i]; // 같은 런타임 상태를 표시하는 뷰 반환
            }

            return null; // 연결된 화면 뷰가 없으면 null 반환
        }

        private static void RefreshDeckPanelIfPresent() // AI에 의해 플레이어 카드가 죽었을 때 덱 패널 표시를 즉시 새로고침하는 메서드
        {
            var battleController = UnityEngine.Object.FindFirstObjectByType<BattleController>(); // 현재 전투 컨트롤러 탐색
            var boardInput = UnityEngine.Object.FindFirstObjectByType<BoardInputController>(); // 실제 보드 입력 컨트롤러 탐색
            if (battleController?.DeckPanelUI != null && boardInput != null) battleController.DeckPanelUI.Bind(boardInput); // 기존 Bind가 버튼·열린 패널을 현재 상태로 즉시 갱신
        }

        private static void DestroyObject(UnityEngine.Object target) // Play/EditMode에 맞춰 오브젝트를 안전하게 제거하는 메서드
        {
            if (target == null) return; // 이미 제거됐다면 종료
            if (Application.isPlaying) UnityEngine.Object.Destroy(target); // Play Mode에서는 프레임 종료 시 안전하게 제거
            else UnityEngine.Object.DestroyImmediate(target); // EditMode 테스트에서는 즉시 제거
        }

        private static bool IsKing(PieceRuntimeState piece) // 플레이어 킹 여부를 PieceId와 Legacy 이동 타입 양쪽으로 판별하는 메서드
        {
            if (piece?.Definition == null) return false; // 정의가 없으면 킹이 아님
            if (string.Equals(piece.Definition.PieceId, "king", StringComparison.OrdinalIgnoreCase)) return true; // PieceId 우선 확인
            return piece.Definition.MovementType == PieceMovementType.King; // 구형 데이터 호환을 위해 MovementType도 확인
        }
    }
}
