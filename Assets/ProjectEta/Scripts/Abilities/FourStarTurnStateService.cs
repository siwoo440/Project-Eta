using System.Collections.Generic;
using ProjectEta.Battle;
using ProjectEta.Board;
using ProjectEta.Pieces;

namespace ProjectEta.Abilities
{
    public static class FourStarTurnStateService
    {
        public static void ProcessTurnStart(TurnState state, int turnNumber)
        {
            if (state != TurnState.PlayerTurn) return;

            IReadOnlyList<BoardState> boards = AbilityBoardRegistry.SnapshotBoards();

            for (int boardIndex = 0; boardIndex < boards.Count; boardIndex++)
            {
                BoardState board = boards[boardIndex];
                IReadOnlyList<PieceRuntimeState> pieces = AbilityBoardRegistry.GetUniquePieces(board);

                for (int i = 0; i < pieces.Count; i++)
                {
                    PieceRuntimeState piece = pieces[i];
                    if (piece == null || piece.IsDead) continue;

                    if (piece.IsPlayerPiece)
                    {
                        piece.ResetBastionFortifyCycle();
                        piece.ResetOwnTurnMovement();

                        if (FourStarCombatAbilityResolver.HasAbility(
                                piece,
                                FourStarAbilityIds.GatekeeperBlock))
                        {
                            TileBlockService.ClearForSource(piece);
                        }

                        continue;
                    }

                    // 현재 PlayerTurn은 직전 EnemyTurn의 결과를 반영하는 시점이다.
                    // 첫 전투 PlayerTurn에는 직전 EnemyTurn이 없으므로 2턴 이후부터만 적 Bastion을 준비한다.
                    if (turnNumber > 1 &&
                        FourStarCombatAbilityResolver.HasAbility(
                            piece,
                            FourStarAbilityIds.BastionFortify))
                    {
                        bool didNotMove = !piece.MovedSinceOwnTurnStart;
                        piece.ResetBastionFortifyCycle();
                        piece.SetBastionFortifyReady(didNotMove);
                    }

                    // 다음 EnemyTurn 이동 여부를 새로 기록한다.
                    piece.ResetOwnTurnMovement();
                }
            }
        }
    }
}
