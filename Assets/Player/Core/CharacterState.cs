using System;

namespace RPG.Core
{
    [Flags]
    public enum CharacterState
    {
        Idle = 1 << 0,
        Running = 1 << 1,
        Jumping = 1 << 2,
        Falling = 1 << 3,
        Grounded = 1 << 4,
        Attacking = 1 << 5,
        Damaged = 1 << 6,
        Dead = 1 << 7
    }
}