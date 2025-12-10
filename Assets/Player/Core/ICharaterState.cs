using System.Collections.Generic;

namespace RPG.Core
{
    public interface ICharacterState
    {
        CharacterState State { get; }
        void Enter();
        void Update();
        void FixedUpdate();
        void Exit();
        bool CanTransitionTo(CharacterState newState);
    }
}