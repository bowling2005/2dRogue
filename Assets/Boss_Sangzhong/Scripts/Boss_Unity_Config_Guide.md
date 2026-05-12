# Boss Unity Configuration Guide

## 1. Script Structure After Refactor

### Core controller
- `Boss.cs`
  - Acts as the single gameplay coordinator.
  - Owns runtime health, movement speed, patrol progression, skill registration, and state machine startup.
  - Centralizes Inspector config into `Stats`, `Detection`, `Patrol`, and `Combat`.

### State flow
- `StateMachine.cs`
- `IdleState.cs`
- `PatrolState.cs`
- `DiscoverState.cs`
- `FightState.cs`
- `HurtState.cs`
- `DeathState.cs`

### Combat and decision
- `SkillManager.cs`
- `SkillDecisionMaker.cs`
- `MovementDecisionMaker.cs`
- `Skill.cs`
- `MeleeAttackSkill.cs`
- `ProjectileSkill.cs`

### Sensing and movement
- `PlayerDetector.cs`
- `BossAutoPathfinding.cs`

## 2. Required Components On Boss GameObject

Attach these to the same Boss root object:

1. `Boss`
2. `Rigidbody2D`
3. `Animator`
4. `PlayerDetector`
5. `BossAutoPathfinding`
6. `BoxCollider2D`

Recommended:

- Use one `BoxCollider2D` for player detection trigger.
- Use your normal hit / body collider separately if combat collision and detection should not share the same shape.

## 3. Rigidbody2D Setup

Suggested values:

- `Body Type`: `Dynamic`
- `Gravity Scale`: according to your platformer setup
- `Freeze Rotation Z`: enabled
- `Collision Detection`: `Continuous` if the boss moves fast

## 4. Animator Setup

The current scripts expect these Animator parameters:

- `Bool IsMoving`
- `Bool IsActing`
- `Bool isRunning`
- `Bool isJumping`
- `Bool isClimbing`
- `Bool isFalling`
- `Trigger OnDiscover`
- `Trigger IsHurting`
- `Trigger Die`
- `Trigger Attack_Melee`
- `Trigger Attack_Range`
- `Float speed`

Animation Event callbacks expected on the Boss object:

- `OnActionEnd()`
  - Call this at the end of attack / action animations to release `IsActing`.
- `OnHurtAnimationEnd()`
  - Call this at the end of the hurt animation so the state machine can return to the interrupted state.

## 5. Boss Inspector Configuration

### Stats
- `Max Health`: boss max HP
- `Base Move Speed`: default movement speed used by normal states
- `Attack Speed`: reserved for attack pacing if you expand combat logic later

### Detection
- `Spot Threshold`: number of successful observation ticks before switching from discover behavior into fight

### Patrol
- `Patrol Points`: assign waypoint transforms in order
- `Idle Wait Range X/Y`: random idle wait min and max
- `Patrol Speed`: movement speed used during patrol

### Combat
- `Projectile Prefab`: projectile used by `ProjectileSkill`
- `Low Health Ratio`: threshold for low-health event logic
- `Fight Detection Multiplier`: expands detection range while fighting
- `Immediate Fight Distance`: direct enter-fight distance during discover
- `Retreat Speed Multiplier`: movement speed multiplier when moving away from player

## 6. PlayerDetector Setup

On `PlayerDetector`:

1. Set `Player Tag` to the tag used by the player object.
2. Set `Base Detection Range`.
3. Set `Observe Interval`.
4. Make sure the detection collider is a trigger.
5. Ensure the player has a collider and a matching tag.

Notes:

- `PlayerDetector` updates trigger size and forward offset automatically based on facing direction.
- In fight state, detection range is expanded automatically and restored when exiting combat.

## 7. BossAutoPathfinding Setup

Assign:

1. `Boss`: the same root boss object
2. `Self Collider`: the collider used to measure body height
3. `Obstacle Layers`: ground / wall layers that should block movement

Tune if needed:

- `Replan Interval`
- `Ray Distance`
- `Ground Extra`
- `Jump Force`
- `Climb Speed Y`

Notes:

- `MoveTowardsTarget()` and `MoveAwayFromTarget()` are called by fight logic.
- The script now focuses on movement planning and execution, while `Boss` remains the gameplay coordinator.

## 8. Scene Wiring Checklist

1. Create the boss root object.
2. Add `Rigidbody2D`, `Animator`, `Boss`, `PlayerDetector`, `BossAutoPathfinding`, and collider components.
3. Create patrol point empty objects in the scene.
4. Drag patrol points into `Boss > Patrol > Patrol Points`.
5. Set the projectile prefab in `Boss > Combat > Projectile Prefab`.
6. Set obstacle layers in `BossAutoPathfinding`.
7. Ensure player object tag matches `PlayerDetector`.
8. Ensure Animator contains all required parameters and transitions.
9. Add animation events to hurt / attack clips.
10. Enter Play Mode and watch the current state label for quick debugging.

## 9. Recommended Validation Flow

### Idle and patrol
1. Press Play.
2. Confirm the boss enters `IdleState`.
3. Confirm it transitions to `PatrolState`.
4. Confirm it walks between patrol points and bounces back at the ends.

### Discover and fight
1. Move player into detection range.
2. Confirm repeated observation eventually triggers `IntoFight`.
3. Confirm `DiscoverState` can promote into `FightState`.
4. Confirm the boss moves toward or away from the player and can cast skills.

### Hurt and death
1. Call `TakeDamage()` from your combat system.
2. Confirm hurt animation triggers.
3. Confirm `OnHurtAnimationEnd()` returns to the prior state.
4. Confirm lethal damage enters `DeathState`.

## 10. Common Troubleshooting

### Boss does not move
- Check `Rigidbody2D` is on the same object as `Boss`.
- Check `BossAutoPathfinding` has `Boss` and `Self Collider` assigned.
- Check `Obstacle Layers` includes the ground and wall layers you actually use.

### Boss never detects player
- Check player tag.
- Check trigger collider is enabled.
- Check the player's collider enters the detector trigger.
- Check `Base Detection Range` is large enough.

### Range attack does nothing
- Check `Boss > Combat > Projectile Prefab` is assigned.
- Check projectile prefab itself has its own movement / hit logic.

### Hurt animation never returns
- Check the hurt clip has an animation event that calls `OnHurtAnimationEnd`.

### Action lock never clears
- Check attack clips call `OnActionEnd()`.
- Check your Animator actually sets `IsActing` during action animations.
