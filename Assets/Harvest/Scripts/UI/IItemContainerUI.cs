using UnityEngine;

public interface IItemContainerUI
{
    void Click(ItemUI itemUI, Vector2 pos);

    void PreviewHover(ItemUI itemUI, Vector2 pos);

    void DisablePreview();
}
