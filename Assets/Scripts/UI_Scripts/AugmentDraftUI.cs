using UnityEngine;
using System.Collections.Generic;

public class AugmentDraftUI : MonoBehaviour
{
    [Header("Setup")]
    public GameObject player;
    public MenuUIHelper menuHelper;
    public WeaponInventory weaponInventory;

    [Header("Upgrade Draft (Level Ups)")]
    public List<UpgradeData> upgradePool;
    public List<WeaponUpgradeData> weaponUpgradePool;
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

    // Wrapper to unify upgrade and weapon upgrade picks
    struct DraftEntry
    {
        public UpgradeData upgradeData;
        public WeaponUpgradeData weaponUpgradeData;
        public int weaponSlot;
        public bool IsWeaponUpgrade => weaponUpgradeData != null;
    }

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
        if (!panelGroup || !upgradeCardPrefab || !cardParent || !player) return;
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

        var picks = PickUnifiedDraft(cardsToShow);
        for (int i = 0; i < picks.Count; i++)
        {
            var entry = picks[i];
            var card = Instantiate(upgradeCardPrefab, cardParent);
            card.transform.localScale = Vector3.zero;
            card.transform.SetSiblingIndex(i);

            if (entry.IsWeaponUpgrade)
            {
                int slot = entry.weaponSlot;
                int currentLevel = weaponInventory.GetWeaponLevel(slot);
                int nextLevel = currentLevel + 1;
                var upgradeData = entry.weaponUpgradeData;

                UpgradeRarity rarity = WeaponUpgradeData.GetRarityForLevel(nextLevel);
                Color color = WeaponUpgradeData.GetColorForLevel(nextLevel);

                // Get the next WeaponData from the upgrade path
                WeaponData nextWeaponData = GetWeaponDataForUpgrade(upgradeData, nextLevel);

                card.Setup(
                    upgradeData.weaponName,
                    $"Upgrade to level {nextLevel}",
                    upgradeData.icon,
                    rarity,
                    color,
                    () =>
                    {
                        if (nextWeaponData != null)
                            weaponInventory.UpgradeWeaponInSlot(slot, nextWeaponData);
                        CloseDraft();
                    }
                );
            }
            else
            {
                float luck = player.GetComponent<PlayerStats>()?.luck ?? 0f;
                var rarity = UpgradeRarityHelper.RollRarity(luck);
                var color = UpgradeRarityHelper.GetColor(rarity);
                var data = entry.upgradeData;

                card.Setup(data, rarity, color, OnUpgradeChosen);
            }

            spawned.Add(card);
        }

        if (scaleRoutine != null) StopCoroutine(scaleRoutine);
        scaleRoutine = StartCoroutine(ScaleAllCards());
    }

    // Builds combined pool of stat upgrades + eligible weapon upgrades and picks randomly
    List<DraftEntry> PickUnifiedDraft(int count)
    {
        var pool = new List<DraftEntry>();

        // Add stat upgrades
        if (upgradePool != null)
        {
            foreach (var u in upgradePool)
            {
                if (u != null)
                    pool.Add(new DraftEntry { upgradeData = u });
            }
        }

        // Add eligible weapon upgrades (below level 10, has upgrade data assigned)
        if (weaponInventory != null && weaponUpgradePool != null)
        {
            for (int slot = 0; slot < weaponInventory.equippedWeapons.Count; slot++)
            {
                WeaponUpgradeData upgradeData = weaponInventory.GetWeaponUpgradeData(slot);
                if (upgradeData == null) continue;

                // Check this weapon's upgrade data is in the pool
                if (!weaponUpgradePool.Contains(upgradeData)) continue;

                int level = weaponInventory.GetWeaponLevel(slot);
                if (level >= 10) continue;

                pool.Add(new DraftEntry
                {
                    weaponUpgradeData = upgradeData,
                    weaponSlot = slot
                });
            }
        }

        // Pick randomly
        var result = new List<DraftEntry>(count);
        var temp = new List<DraftEntry>(pool);
        for (int i = 0; i < count && temp.Count > 0; i++)
        {
            int idx = Random.Range(0, temp.Count);
            result.Add(temp[idx]);
            temp.RemoveAt(idx);
        }
        return result;
    }

    // Maps WeaponUpgradeData + level to a WeaponData so inventory can instantiate it
    WeaponData GetWeaponDataForUpgrade(WeaponUpgradeData upgradeData, int level)
    {
        GameObject prefab = upgradeData.GetPrefabForLevel(level);
        if (prefab == null)
        {
            Debug.LogWarning($"[AugmentDraftUI] No prefab for {upgradeData.weaponName} level {level}");
            return null;
        }

        // Build a transient WeaponData from the prefab
        WeaponData data = ScriptableObject.CreateInstance<WeaponData>();
        data.prefab = prefab;
        data.weaponName = $"{upgradeData.weaponName} Lv{level}";
        return data;
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