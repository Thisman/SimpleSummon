using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class EnemyAnimationEvents : MonoBehaviour
    {
        [SerializeField] private EnemyController controller;

        public void ApplyAttackDamage()
        {
            controller.ApplyAttackDamage();
        }

        public void CompleteDeathAnimation()
        {
            controller.CompleteDeathAnimation();
        }
    }
}
