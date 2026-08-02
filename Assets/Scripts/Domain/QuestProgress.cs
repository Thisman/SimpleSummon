namespace SimpleSummon.Domain
{
    public sealed class QuestProgress
    {
        public bool BossHeartCollected { get; private set; }
        public bool SignDrawn { get; private set; }

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

        public void Apply(bool bossHeart, bool signDrawn)
        {
            BossHeartCollected = bossHeart;
            SignDrawn = signDrawn;
        }
    }
}
