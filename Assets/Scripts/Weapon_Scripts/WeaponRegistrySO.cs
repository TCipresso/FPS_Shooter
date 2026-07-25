using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeaponGroundModel
{
    public WeaponDefinitionSO definition;
    [Tooltip("The mesh shown when this weapon is on the ground as a pickup.")]
    public Mesh mesh;
    [Tooltip("Materials for the ground mesh at level 1 (before pack-a-punch skin). One per submesh.")]
    public Material[] materials;
    [Tooltip("Local offset/rotation/scale applied to the ground model so it sits nicely on the pickup.")]
    public Vector3 localPosition = Vector3.zero;
    public Vector3 localEuler = Vector3.zero;
    public Vector3 localScale = Vector3.one;
}

[CreateAssetMenu(fileName = "WeaponRegistry", menuName = "Zarcade/Weapon Registry")]
public class WeaponRegistrySO : ScriptableObject
{
    [Tooltip("Every weapon definition in the game. The index here is the stable network ID used to sync dropped weapons. Order matters once shipped - append, don't reorder.")]
    public List<WeaponDefinitionSO> weapons = new List<WeaponDefinitionSO>();

    [Tooltip("Ground model (mesh + materials) shown for each weapon when dropped. Match each entry to a definition above.")]
    public List<WeaponGroundModel> groundModels = new List<WeaponGroundModel>();

    Dictionary<WeaponDefinitionSO, int> indexLookup;
    Dictionary<WeaponDefinitionSO, WeaponGroundModel> modelLookup;

    void BuildLookup()
    {
        indexLookup = new Dictionary<WeaponDefinitionSO, int>();
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i] != null)
                indexLookup[weapons[i]] = i;
        }

        modelLookup = new Dictionary<WeaponDefinitionSO, WeaponGroundModel>();
        for (int i = 0; i < groundModels.Count; i++)
        {
            WeaponGroundModel gm = groundModels[i];
            if (gm != null && gm.definition != null)
                modelLookup[gm.definition] = gm;
        }
    }

    public int GetIndex(WeaponDefinitionSO def)
    {
        if (def == null) return -1;
        if (indexLookup == null) BuildLookup();
        return indexLookup.TryGetValue(def, out int index) ? index : -1;
    }

    public WeaponDefinitionSO GetDefinition(int index)
    {
        if (index < 0 || index >= weapons.Count) return null;
        return weapons[index];
    }

    public WeaponGroundModel GetGroundModel(WeaponDefinitionSO def)
    {
        if (def == null) return null;
        if (modelLookup == null) BuildLookup();
        return modelLookup.TryGetValue(def, out WeaponGroundModel gm) ? gm : null;
    }
}