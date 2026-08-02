using System;
using SimpleSummon.Network;

namespace SimpleSummon.Runtime
{
    internal sealed class EnemyReplicationPresenter
    {
        private readonly NetworkEnemyState networkState;
        private readonly EnemyNavigation navigation;
        private readonly EnemyPresentation presentation;
        private Action died;

        public EnemyReplicationPresenter(
            NetworkEnemyState networkState,
            EnemyNavigation navigation,
            EnemyPresentation presentation)
        {
            this.networkState = networkState;
            this.navigation = navigation;
            this.presentation = presentation;
        }

        public void Enable(Action onDied)
        {
            if (networkState == null)
            {
                return;
            }

            died = onDied;
            networkState.StateChanged += HandleStateChanged;
            networkState.DisappearedChanged += HandleDisappearedChanged;
        }

        public void Disable()
        {
            if (networkState == null)
            {
                return;
            }

            networkState.StateChanged -= HandleStateChanged;
            networkState.DisappearedChanged -= HandleDisappearedChanged;
            died = null;
        }

        private void HandleStateChanged(bool isDead)
        {
            if (networkState.IsServer)
            {
                return;
            }

            if (isDead)
            {
                navigation.Disable();
                presentation.PlayDeath();
                died?.Invoke();
            }

            HideIfDisappeared();
        }

        private void HandleDisappearedChanged()
        {
            HideIfDisappeared();
        }

        private void HideIfDisappeared()
        {
            if (networkState.Disappeared)
            {
                presentation.Hide();
            }
        }
    }
}
