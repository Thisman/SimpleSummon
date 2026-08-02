using System.Reflection;
using NUnit.Framework;
using SimpleSummon.Domain;
using SimpleSummon.Network;
using SimpleSummon.Runtime;
using TMPro;
using UnityEngine;

namespace SimpleSummon.Tests.PlayMode
{
    public sealed class QuestUiControllerTests
    {
        [Test]
        public void IngredientsController_RefreshesImmediatelyOnChangeAndReenable()
        {
            GameObject questObject = new("Quest State");
            NetworkQuestState quest = questObject.AddComponent<NetworkQuestState>();
            GameObject ui = new("Ingredients UI");
            ui.SetActive(false);
            TextMeshProUGUI text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI))
                .GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(ui.transform);
            IngredientsView view = ui.AddComponent<IngredientsView>();
            SetField(view, "ingredientsText", text);
            IngredientsController controller = ui.AddComponent<IngredientsController>();
            SetField(controller, "questState", quest);
            SetField(controller, "view", view);

            try
            {
                ui.SetActive(true);
                Assert.That(text.text, Does.Contain("green - 0"));

                quest.CollectIngredient(IngredientType.BottleGreen);
                Assert.That(text.text, Does.Contain("green - 1"));

                ui.SetActive(false);
                quest.CollectIngredient(IngredientType.BottleBrown);
                ui.SetActive(true);
                Assert.That(text.text, Does.Contain("brown - 1"));
            }
            finally
            {
                Object.DestroyImmediate(ui);
                Object.DestroyImmediate(questObject);
            }
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {name}");
            field.SetValue(target, value);
        }
    }
}
