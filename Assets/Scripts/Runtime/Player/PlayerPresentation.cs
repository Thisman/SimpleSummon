using UnityEngine;

namespace SimpleSummon.Runtime
{
    internal sealed class PlayerPresentation
    {
        private static readonly int MovementSpeedId = Animator.StringToHash("MovementSpeed");
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int DeathId = Animator.StringToHash("Death");
        private static readonly int RespawnId = Animator.StringToHash("Respawn");

        private readonly Animator animator;
        private readonly DamageFlash damageFlash;

        public PlayerPresentation(Animator animator, DamageFlash damageFlash)
        {
            this.animator = animator;
            this.damageFlash = damageFlash;
        }

        public void SetMovementSpeed(float value, float deltaTime) =>
            animator.SetFloat(MovementSpeedId, value, 0.1f, deltaTime);

        public void StopMovement() => animator.SetFloat(MovementSpeedId, 0f);
        public void PlayAttack() => animator.SetTrigger(AttackId);
        public void PlayDamage() => damageFlash.Play();
        public void PlayDeath() => animator.SetTrigger(DeathId);
        public void PlayRespawn() => animator.SetTrigger(RespawnId);
    }
}
