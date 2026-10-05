using System.Collections.Generic;
using ProjectEta.Battle;
using ProjectEta.Pieces;

namespace ProjectEta.Abilities
{
    public static class TemporaryDamageModifierService
    {
        private static readonly Dictionary<PieceRuntimeState, int> Modifiers =
            new Dictionary<PieceRuntimeState, int>();

        public static int ActiveCount => Modifiers.Count;

        public static void Register(PieceRuntimeState piece, int amount)
        {
            if (piece == null || amount == 0) return;

            if (Modifiers.TryGetValue(piece, out int current))
            {
                Modifiers[piece] = current + amount;
            }
            else
            {
                Modifiers[piece] = amount;
            }
        }

        public static int Peek(PieceRuntimeState piece)
        {
            if (piece == null) return 0;
            return Modifiers.TryGetValue(piece, out int amount) ? amount : 0;
        }

        public static int Consume(PieceRuntimeState piece)
        {
            if (piece == null || !Modifiers.TryGetValue(piece, out int amount)) return 0;
            Modifiers.Remove(piece);
            return amount;
        }

        public static void ApplyAndConsume(DamageContext context)
        {
            if (context?.Source == null) return;

            int amount = Consume(context.Source);
            if (amount == 0) return;

            context.Amount = System.Math.Max(1, context.Amount + amount);
        }

        public static void Clear()
        {
            Modifiers.Clear();
        }
    }
}
