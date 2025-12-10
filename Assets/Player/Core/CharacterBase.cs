using UnityEngine;
using System.Collections.Generic;
using RPG.Equipment;

namespace RPG.Core
{
    public abstract class CharacterBase : MonoBehaviour
    {
        [Header("角色属性")]
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected float currentHealth;

        [Header("移动属性")]
        [SerializeField] protected float moveSpeed = 5f;
        [SerializeField] protected float jumpForce = 10f;
        [SerializeField] protected int maxJumpCount = 1;

        [Header("组件引用")]
        [SerializeField] protected Rigidbody2D rb;
        [SerializeField] protected Animator animator;
        [SerializeField] protected Collider2D groundCheck;

        [Header("装备系统")]
        [SerializeField] protected List<EquipmentBase> equippedItems = new List<EquipmentBase>();

        // 状态管理
        protected CharacterState currentState;
        protected Dictionary<CharacterState, ICharacterState> stateInstances;
        protected ICharacterState activeState;

        // 属性
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float MoveSpeed => moveSpeed;
        public float JumpForce => jumpForce;
        public int CurrentJumpCount { get; protected set; }
        public bool IsGrounded { get; protected set; }
        public Vector2 Velocity => rb.velocity;
        public CharacterState CurrentState => currentState;

        // 事件
        public System.Action<float> OnHealthChanged;
        public System.Action<CharacterState> OnStateChanged;
        public System.Action OnDeath;

        protected virtual void Awake()
        {
            InitializeComponents();
            InitializeStates();
            currentHealth = maxHealth;
        }

        protected virtual void InitializeComponents()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (animator == null) animator = GetComponent<Animator>();
        }

        protected abstract void InitializeStates();

        protected virtual void Update()
        {
            activeState?.Update();
            UpdateAnimation();
        }

        protected virtual void FixedUpdate()
        {
            CheckGround();
            activeState?.FixedUpdate();
        }

        protected virtual void CheckGround()
        {
            if (groundCheck != null)
            {
                IsGrounded = Physics2D.OverlapCollider(groundCheck, new ContactFilter2D(), new Collider2D[1]) > 0;
                if (IsGrounded) CurrentJumpCount = 0;
            }
        }

        public virtual bool ChangeState(CharacterState newState)
        {
            if (activeState != null && !activeState.CanTransitionTo(newState))
                return false;

            if (stateInstances.TryGetValue(newState, out var nextState))
            {
                activeState?.Exit();
                currentState = newState;
                activeState = nextState;
                activeState.Enter();

                OnStateChanged?.Invoke(newState);
                return true;
            }

            return false;
        }

        protected virtual void UpdateAnimation()
        {
            if (animator == null) return;

            // 设置基础动画参数
            animator.SetBool("IsGrounded", IsGrounded);
            animator.SetFloat("VerticalVelocity", rb.velocity.y);

            // 根据状态设置特定参数
            animator.SetBool("IsRunning", currentState.HasFlag(CharacterState.Running));
            animator.SetBool("IsJumping", currentState.HasFlag(CharacterState.Jumping));
            animator.SetBool("IsFalling", currentState.HasFlag(CharacterState.Falling));
        }

        public virtual void PlayAnimation(string animationName)
        {
            if (animator == null) return;

            // 检查动画是否存在
            if (HasAnimation(animationName))
            {
                animator.Play(animationName);
            }
            else
            {
                Debug.LogWarning($"动画 '{animationName}' 不存在于 Animator Controller 中");
            }
        }

        protected virtual bool HasAnimation(string animationName)
        {
            if (animator.runtimeAnimatorController == null) return false;

            foreach (var clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == animationName)
                    return true;
            }

            return false;
        }

        public virtual void TakeDamage(float damage)
        {
            if (currentHealth <= 0) return;

            currentHealth = Mathf.Max(0, currentHealth - damage);
            OnHealthChanged?.Invoke(currentHealth);

            if (currentHealth <= 0)
            {
                Die();
            }
            else
            {
                ChangeState(CharacterState.Damaged);
            }
        }

        public virtual void Heal(float amount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth);
        }

        protected virtual void Die()
        {
            ChangeState(CharacterState.Dead);
            OnDeath?.Invoke();
        }

        // 装备系统方法
        public virtual void EquipItem(EquipmentBase item)
        {
            if (equippedItems.Contains(item)) return;

            equippedItems.Add(item);
            item.OnEquip(this);
        }

        public virtual void UnequipItem(EquipmentBase item)
        {
            if (!equippedItems.Contains(item)) return;

            equippedItems.Remove(item);
            item.OnUnequip(this);
        }

        // 用于持久化保存的数据获取/设置方法
        public virtual CharacterData GetCharacterData()
        {
            return new CharacterData
            {
                health = currentHealth,
                position = transform.position,
                rotation = transform.rotation,
                state = currentState
            };
        }

        public virtual void LoadCharacterData(CharacterData data)
        {
            currentHealth = data.health;
            transform.position = data.position;
            transform.rotation = data.rotation;

            // 根据需要加载状态
            ChangeState(data.state);
        }
    }
}