using UnityEngine;

namespace SimpleSummon.Runtime
{
    internal sealed class EnemyPresentation
    {
        private static readonly int MovementSpeedId = Animator.StringToHash("MovementSpeed");
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int DeathId = Animator.StringToHash("Death");

        private readonly Animator animator;
        private readonly DamageFlash damageFlash;
        private readonly Renderer[] visualRenderers;
        private readonly CapsuleCollider capsuleCollider;

        public EnemyPresentation(
            Animator animator,
            DamageFlash damageFlash,
            Renderer[] visualRenderers,
            CapsuleCollider capsuleCollider)
        {
            this.animator = animator;
            this.damageFlash = damageFlash;
            this.visualRenderers = visualRenderers;
            this.capsuleCollider = capsuleCollider;
        }

        public void SetMovementSpeed(float value, float deltaTime)
        {
            animator.SetFloat(MovementSpeedId, value, 0.1f, deltaTime);
        }

        public void PlayAttack() => animator.SetTrigger(AttackId);
        public void PlayDamage() => damageFlash.Play();

        public void PlayDeath()
        {
            capsuleCollider.enabled = false;
            animator.SetFloat(MovementSpeedId, 0f);
            animator.SetTrigger(DeathId);
        }

        public void Hide()
        {
            foreach (Renderer visualRenderer in visualRenderers)
            {
                if (visualRenderer != null)
                {
                    visualRenderer.enabled = false;
                }
            }
        }
    }
}
