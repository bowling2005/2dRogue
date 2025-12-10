using RPG.Core;

namespace RPG.States
{
    public abstract class BaseState : ICharacterState
    {
        protected CharacterBase character;
        protected CharacterState state;

        public CharacterState State => state;

        protected BaseState(CharacterBase character, CharacterState state)
        {
            this.character = character;
            this.state = state;
        }

        public virtual void Enter() { }
        public virtual void Update() { }
        public virtual void FixedUpdate() { }
        public virtual void Exit() { }

        public virtual bool CanTransitionTo(CharacterState newState)
        {
            // 默认允许所有状态转换
            // 可以在子类中重写以限制状态转换
            return true;
        }
    }
}