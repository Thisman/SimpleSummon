using TMPro;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class PlayerStatsView : MonoBehaviour
    {
        [SerializeField] private TMP_Text healthText;

        public void SetHealth(float current, float maximum)
        {
            healthText.text = GameLocalizationController.FormatHealth(current, maximum);
        }
    }
}
