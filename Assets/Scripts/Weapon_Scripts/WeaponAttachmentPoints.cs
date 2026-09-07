using System.Collections.Generic;
using UnityEngine;

// Sits on a weapon's weaponRoot (or a child). Maps string slot keys to pre-placed,
// disabled visual GameObjects (scope, suppressor, extended mag, laser...). The attachment
// system enables the slots an equipped attachment declares.
public class WeaponAttachmentPoints : MonoBehaviour
{
    [System.Serializable]
    public struct Slot
    {
        public string key;
        public GameObject target;
    }

    [Tooltip("Place each visual part as a disabled child of the weapon, then register it here under a key.")]
    public List<Slot> visuals = new List<Slot>();

    public void SetSlot(string key, bool on)
    {
        if (string.IsNullOrEmpty(key)) return;
        for (int i = 0; i < visuals.Count; i++)
        {
            if (visuals[i].key == key && visuals[i].target != null)
                visuals[i].target.SetActive(on);
        }
    }

    public void ResetAll()
    {
        for (int i = 0; i < visuals.Count; i++)
            if (visuals[i].target != null)
                visuals[i].target.SetActive(false);
    }
}
