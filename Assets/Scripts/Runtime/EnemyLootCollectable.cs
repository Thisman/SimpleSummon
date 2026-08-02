using SimpleSummon.Network;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    [RequireComponent(typeof(Collider))]
    public sealed class EnemyLootCollectable : MonoBehaviour
    {
        [SerializeField] private NetworkEnemyState enemyState;
        [SerializeField] private NetworkQuestState questState;
        [SerializeField, Min(0f)] private float bobAmplitude = 0.15f;
        [SerializeField, Min(0f)] private float bobFrequency = 1f;
        [SerializeField] private Vector3 rotationAxes = Vector3.up;
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

        private void OnEnable()
        {
            enemyState.LootStateChanged += RefreshVisibility;
            RefreshVisibility();
        }

        private void OnDisable()
        {
            enemyState.LootStateChanged -= RefreshVisibility;
        }

        private void Update()
        {
            float bob = Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
            transform.localPosition = initialLocalPosition + Vector3.up * bob;
            transform.localRotation = initialLocalRotation *
                                      Quaternion.Euler(rotationAxes.normalized * rotationSpeed * Time.time);
        }

        private void OnTriggerEnter(Collider other)
        {
            NetworkPlayer player = other.GetComponentInParent<NetworkPlayer>();
            if (player == null || !player.CanRunSimulation || !enemyState.TryCollectLoot())
            {
                return;
            }

            questState.CollectArtifactResource();
        }

        public void RefreshVisibility()
        {
            bool visible = enemyState.LootAvailable;
            trigger.enabled = visible;
            foreach (Renderer itemRenderer in GetComponentsInChildren<Renderer>(true))
            {
                itemRenderer.enabled = visible;
            }
        }
    }
}
