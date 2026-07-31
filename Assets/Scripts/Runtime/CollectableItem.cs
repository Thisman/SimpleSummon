using SimpleSummon.Domain;
using SimpleSummon.Network;
using Unity.Netcode;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    [RequireComponent(typeof(Collider))]
    public sealed class CollectableItem : MonoBehaviour
    {
        [SerializeField] private QuestCollectableType type;
        [SerializeField, Range(0, QuestProgress.SignFragmentCount - 1)]
        private int signFragmentId;
        [SerializeField] private NetworkQuestState questState;

        private Collider trigger;

        private void Awake()
        {
            trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        private void OnEnable()
        {
            questState.Changed += RefreshVisibility;
            RefreshVisibility();
        }

        private void OnDisable()
        {
            questState.Changed -= RefreshVisibility;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out PlayerController _) ||
                !other.TryGetComponent(out NetworkPlayer player) ||
                !player.CanRunSimulation)
            {
                return;
            }

            questState.Collect(type, signFragmentId);
        }

        private void RefreshVisibility()
        {
            bool visible = !questState.IsCollected(type, signFragmentId);
            trigger.enabled = visible;
            foreach (Renderer itemRenderer in GetComponentsInChildren<Renderer>(true))
            {
                itemRenderer.enabled = visible;
            }
        }
    }
}
