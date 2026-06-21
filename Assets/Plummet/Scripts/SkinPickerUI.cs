using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Plummet
{
    /// <summary>
    /// Builds the "Choose Player" screen at runtime: one tappable card per skin
    /// in <see cref="SkinLibrary"/>, with the current pick highlighted. Selecting
    /// a card saves the choice and re-skins the player immediately.
    /// </summary>
    public sealed class SkinPickerUI : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private PlayerController player;
        [SerializeField] private Color selectedColor = new Color(1f, 0.82f, 0.2f, 1f);
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.18f);
        [SerializeField] private float cardWidth = 240f;
        [SerializeField] private float cardHeight = 300f;
        [SerializeField] private float spacing = 36f;
        [SerializeField] private int nameFontSize = 34;

        private readonly List<Image> cardBackgrounds = new List<Image>();
        private bool built;

        private void OnEnable()
        {
            Build();
            Refresh();
        }

        private void Build()
        {
            if (built)
            {
                return;
            }

            if (content == null)
            {
                content = (RectTransform)transform;
            }

            SkinLibrary library = SkinLibrary.Instance;
            if (library == null)
            {
                return;
            }

            HorizontalLayoutGroup layout = content.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            for (int i = 0; i < library.Count; i++)
            {
                CreateCard(i, library.Get(i), font);
            }

            built = true;
        }

        private void CreateCard(int index, Skin skin, Font font)
        {
            GameObject card = new GameObject($"Skin {index}", typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(content, false);

            LayoutElement element = card.AddComponent<LayoutElement>();
            element.preferredWidth = cardWidth;
            element.preferredHeight = cardHeight;

            Image background = card.GetComponent<Image>();
            background.color = normalColor;
            cardBackgrounds.Add(background);

            GameObject thumb = new GameObject("Thumb", typeof(RectTransform), typeof(Image));
            thumb.transform.SetParent(card.transform, false);
            RectTransform thumbRect = (RectTransform)thumb.transform;
            thumbRect.anchorMin = new Vector2(0.08f, 0.22f);
            thumbRect.anchorMax = new Vector2(0.92f, 0.94f);
            thumbRect.offsetMin = Vector2.zero;
            thumbRect.offsetMax = Vector2.zero;
            Image thumbImage = thumb.GetComponent<Image>();
            thumbImage.sprite = skin != null ? skin.Standing : null;
            thumbImage.preserveAspect = true;
            thumbImage.raycastTarget = false;
            thumbImage.enabled = thumbImage.sprite != null;

            GameObject label = new GameObject("Name", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(card.transform, false);
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = new Vector2(0f, 0.02f);
            labelRect.anchorMax = new Vector2(1f, 0.22f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            Text text = label.GetComponent<Text>();
            text.font = font;
            text.text = skin != null ? skin.DisplayName : "?";
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = nameFontSize;
            text.raycastTarget = false;

            int captured = index;
            Button button = card.GetComponent<Button>();
            button.onClick.AddListener(() => Select(captured));
        }

        public void Select(int index)
        {
            SkinSelection.SelectedIndex = index;
            Refresh();

            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
            }

            if (player != null)
            {
                player.ApplySelectedSkin();
            }

            // Keep the start-screen standing/falling character in sync with the pick.
            UIManager uiManager = FindFirstObjectByType<UIManager>();
            if (uiManager != null)
            {
                uiManager.RefreshStartCharacterSkin();
            }
        }

        private void Refresh()
        {
            int selected = SkinSelection.SelectedIndex;
            for (int i = 0; i < cardBackgrounds.Count; i++)
            {
                if (cardBackgrounds[i] != null)
                {
                    cardBackgrounds[i].color = i == selected ? selectedColor : normalColor;
                }
            }
        }
    }
}
