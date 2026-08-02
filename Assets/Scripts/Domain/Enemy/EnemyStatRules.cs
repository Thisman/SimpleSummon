using System;

namespace SimpleSummon.Domain
{
    public static class EnemyStatRules
    {
        public static float GetStatMultiplier(
            bool isBoss,
            bool artifactCrafted,
            float bossStatMultiplier)
        {
            if (!float.IsFinite(bossStatMultiplier) || bossStatMultiplier <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(bossStatMultiplier));
            }

            return isBoss && !artifactCrafted ? bossStatMultiplier : 1f;
        }
    }
}
