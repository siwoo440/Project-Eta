using System;

namespace ProjectEta.Pieces
{
    [Flags]
    public enum PieceRoleTag
    {
        None = 0,
        Melee = 1 << 0,
        Ranged = 1 << 1,
        Jumper = 1 << 2,
        Slider = 1 << 3,
        Rider = 1 << 4,
        Support = 1 << 5,
        Tanker = 1 << 6,
        Attacker = 1 << 7,
        Summoner = 1 << 8
    }
}
