using TMPro;
using UnityEngine;

namespace SimpleSummon.Runtime
{
    public sealed class IngredientsView : MonoBehaviour
    {
        [SerializeField] private TMP_Text ingredientsText;

        public void SetCounts(int greenBottles, int brownBottles)
        {
            ingredientsText.text =
                $"bottle_C_green - {greenBottles}\n" +
                $"bottle_C_brown - {brownBottles}";
        }
    }
}
