using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WordShot.EditorTools
{
    /// <summary>
    /// エディタ用ツールで UI の部品を作るときの共通処理。
    /// シーンやプレハブを自動で作るツール（GameLoopSceneBuilder など）から使う。
    /// </summary>
    public static class EditorUIFactory
    {
        public static Transform CreateCanvas()
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            return go.transform;
        }

        public static RectTransform CreateRect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static Image CreateImage(Transform parent, string name, Color color, Vector2 position, Vector2 size)
        {
            RectTransform rect = CreateRect(parent, name, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static TMP_Text CreateText(Transform parent, string name, string text, float fontSize, Vector2 position, Vector2 size)
        {
            RectTransform rect = CreateRect(parent, name, position, size);

            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            return label;
        }

        // 子を上から縦に並べる入れ物。子の数に合わせて高さが伸びる
        public static Transform CreateVerticalList(Transform parent, string name, Vector2 position)
        {
            RectTransform rect = CreateRect(parent, name, position, new Vector2(500, 0));
            rect.pivot = new Vector2(0.5f, 1f);

            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            rect.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rect;
        }

        public static Button CreateButton(Transform parent, string name, string labelText, Vector2 position = default, Vector2? size = null)
        {
            Image image = CreateImage(parent, name, Color.white, position, size ?? new Vector2(500, 80));
            var button = image.gameObject.AddComponent<Button>();
            ApplySelectableColors(button);

            TMP_Text label = CreateText(button.transform, "Label", labelText, 40, Vector2.zero, Vector2.zero);
            Stretch(label.rectTransform);

            return button;
        }

        public static Slider CreateSlider(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject go = DefaultControls.CreateSlider(BuiltinResources());
            go.name = name;
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var slider = go.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            ApplySelectableColors(slider);
            return slider;
        }

        public static Toggle CreateToggle(Transform parent, string name, Vector2 position, float size)
        {
            GameObject go = DefaultControls.CreateToggle(BuiltinResources());
            go.name = name;
            go.transform.SetParent(parent, false);

            // 標準のラベルは古い Text なので消し、チェックボックスだけにする
            Object.DestroyImmediate(go.transform.Find("Label").gameObject);

            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);

            Transform background = go.transform.Find("Background");
            Stretch((RectTransform)background);
            Transform checkmark = background.Find("Checkmark");
            Stretch((RectTransform)checkmark);
            checkmark.GetComponent<Image>().color = Color.white;   // 暗い背景でも見えるように

            var toggle = go.GetComponent<Toggle>();
            ApplySelectableColors(toggle);
            return toggle;
        }

        /// <summary>
        /// 上下キーで、並べた順に選択が移るようにする（それ以外の部品には移らない）
        /// </summary>
        public static void SetVerticalNavigation(IReadOnlyList<Selectable> order)
        {
            for (int i = 0; i < order.Count; i++)
            {
                Navigation navigation = order[i].navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnUp = i > 0 ? order[i - 1] : null;
                navigation.selectOnDown = i < order.Count - 1 ? order[i + 1] : null;
                order[i].navigation = navigation;
            }
        }

        /// <summary>
        /// [SerializeField] の private な欄に値を入れる（インスペクタでドラッグするのと同じ）
        /// </summary>
        public static void SetReferences(Object target, params (string field, Object value)[] references)
        {
            var serialized = new SerializedObject(target);
            foreach ((string field, Object value) in references)
            {
                serialized.FindProperty(field).objectReferenceValue = value;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>親いっぱいに広げる</summary>
        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        /// <summary>Assets/A/B のようなフォルダがなければ、順に作る</summary>
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }

        // キーボードで選んでいる部品が分かるよう、選択中はオレンジにする
        static void ApplySelectableColors(Selectable selectable)
        {
            ColorBlock colors = selectable.colors;
            colors.normalColor = new Color(0.25f, 0.25f, 0.32f);
            colors.highlightedColor = new Color(0.4f, 0.4f, 0.5f);
            colors.selectedColor = new Color(0.9f, 0.55f, 0.15f);
            colors.pressedColor = new Color(1f, 0.75f, 0.35f);
            colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            selectable.colors = colors;
        }

        static DefaultControls.Resources BuiltinResources() => new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
        };
    }
}
