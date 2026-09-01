using UnityEngine;
using System.Collections.Generic;
using System;

public class WeaponLevelUpDraftUI : MonoBehaviour
{
    public static WeaponLevelUpDraftUI Instance { get; private set; }

    [Header("Setup")]
    public CanvasGroup panelGroup;
    public Transform cardParent;
    public WeaponUpgradeCardUI cardPrefab;
    public MenuUIHelper menuHelper;

    [Header("Evolution Visual")]
    public Color evolutionCardColor = new Color(1f, 0.75f, 0.1f);

    [Header("Draft")]
    public int cardsToShow = 3;
    public bool evolutionsEnabled = false;

    [Header("Visuals")]
    public float fadeSpeed = 8f;
    public float spawnScaleTime = 0.25f;
    public bool overshootBounce = true;

    struct DraftCardEntry
    {
        public string title;
        public string description;
        public Sprite icon;
        public Color color;
        public Action onPicked;
    }

    readonly List<WeaponUpgradeCardUI> spawned = new List<WeaponUpgradeCardUI>();
    bool fadingIn, fadingOut, isOpen;
    Coroutine scaleRoutine;
    float previousTimeScale = 1f;
    WeaponBase currentWeapon;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (panelGroup)
        {
            panelGroup.alpha = 0f;
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Show(WeaponBase weapon)
    {
        if (!panelGroup || !cardPrefab || !cardParent || weapon == null || weapon.weaponDefinition == null) return;
        if (isOpen) return;

        currentWeapon = weapon;
        WeaponDefinitionSO def = weapon.weaponDefinition;

        if (def.upgradePool == null || def.upgradePool.Count == 0)
        {
            Debug.LogWarning($"[WeaponLevelUpDraftUI] No upgrade pool defined for {def.weaponName}");
            return;
        }

        List<DraftCardEntry> picks = evolutionsEnabled && def.IsEvolutionLevel(def.level)
            ? BuildEvolutionDraft(weapon)
            : BuildStatDraft(weapon);

        if (picks == null || picks.Count == 0) return;

        isOpen = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        if (menuHelper != null) menuHelper.EnterDraftState();

        fadingOut = false;
        fadingIn = true;
        panelGroup.interactable = true;
        panelGroup.blocksRaycasts = true;

        ClearCards();

        for (int i = 0; i < picks.Count; i++)
        {
            DraftCardEntry entry = picks[i];
            WeaponUpgradeCardUI card = Instantiate(cardPrefab, cardParent);
            card.transform.localScale = Vector3.zero;
            card.transform.SetSiblingIndex(i);
            card.Setup(entry.title, entry.description, entry.icon, entry.color, () =>
            {
                entry.onPicked?.Invoke();
                CloseDraft();
            });
            spawned.Add(card);
        }

        if (scaleRoutine != null) StopCoroutine(scaleRoutine);
        scaleRoutine = StartCoroutine(ScaleAllCards());
    }

    List<DraftCardEntry> BuildStatDraft(WeaponBase weapon)
    {
        WeaponDefinitionSO def = weapon.weaponDefinition;
        List<WeaponStatUpgradeSO> availableUpgrades = new List<WeaponStatUpgradeSO>(def.upgradePool);

        if (availableUpgrades.Count == 0) return null;

        List<DraftCardEntry> result = new List<DraftCardEntry>(cardsToShow);
        float luck = PlayerStats.Instance != null ? PlayerStats.Instance.luck : 0f;

        // Shuffle the available upgrades
        for (int i = availableUpgrades.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            WeaponStatUpgradeSO temp = availableUpgrades[i];
            availableUpgrades[i] = availableUpgrades[j];
            availableUpgrades[j] = temp;
        }

        int count = Mathf.Min(cardsToShow, availableUpgrades.Count);
        for (int i = 0; i < count; i++)
        {
            WeaponStatUpgradeSO option = availableUpgrades[i];
            if (option == null) continue;

            UpgradeRarity rarity = UpgradeRarityHelper.RollRarity(luck);
            float value = option.GetRange(rarity).GetRandom();
            Color color = UpgradeRarityHelper.GetColor(rarity);

            // TITLE: Just the display name (no values)
            string title = option.displayName;

            // DESCRIPTION: The full description with the value
            string description = option.GetRolledDescription(value);

            result.Add(new DraftCardEntry
            {
                title = title,
                description = description,
                icon = option.icon,
                color = color,
                onPicked = () => option.Apply(def, value)
            });
        }

        return result;
    }

    List<DraftCardEntry> BuildEvolutionDraft(WeaponBase weapon)
    {
        WeaponDefinitionSO def = weapon.weaponDefinition;
        List<WeaponEvolutionSO> eligible = new List<WeaponEvolutionSO>();

        foreach (WeaponEvolutionSO evo in def.evolutionPool)
        {
            if (evo != null && !def.usedEvolutions.Contains(evo))
                eligible.Add(evo);
        }

        if (eligible.Count == 0)
            return BuildStatDraft(weapon);

        List<DraftCardEntry> result = new List<DraftCardEntry>(cardsToShow);

        for (int i = eligible.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            WeaponEvolutionSO temp = eligible[i];
            eligible[i] = eligible[j];
            eligible[j] = temp;
        }

        int count = Mathf.Min(cardsToShow, eligible.Count);
        for (int i = 0; i < count; i++)
        {
            WeaponEvolutionSO evo = eligible[i];
            result.Add(new DraftCardEntry
            {
                title = evo.displayName,
                description = evo.description,
                icon = evo.icon,
                color = evolutionCardColor,
                onPicked = () =>
                {
                    evo.Apply(weapon);
                    def.usedEvolutions.Add(evo);
                }
            });
        }

        return result;
    }

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
        currentWeapon = null;
    }

    void ClearCards()
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null)
                Destroy(spawned[i].gameObject);
        }
        spawned.Clear();
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
                if (spawned[i] != null)
                    spawned[i].transform.localScale = Vector3.one * s;
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
                    if (spawned[i] != null)
                        spawned[i].transform.localScale = Vector3.one * s;
                }
                yield return null;
            }
        }
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null)
                spawned[i].transform.localScale = Vector3.one;
        }
    }
}