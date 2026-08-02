using System;
using Unity.Collections;
using Unity.Netcode;

namespace SimpleSummon.Network
{
    public sealed class NetworkPlayerIdentity : NetworkBehaviour
    {
        private readonly NetworkVariable<FixedString64Bytes> nickname = new("Player");

        public event Action<string> Changed;
        public string Nickname => IsSpawned
            ? nickname.Value.ToString()
            : Normalize(NicknameStorage.Load());

        public override void OnNetworkSpawn()
        {
            nickname.OnValueChanged += HandleChanged;
            if (IsOwner)
            {
                Set(NicknameStorage.Load());
            }
            Changed?.Invoke(Nickname);
        }

        public override void OnNetworkDespawn()
        {
            nickname.OnValueChanged -= HandleChanged;
        }

        private void Set(string value)
        {
            string normalized = Normalize(value);
            if (IsServer)
            {
                nickname.Value = normalized;
            }
            else
            {
                SetRpc(normalized);
            }
        }

        [Rpc(SendTo.Server)]
        private void SetRpc(FixedString64Bytes value)
        {
            nickname.Value = Normalize(value.ToString());
        }

        private void HandleChanged(FixedString64Bytes _, FixedString64Bytes value) =>
            Changed?.Invoke(value.ToString());

        private static string Normalize(string value)
        {
            string result = string.IsNullOrWhiteSpace(value) ? "Player" : value.Trim();
            return result.Length <= 32 ? result : result.Substring(0, 32);
        }
    }
}
