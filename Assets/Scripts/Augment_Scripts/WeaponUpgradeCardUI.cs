using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
public class WeaponUpgradeCardUI : MonoBehaviour
{
    [Header("UI")]
    public Image icon;
    public Image rarityBorder;
    public TMP_Text titleText;
    public TMP_Text descText;
    public Button pickButton;
    Action onPicked;
    public void Setup(string title, string description, Sprite iconSprite, Color rarityColor, Action onPickedCallback)
    {
        onPicked = onPickedCallback;
        if (titleText) titleText.text = title;
        if (descText) descText.text = description;
        if (icon) icon.sprite = iconSprite;
        if (rarityBorder) rarityBorder.color = rarityColor;
        if (pickButton)
        {
            pickButton.onClick.RemoveAllListeners();
            pickButton.onClick.AddListener(() => onPicked?.Invoke());
        }
    }
}
