
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpriteCountDisplay : MonoBehaviour
{
    [Header("表示設定")]
    [SerializeField] private Sprite iconSprite;
    [SerializeField] private int maxCount = 3;
    [SerializeField] private Vector2 iconSize = new Vector2(48f, 48f);
    [SerializeField] private float spacing = 8f;
    [SerializeField] private Color placeholderColor = Color.cyan;

    [Header("アイコンの親")]
    [SerializeField] private RectTransform iconParent;

    private readonly List<Image> icons = new List<Image>();

    private int currentCount;

    private Sprite runtimePlaceholderSprite;
    private Texture2D runtimePlaceholderTexture;

    private void Awake()
    {
        CreateIcons();
        SetCount(maxCount);
    }

    private void CreateIcons()
    {
        if (iconParent == null)
        {
            Debug.LogWarning($"{name}: Icon Parent が未設定です。");
            return;
        }

        Sprite spriteToUse = iconSprite;

        // 画像が未設定なら、仮の白い四角形を生成
        if (spriteToUse == null)
        {
            runtimePlaceholderTexture = new Texture2D(1, 1);
            runtimePlaceholderTexture.SetPixel(0, 0, Color.white);
            runtimePlaceholderTexture.Apply();

            runtimePlaceholderSprite = Sprite.Create(
                runtimePlaceholderTexture,
                new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f)
            );

            spriteToUse = runtimePlaceholderSprite;
        }

        icons.Clear();

        for (int i = 0; i < maxCount; i++)
        {
            GameObject iconObject = new GameObject(
                $"Icon_{i + 1}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

            iconObject.transform.SetParent(iconParent, false);

            Image image = iconObject.GetComponent<Image>();
            image.sprite = spriteToUse;
            image.preserveAspect = true;

            // 仮画像には指定色、本物の画像には白を適用
            image.color = iconSprite == null
                ? placeholderColor
                : Color.white;

            RectTransform rect = iconObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = iconSize;
            rect.anchoredPosition = new Vector2(
                i * (iconSize.x + spacing),
                0f
            );

            icons.Add(image);
        }
    }

    public void SetCount(int count)
    {
        currentCount = Mathf.Clamp(count, 0, maxCount);

        int hideenCount = maxCount - currentCount;

        for (int i = 0; i < icons.Count; i++)
        {
            icons[i].enabled = i >= hideenCount;
        }
    }

    public void DecreaseCount()
    {
        SetCount(currentCount - 1);
    }

    public void ResetCount()
    {
        SetCount(maxCount);
    }

    public int GetCount()
    {
        return currentCount;
    }

    private void OnDestroy()
    {
        if (runtimePlaceholderSprite != null)
        {
            Destroy(runtimePlaceholderSprite);
        }

        if (runtimePlaceholderTexture != null)
        {
            Destroy(runtimePlaceholderTexture);
        }
    }
}
