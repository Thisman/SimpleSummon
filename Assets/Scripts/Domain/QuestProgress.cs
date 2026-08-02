using System;

namespace SimpleSummon.Domain
{
    public sealed class QuestProgress
    {
        public const int SignFragmentCount = 8;
        public const int ArtifactResourceRequirement = 5;

        private byte collectedSignFragments;

        public byte SignFragmentMask => collectedSignFragments;
        public int CollectedSignFragmentCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < SignFragmentCount; i++)
                {
                    if (IsSignFragmentCollected(i))
                    {
                        count++;
                    }
                }

                return count;
            }
        }
        public bool BossHeartCollected { get; private set; }
        public int ArtifactResourceCount { get; private set; }
        public bool ArtifactCrafted { get; private set; }
        public bool SignDrawn { get; private set; }

        public bool CollectArtifactResource()
        {
            if (ArtifactCrafted || ArtifactResourceCount >= ArtifactResourceRequirement)
            {
                return false;
            }

            ArtifactResourceCount++;
            return true;
        }

        public bool CraftArtifact()
        {
            if (ArtifactCrafted || ArtifactResourceCount != ArtifactResourceRequirement)
            {
                return false;
            }

            ArtifactResourceCount = 0;
            ArtifactCrafted = true;
            return true;
        }

        public bool CollectSignFragment(int fragmentId)
        {
            ValidateFragmentId(fragmentId);
            byte fragmentMask = (byte)(1 << fragmentId);
            if ((collectedSignFragments & fragmentMask) != 0)
            {
                return false;
            }

            collectedSignFragments |= fragmentMask;
            return true;
        }

        public bool IsSignFragmentCollected(int fragmentId)
        {
            ValidateFragmentId(fragmentId);
            return (collectedSignFragments & 1 << fragmentId) != 0;
        }

        public bool CollectBossHeart()
        {
            if (BossHeartCollected)
            {
                return false;
            }

            BossHeartCollected = true;
            return true;
        }

        public bool DrawSign()
        {
            if (SignDrawn)
            {
                return false;
            }

            SignDrawn = true;
            return true;
        }

        public void Apply(
            byte signFragments,
            bool bossHeart,
            bool signDrawn,
            int artifactResources = 0,
            bool artifactCrafted = false)
        {
            if (artifactResources < 0 || artifactResources > ArtifactResourceRequirement)
            {
                throw new ArgumentOutOfRangeException(nameof(artifactResources));
            }

            collectedSignFragments = signFragments;
            BossHeartCollected = bossHeart;
            SignDrawn = signDrawn;
            ArtifactResourceCount = artifactResources;
            ArtifactCrafted = artifactCrafted;
        }

        private static void ValidateFragmentId(int fragmentId)
        {
            if (fragmentId < 0 || fragmentId >= SignFragmentCount)
            {
                throw new ArgumentOutOfRangeException(nameof(fragmentId));
            }
        }
    }
}
