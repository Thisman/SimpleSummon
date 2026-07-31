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

        [Header("Idle Animation")]
        [SerializeField, Min(0f)] private float bobAmplitude = 0.15f;
        [SerializeField, Min(0f)] private float bobFrequency = 1f;
        [SerializeField] private Vector3 rotationAxes = Vector3.forward;
        [SerializeField] private float rotationSpeed = 45f;

        private Collider trigger;
        private Vector3 initialLocalPosition;
        private Quaternion initialLocalRotation;

        private void Awake()
        {
            trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
            initialLocalPosition = transform.localPosition;
            initialLocalRotation = transform.localRotation;
        }

        private void Update()
        {
            float bobOffset = Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
            transform.localPosition = initialLocalPosition + Vector3.up * bobOffset;
            transform.localRotation = initialLocalRotation *
                                      Quaternion.Euler(rotationAxes.normalized * rotationSpeed * Time.time);
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
