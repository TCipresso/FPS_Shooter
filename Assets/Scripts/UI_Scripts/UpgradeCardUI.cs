using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UpgradeCardUI : MonoBehaviour
{
    [Header("UI")]
    public Image icon;
    public TMP_Text titleText;
    public TMP_Text descText;
    public Button pickButton;

    [Header("Rarity Visuals")]
    public Image frameImage; // optional border/background to tint by rarity
    public TMP_Text rarityText; // optional label to show "Common/Rare/Epic/Extraterrestrial"

    UpgradeData data;
    UpgradeRarity rarity;
    Action<UpgradeData, UpgradeRarity> onPicked;

    public void Setup(UpgradeData upgrade, UpgradeRarity rolledRarity, Color rarityColor, Action<UpgradeData, UpgradeRarity, float> onPickedCallback)
    {
        data = upgrade;
        rarity = rolledRarity;

        // Roll the value once and store it
        float rolledValue = data.GetRandomValueForRarity(rarity);
        float displayPercent = rolledValue * 100f;

        if (titleText) titleText.text = upgrade.upgradeName;
        if (descText) descText.text = upgrade.description.Replace("{value}", displayPercent.ToString("F1"));
        if (icon && upgrade.icon) icon.sprite = upgrade.icon;

        if (frameImage) frameImage.color = rarityColor;
        if (rarityText) rarityText.text = rolledRarity.ToString();

        if (pickButton)
        {
            pickButton.onClick.RemoveAllListeners();
            pickButton.onClick.AddListener(() => onPickedCallback?.Invoke(data, rarity, rolledValue));
        }
    }

}
