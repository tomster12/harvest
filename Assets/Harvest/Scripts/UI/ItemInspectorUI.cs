using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

public class ItemInspectorUI : MonoBehaviour
{
    public void Init()
    {
        SetVisible(false);
        RenderContainer();
        RenderInspectorPanel();
    }

    public void ShowInspector(ItemInstance item)
    {
        SetVisible(true);
        RenderItem(item);
        FollowCursor();
    }

    public void HideInspector()
    {
        SetVisible(false);
    }

    [Header("Assets")]
    [SerializeField] private TMP_FontAsset titleFont;
    [SerializeField] private TMP_FontAsset bodyFont;
    [SerializeField] private Sprite dividerSprite;

    [Header("Config")]
    [SerializeField] private float followMargin = 20f;
    [SerializeField] private float fixedWidth = 300f;
    [SerializeField] private LayoutConfig inspectorLayoutConfig = new(12, 10, 10, 10, 0f);
    [SerializeField] private LayoutConfig contentLayoutConfig = new(0, 0, 0, 0, 8f);
    [SerializeField] private LayoutConfig partLayoutConfig = new(8, 8, 4, 4, 2f);
    [SerializeField] private LayoutConfig affixLayoutConfig = new(10, 10, 10, 10, 6f);

    [SerializeField] private int fontSizeH1 = 28;
    [SerializeField] private int fontSizeH2 = 20;
    [SerializeField] private int fontSizeText = 14;

    [SerializeField] private Color colCommon = new(0.70f, 0.70f, 0.70f, 1f);
    [SerializeField] private Color colMagic = new(0.40f, 0.55f, 1.00f, 1f);
    [SerializeField] private Color colRare = new(1.00f, 0.85f, 0.20f, 1f);
    [SerializeField] private Color colUnique = new(0.85f, 0.45f, 0.10f, 1f);
    [SerializeField] private Color colBg = new(0.10f, 0.10f, 0.12f, 0.97f);
    [SerializeField] private Color colSectionBg = new(0.14f, 0.14f, 0.17f, 1.00f);
    [SerializeField] private Color colTextPrimary = new(0.95f, 0.93f, 0.88f, 1.00f);
    [SerializeField] private Color colTextSecondary = new(0.60f, 0.60f, 0.60f, 1.00f);
    [SerializeField] private Color colTextBaseStat = new(0.85f, 0.85f, 0.85f, 1.00f);
    [SerializeField] private Color colTextAffix = new(0.75f, 0.88f, 1.00f, 1.00f);
    [SerializeField] private Color colTextPartAffix = new(0.65f, 0.80f, 0.70f, 1.00f);
    [SerializeField] private Color colDividerColor = new(0.30f, 0.30f, 0.35f, 1.00f);

    private bool isVisible;
    private RectTransform panelRect;
    private VerticalLayoutGroup contentLayout;
    private RectTransform contentRect;
    private Image borderImage;

    private RectTransform Rect => (RectTransform)transform;

    private void Update()
    {
        if (isVisible) FollowCursor();
    }

    private void FollowCursor()
    {

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            Rect, PlayerInput.PointerPosition, null, out Vector2 mouseLocalPos);

        // Offset so the panel up-right of the cursor
        Vector2 target = new(
            mouseLocalPos.x + followMargin,
            mouseLocalPos.y + followMargin
        );

        // Clamp so it never escapes the canvas
        float xMin = Rect.rect.xMin + followMargin;
        float xMax = Rect.rect.xMax - panelRect.rect.width - followMargin;
        float yMin = Rect.rect.yMin + panelRect.rect.height - followMargin;
        float yMax = Rect.rect.yMax - followMargin;

        target.x = Mathf.Max(Mathf.Min(target.x, xMax), xMin);
        target.y = Mathf.Max(Mathf.Min(target.y, yMax), yMin);

        panelRect.anchoredPosition = target;
    }

    private void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
        isVisible = visible;
    }

    private void RenderContainer()
    {
        Rect.pivot = new(0f, 0f);
        Rect.anchorMin = new Vector2(0f, 0f);
        Rect.anchorMax = new Vector2(1f, 1f);
    }

    private void RenderInspectorPanel()
    {
        // Panel container
        var panelGO = new GameObject("Inspector Panel");
        panelGO.transform.SetParent(Rect, false);
        panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0f, 0f);
        panelRect.sizeDelta = new Vector2(fixedWidth, 0f);

        Image panelImage = panelGO.AddComponent<Image>();
        panelImage.color = colBg;

        var panelLayout = panelGO.AddComponent<VerticalLayoutGroup>();
        inspectorLayoutConfig.Set(panelLayout);
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = true;
        panelLayout.childForceExpandHeight = false;
        panelLayout.childForceExpandWidth = true;

        // Coloured left border strip
        var borderGO = new GameObject("Border");
        borderGO.transform.SetParent(panelRect, false);
        var borderRect = borderGO.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = new Vector2(0f, 1f);
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = new Vector2(4f, 0f);

        borderImage = borderGO.AddComponent<Image>();
        borderImage.color = colCommon;

        var borderLE = borderGO.AddComponent<LayoutElement>();
        borderLE.ignoreLayout = true;

        // Content container with vertical layout
        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(panelRect, false);
        contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);

        contentLayout = contentGO.AddComponent<VerticalLayoutGroup>();
        contentLayoutConfig.Set(contentLayout);
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.padding = new RectOffset(0, 0, 0, 0);

        // Make the panel height hug content
        var panelFitter = panelGO.AddComponent<ContentSizeFitter>();
        panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var contentFitter = contentGO.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var contentLE = contentGO.AddComponent<LayoutElement>();
        contentLE.flexibleWidth = 1f;
    }

    private void RenderItem(ItemInstance item)
    {
        // Clear previous content
        foreach (Transform child in contentRect)
        {
            Destroy(child.gameObject);
        }

        if (item == null)
        {
            HideInspector();
            return;
        }

        switch (item.Data.Type)
        {
            case ItemType.Resource:
                RenderItemResource(item);
                break;

            case ItemType.Equipment:
            case ItemType.Tool:
                RenderItemComplex(item);
                break;

            case ItemType.Part:
                RenderItemPart(item);
                break;
        }
    }

    private void RenderItemResource(ItemInstance item)
    {
        borderImage.color = colCommon;

        RenderText(contentRect, item.Data.DisplayName, fontSizeH1, FontStyles.Bold, colTextPrimary);
        RenderText(contentRect, $"x{item.Amount}", fontSizeH2, FontStyles.Normal, colTextSecondary);

        if (!string.IsNullOrEmpty(item.Data.Description))
        {
            RenderText(contentRect, item.Data.Description, fontSizeText, FontStyles.Italic, colTextSecondary);
        }
    }

    private void RenderItemComplex(ItemInstance item)
    {
        borderImage.color = RarityColour(item);

        RenderText(contentRect, item.Data.DisplayName, fontSizeH1, FontStyles.Bold, colTextPrimary);

        string typeLabel = item.Data.Type == ItemType.Tool
            ? item.Data.ToolType.ToString()
            : item.Data.EquipmentType.ToString();
        RenderText(contentRect, $"Level {item.Level}  ·  {typeLabel}", fontSizeText, FontStyles.Normal, colTextSecondary);

        RenderDivider();
        RenderBaseStatsList(item.Data.BaseStats);

        RenderDivider();
        RenderItemAffixesList(item.Affixes);

        if (item.PartSlots.Count > 0)
        {
            RenderDivider();

            foreach (var slot in item.PartSlots)
            {
                RenderItemPartSection(slot);
            }
        }
    }

    private void RenderItemPart(ItemInstance item)
    {
        borderImage.color = RarityColour(item);

        RenderText(contentRect, item.Data.DisplayName, fontSizeH1, FontStyles.Bold, colTextPrimary);
        string typeLabel = item.Data.Type == ItemType.Tool
            ? item.Data.ToolType.ToString()
            : item.Data.EquipmentType.ToString();
        RenderText(contentRect, $"Level {item.Level}  ·  {typeLabel}", fontSizeText, FontStyles.Normal, colTextSecondary);

        RenderDivider();
        RenderItemAffixesList(item.Affixes);
    }

    private void RenderItemPartSection(PartSlotInstance slot)
    {
        // Section background box
        var sectionGO = new GameObject($"Part Section Layout ({slot.RequiredType})");
        sectionGO.transform.SetParent(contentRect, false);

        var sectionImage = sectionGO.AddComponent<Image>();
        sectionImage.color = colSectionBg;

        var sectionLayout = sectionGO.AddComponent<VerticalLayoutGroup>();
        partLayoutConfig.Set(sectionLayout);
        sectionLayout.childForceExpandWidth = true;
        sectionLayout.childForceExpandHeight = false;
        sectionLayout.childControlWidth = true;
        sectionLayout.childControlHeight = true;

        var sectionFitter = sectionGO.AddComponent<ContentSizeFitter>();
        sectionFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sectionLE = sectionGO.AddComponent<LayoutElement>();
        sectionLE.flexibleWidth = 1f;

        if (slot.Item != null)
        {
            RenderText(sectionGO.transform, slot.Item.Data.DisplayName, fontSizeH2, FontStyles.Bold, colTextSecondary);

            if (slot.Item.Data.Type == ItemType.Part || slot.Item.Data.Type == ItemType.Equipment || slot.Item.Data.Type == ItemType.Tool)
            {
                RenderItemAffixesList(slot.Item.Affixes, sectionGO.transform, colTextPartAffix);
            }
        }
        else
        {
            string slotName = slot.RequiredType.ToString();

            RenderText(sectionGO.transform, $"[Empty – {slotName}]", fontSizeText, FontStyles.Italic,
                new Color(colTextSecondary.r, colTextSecondary.g, colTextSecondary.b, 0.5f));
        }
    }

    private void RenderDivider()
    {
        var dividerGO = new GameObject("Divider");
        dividerGO.transform.SetParent(contentRect, false);
        Image img = dividerGO.AddComponent<Image>();
        img.color = colDividerColor;
        if (dividerSprite != null) img.sprite = dividerSprite;

        var dividerLE = dividerGO.AddComponent<LayoutElement>();
        dividerLE.preferredHeight = 1f;
        dividerLE.flexibleWidth = 1f;

        var dividerRect = dividerGO.GetComponent<RectTransform>();
        dividerRect.sizeDelta = new Vector2(0f, 1f);
    }

    private void RenderBaseStatsList(IEnumerable<BaseStat> baseStats, Transform parent = null, Color? color = null)
    {
        if (parent == null) parent = contentRect;
        if (color == null) color = colTextBaseStat;

        // Section background box
        var listGO = new GameObject($"Base Stats Layout");
        listGO.transform.SetParent(parent, false);

        var listLayout = listGO.AddComponent<VerticalLayoutGroup>();
        affixLayoutConfig.Set(listLayout);
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = true;

        var sectionFitter = listGO.AddComponent<ContentSizeFitter>();
        sectionFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var listLE = listGO.AddComponent<LayoutElement>();
        listLE.flexibleWidth = 1f;

        if (baseStats.Count() > 0)
        {
            foreach (var baseStat in baseStats)
            {
                RenderBaseStatListItem(listGO.transform, baseStat, color.Value);
            }
        }
        else
        {
            RenderText(listGO.transform, "No affixes", fontSizeText, FontStyles.Italic, new Color(color.Value.r, color.Value.g, color.Value.b, 0.6f));
        }
    }

    private void RenderBaseStatListItem(Transform parent, BaseStat baseStat, Color color)
    {
        string valueStr = $"{baseStat.Value:F1}";
        string line = $"{baseStat.Stat.ToDisplayString()}  {valueStr}";

        RenderText(parent, line, fontSizeText, FontStyles.Normal, color);
    }

    private void RenderItemAffixesList(IEnumerable<AffixInstance> affixes, Transform parent = null, Color? color = null)
    {
        if (parent == null) parent = contentRect;
        if (color == null) color = colTextAffix;

        // Section background box
        var listGO = new GameObject($"Affix List Layout");
        listGO.transform.SetParent(parent, false);

        var listLayout = listGO.AddComponent<VerticalLayoutGroup>();
        affixLayoutConfig.Set(listLayout);
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = true;

        var sectionFitter = listGO.AddComponent<ContentSizeFitter>();
        sectionFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var listLE = listGO.AddComponent<LayoutElement>();
        listLE.flexibleWidth = 1f;

        if (affixes.Count() > 0)
        {
            foreach (var affix in affixes)
            {
                RenderAffixListItem(listGO.transform, affix, color.Value);
            }
        }
        else
        {
            RenderText(listGO.transform, "No affixes", fontSizeText, FontStyles.Italic, new Color(color.Value.r, color.Value.g, color.Value.b, 0.6f));
        }
    }

    private void RenderAffixListItem(Transform parent, AffixInstance affix, Color color)
    {
        string sign = affix.RolledValue >= 0 ? "+" : "";
        string valueStr = affix.Data.ValueType == AffixValueType.Multiplicative
            ? $"{sign}{affix.RolledValue * 100f:F0}%"
            : $"{sign}{affix.RolledValue:F1}";
        string line = $"• {affix.Data.DisplayName}  {valueStr}";

        RenderText(parent, line, fontSizeText, FontStyles.Normal, color);
    }

    private void RenderText(Transform parent, string text, int size, FontStyles style, Color color)
    {
        GameObject labelGO = new("Label");
        labelGO.transform.SetParent(parent, false);

        var labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.text = text;
        labelText.fontSize = size;
        labelText.fontStyle = style;
        labelText.color = color;
        labelText.textWrappingMode = TextWrappingModes.Normal;
        labelText.overflowMode = TextOverflowModes.Overflow;

        if (titleFont != null && style == FontStyles.Bold) labelText.font = titleFont;
        else if (bodyFont != null) labelText.font = bodyFont;

        var labelFitter = labelGO.AddComponent<ContentSizeFitter>();
        labelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var labelLE = labelGO.AddComponent<LayoutElement>();
        labelLE.flexibleWidth = 1f;
    }

    private Color RarityColour(ItemInstance item)
    {
        return item.Rarity switch
        {
            ItemRarity.Magic => colMagic,
            ItemRarity.Rare => colRare,
            ItemRarity.Unique => colUnique,
            _ => colCommon,
        };
    }
}

[Serializable]
public class LayoutConfig
{
    public int Left, Right, Top, Bottom;
    public float Spacing;

    public LayoutConfig(int Left, int Right, int Top, int Bottom, float Spacing)
    {
        this.Left = Left;
        this.Right = Right;
        this.Top = Top;
        this.Bottom = Bottom;
        this.Spacing = Spacing;
    }

    public void Set(HorizontalOrVerticalLayoutGroup layout)
    {
        layout.padding = new(Left, Right, Top, Bottom);
        layout.spacing = Spacing;
    }
}
