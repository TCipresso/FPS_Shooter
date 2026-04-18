using UnityEngine;
using System.Collections.Generic;

public class AugmentDraftUI : MonoBehaviour
{
    [Header("Setup")]
    public GameObject player;
    public MenuUIHelper menuHelper;

    [Header("Upgrade Draft (Level Ups)")]
    public List<UpgradeData> upgradePool;
    public UpgradeCardUI upgradeCardPrefab;
    public Transform cardParent;
    public CanvasGroup panelGroup;

    [Header("Augment Draft")]
    public List<AugmentData> augmentPool;
    public AugmentCardUI augmentCardPrefab;

    [Header("Visuals")]
    public float fadeSpeed = 8f;
    public float spawnScaleTime = 0.25f;
    public bool overshootBounce = true;

    [Header("Draft")]
    public int cardsToShow = 3;

    readonly List<Component> spawned = new();
    bool fadingIn, fadingOut, isOpen;
    Coroutine scaleRoutine;
    float previousTimeScale = 1f;

    void Awake()
    {
        if (panelGroup)
        {
            panelGroup.alpha = 0f;
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying && panelGroup)
        {
            panelGroup.alpha = 0f;
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;
        }
    }
#endif

    // ============== UPGRADE DRAFT ==============
    public void OpenUpgradeDraft()
    {
        if (!panelGroup || !upgradeCardPrefab || !cardParent || upgradePool == null || upgradePool.Count == 0 || !player)
            return;
        if (isOpen) return;

        isOpen = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        fadingOut = false;
        fadingIn = true;
        panelGroup.interactable = true;
        panelGroup.blocksRaycasts = true;

        ClearCards();

        var picks = PickUniqueUpgrades(upgradePool, cardsToShow);
        for (int i = 0; i < picks.Count; i++)
        {
            var data = picks[i];
            var card = Instantiate(upgradeCardPrefab, cardParent);
            card.transform.localScale = Vector3.zero;
            card.transform.SetSiblingIndex(i);

            float luck = player.GetComponent<PlayerStats>()?.luck ?? 0f;
            var rarity = UpgradeRarityHelper.RollRarity(luck);
            var color = UpgradeRarityHelper.GetColor(rarity);

            card.Setup(data, rarity, color, OnUpgradeChosen);
            spawned.Add(card);
        }

        if (scaleRoutine != null) StopCoroutine(scaleRoutine);
        scaleRoutine = StartCoroutine(ScaleAllCards());
    }

    void OnUpgradeChosen(UpgradeData data, UpgradeRarity rarity, float rolledValue)
    {
        if (data == null) return;
        data.effect.ApplyUpgrade(player, rolledValue);
        CloseDraft();
    }

    // ============== AUGMENT DRAFT (ON LEVEL UP) ==============
    public void OpenAugmentDraft()
    {
        Debug.Log($"[AugmentDraftUI] OpenAugmentDraft called. panel={panelGroup}, cardPrefab={augmentCardPrefab}, cardParent={cardParent}, poolCount={augmentPool?.Count}, player={player}, isOpen={isOpen}");
        if (!panelGroup || !augmentCardPrefab || !cardParent || augmentPool == null || augmentPool.Count == 0 || !player)
            return;
        if (isOpen) return;

        isOpen = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        if (menuHelper != null) menuHelper.EnterDraftState();

        fadingOut = false;
        fadingIn = true;
        panelGroup.interactable = true;
        panelGroup.blocksRaycasts = true;

        ClearCards();

        var picks = PickUniqueAugments(augmentPool, cardsToShow);
        for (int i = 0; i < picks.Count; i++)
        {
            var data = picks[i];
            var card = Instantiate(augmentCardPrefab, cardParent);
            card.transform.localScale = Vector3.zero;
            card.transform.SetSiblingIndex(i);
            card.Setup(data, OnAugmentChosen);
            spawned.Add(card);
        }

        if (scaleRoutine != null) StopCoroutine(scaleRoutine);
        scaleRoutine = StartCoroutine(ScaleAllCards());
    }

    void OnAugmentChosen(AugmentData data)
    {
        data?.Apply(player);
        CloseDraft();
    }

    // ============== PANEL / FX ==============
    public void CloseDraft()
    {
        if (!isOpen) return;
        isOpen = false;

        Time.timeScale = previousTimeScale;
        if (menuHelper != null) menuHelper.ExitDraftState();

        fadingIn = false;
        fadingOut = true;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;

        if (scaleRoutine != null)
        {
            StopCoroutine(scaleRoutine);
            scaleRoutine = null;
        }
    }

    void ClearCards()
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            var c = spawned[i] as Component;
            if (c) Destroy(c.gameObject);
        }
        spawned.Clear();
    }

    List<UpgradeData> PickUniqueUpgrades(List<UpgradeData> src, int count)
    {
        var result = new List<UpgradeData>(count);
        var temp = new List<UpgradeData>(src);
        for (int i = 0; i < count && temp.Count > 0; i++)
        {
            int idx = Random.Range(0, temp.Count);
            result.Add(temp[idx]);
            temp.RemoveAt(idx);
        }
        return result;
    }

    List<AugmentData> PickUniqueAugments(List<AugmentData> src, int count)
    {
        var result = new List<AugmentData>(count);
        var temp = new List<AugmentData>(src);
        for (int i = 0; i < count && temp.Count > 0; i++)
        {
            int idx = Random.Range(0, temp.Count);
            result.Add(temp[idx]);
            temp.RemoveAt(idx);
        }
        return result;
    }

    void Update()
    {
        if (!panelGroup) return;

        if (fadingIn)
        {
            panelGroup.alpha = Mathf.Lerp(panelGroup.alpha, 1f, fadeSpeed * Time.unscaledDeltaTime);
            if (Mathf.Abs(panelGroup.alpha - 1f) < 0.01f)
            {
                panelGroup.alpha = 1f;
                fadingIn = false;
            }
        }
        else if (fadingOut)
        {
            panelGroup.alpha = Mathf.Lerp(panelGroup.alpha, 0f, fadeSpeed * Time.unscaledDeltaTime);
            if (panelGroup.alpha < 0.01f)
            {
                panelGroup.alpha = 0f;
                fadingOut = false;
                ClearCards();
            }
        }
    }

    System.Collections.IEnumerator ScaleAllCards()
    {
        float t = 0f;
        float overshoot = overshootBounce ? 1.06f : 1f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / spawnScaleTime;
            float s = Mathf.SmoothStep(0f, overshoot, t);
            for (int i = 0; i < spawned.Count; i++)
            {
                var c = spawned[i] as Component;
                if (c) c.transform.localScale = Vector3.one * s;
            }
            yield return null;
        }

        if (overshootBounce)
        {
            float t2 = 0f;
            while (t2 < 1f)
            {
                t2 += Time.unscaledDeltaTime / (spawnScaleTime * 0.6f);
                float s = Mathf.SmoothStep(overshoot, 1f, t2);
                for (int i = 0; i < spawned.Count; i++)
                {
                    var c = spawned[i] as Component;
                    if (c) c.transform.localScale = Vector3.one * s;
                }
                yield return null;
            }
        }

        for (int i = 0; i < spawned.Count; i++)
        {
            var c = spawned[i] as Component;
            if (c) c.transform.localScale = Vector3.one;
        }
    }
}