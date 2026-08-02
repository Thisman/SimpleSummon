using SimpleSummon.Network;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class PlayerStatsController : MonoBehaviour
    {
        [SerializeField] private PlayerStatsView view;

        private PlayerController localController;

        private void OnEnable()
        {
            NetworkPlayer.LocalPlayerChanged += BindLocalPlayer;
            PlayerRegistry.Changed += BindLocalPlayer;
            GameLocalizationController.AddLocaleChangedListener(RefreshHealth);
            BindLocalPlayer();
        }

        private void OnDisable()
        {
            NetworkPlayer.LocalPlayerChanged -= BindLocalPlayer;
            PlayerRegistry.Changed -= BindLocalPlayer;
            GameLocalizationController.RemoveLocaleChangedListener(RefreshHealth);
            UnbindLocalPlayer();
        }

        private void BindLocalPlayer()
        {
            UnbindLocalPlayer();
            NetworkPlayer localPlayer = NetworkPlayer.LocalPlayer;
            if (localPlayer == null)
            {
                localPlayer = PlayerRegistry.GetLocalPlayer()?.GetComponent<NetworkPlayer>();
            }
            if (localPlayer == null)
            {
                return;
            }

            localController = localPlayer.GetComponent<PlayerController>();
            localController.VitalStateChanged += HandleVitalStateChanged;
            RefreshHealth(localController.CurrentHealth);
        }

        private void UnbindLocalPlayer()
        {
            if (localController != null)
            {
                localController.VitalStateChanged -= HandleVitalStateChanged;
            }
            localController = null;
        }

        private void HandleVitalStateChanged(float health, float _) => RefreshHealth(health);

        private void RefreshHealth(float health)
        {
            view.SetHealth(health, localController != null ? localController.MaximumHealth : 0f);
        }

        private void RefreshHealth() =>
            RefreshHealth(localController != null ? localController.CurrentHealth : 0f);
    }
}
