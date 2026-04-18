using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class AugmentCardUI : MonoBehaviour
{
    [Header("UI")]
    public Image icon;
    public TMP_Text titleText;
    public TMP_Text descText;
    public Button pickButton;

    AugmentData data;
    Action<AugmentData> onPicked;

    public void Setup(AugmentData augment, Action<AugmentData> onPickedCallback)
    {
        data = augment;
        onPicked = onPickedCallback;

        if (titleText) titleText.text = augment.augmentName;
        if (descText) descText.text = augment.description;
        if (icon) icon.sprite = augment.icon;

        if (pickButton)
        {
            pickButton.onClick.RemoveAllListeners();
            pickButton.onClick.AddListener(() => onPicked?.Invoke(data));
        }
    }
}
