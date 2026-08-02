using SimpleSummon.Network;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class TorchHudController : MonoBehaviour
    {
        [SerializeField] private TorchView view;

        private NetworkPlayer player;

        private void Awake() => view.Initialize();

        private void OnEnable()
        {
            NetworkPlayer.LocalPlayerChanged += BindLocalPlayer;
            PlayerRegistry.Changed += BindLocalPlayer;
            BindLocalPlayer();
        }

        private void OnDisable()
        {
            NetworkPlayer.LocalPlayerChanged -= BindLocalPlayer;
            PlayerRegistry.Changed -= BindLocalPlayer;
            Unbind();
            view.Hide();
        }

        private void BindLocalPlayer()
        {
            Unbind();
            player = NetworkPlayer.LocalPlayer;
            if (player == null)
            {
                PlayerRegistry.GetLocalPlayer()?.TryGetComponent(out player);
            }
            if (player != null)
            {
                player.TorchChanged += Refresh;
            }
            Refresh();
        }

        private void Unbind()
        {
            if (player != null)
            {
                player.TorchChanged -= Refresh;
                player = null;
            }
        }

        private void Refresh()
        {
            if (player != null && player.HasTorch)
            {
                view.Show(player.TorchStrength);
            }
            else
            {
                view.Hide();
            }
        }
    }
}
