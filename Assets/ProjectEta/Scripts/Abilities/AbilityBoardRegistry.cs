using System; // WeakReference 사용
using System.Collections.Generic; // List·HashSet 사용
using UnityEngine; // Vector2Int 사용
using ProjectEta.Board; // BoardState 사용
using ProjectEta.Pieces; // PieceRuntimeState 사용

namespace ProjectEta.Abilities
{
    public static class AbilityBoardRegistry
    {
        private static readonly List<WeakReference<BoardState>> Boards =
            new List<WeakReference<BoardState>>();

        public static void Register(BoardState board)
        {
            if (board == null) return;

            for (int i = Boards.Count - 1; i >= 0; i--)
            {
                if (!Boards[i].TryGetTarget(out BoardState existing) || existing == null)
                {
                    Boards.RemoveAt(i);
                    continue;
                }

                if (object.ReferenceEquals(existing, board)) return;
            }

            Boards.Add(new WeakReference<BoardState>(board));
        }

        public static BoardState FindBoardContaining(PieceRuntimeState piece)
        {
            if (piece == null) return null;

            for (int i = Boards.Count - 1; i >= 0; i--)
            {
                if (!Boards[i].TryGetTarget(out BoardState board) || board == null)
                {
                    Boards.RemoveAt(i);
                    continue;
                }

                for (int x = 0; x < BoardState.Width; x++)
                {
                    for (int y = 0; y < BoardState.Height; y++)
                    {
                        if (object.ReferenceEquals(board.GetTile(new Vector2Int(x, y)).OccupyingPiece, piece))
                        {
                            return board;
                        }
                    }
                }
            }

            return null;
        }

        public static IReadOnlyList<PieceRuntimeState> GetUniquePieces(BoardState board)
        {
            var result = new List<PieceRuntimeState>();
            if (board == null) return result;

            var unique = new HashSet<PieceRuntimeState>();

            for (int x = 0; x < BoardState.Width; x++)
            {
                for (int y = 0; y < BoardState.Height; y++)
                {
                    PieceRuntimeState piece = board.GetTile(new Vector2Int(x, y)).OccupyingPiece;
                    if (piece == null || !unique.Add(piece)) continue;
                    result.Add(piece);
                }
            }

            return result;
        }


        public static IReadOnlyList<BoardState> SnapshotBoards()
        {
            var result = new List<BoardState>();

            for (int i = Boards.Count - 1; i >= 0; i--)
            {
                if (!Boards[i].TryGetTarget(out BoardState board) || board == null)
                {
                    Boards.RemoveAt(i);
                    continue;
                }

                result.Add(board);
            }

            return result;
        }

        public static void Clear()
        {
            Boards.Clear(); // EditMode 회귀 테스트용 명시적 정리 API
        }
    }
}
