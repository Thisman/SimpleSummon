using SimpleSummon.Domain;
using SimpleSummon.Network;
using Unity.Netcode;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class EnemyIngredientCollectable : MonoBehaviour
    {
        [SerializeField] private IngredientType ingredient;
        [SerializeField] private NetworkEnemyState enemyState;
        [SerializeField] private NetworkQuestState questState;
        [SerializeField] private Collider pickupCollider;
        [SerializeField] private GameObject greenBottle;
        [SerializeField] private GameObject brownBottle;

        private void OnEnable()
        {
            enemyState.DisappearedChanged += Refresh;
            enemyState.LootChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            enemyState.DisappearedChanged -= Refresh;
            enemyState.LootChanged -= Refresh;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null ||
                !enemyState.Disappeared ||
                enemyState.LootCollected ||
                (NetworkManager.Singleton != null &&
                 NetworkManager.Singleton.IsListening &&
                 !NetworkManager.Singleton.IsServer))
            {
                return;
            }

            if (questState.CollectIngredient(ingredient))
            {
                enemyState.PublishLootCollected();
            }
        }

        private void Refresh()
        {
            bool visible = ingredient != IngredientType.None &&
                           enemyState.Disappeared &&
                           !enemyState.LootCollected;
            pickupCollider.enabled = visible;
            greenBottle.SetActive(visible && ingredient == IngredientType.BottleGreen);
            brownBottle.SetActive(visible && ingredient == IngredientType.BottleBrown);
        }
    }
}
