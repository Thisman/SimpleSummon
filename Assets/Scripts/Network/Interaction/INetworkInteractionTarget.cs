using UnityEngine;

namespace SimpleSummon.Network
{
    public interface INetworkInteractionTarget
    {
        void InteractOnServer(GameObject interactor);
    }
}
