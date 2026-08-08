using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class OffHandEntry
{
    public string entryName;
    public OffHandDefinitionSO definition;
    public GameObject offHandRoot;
    public List<OffHandBase> offHandBases = new List<OffHandBase>();

    public OffHandBase Primary => offHandBases != null && offHandBases.Count > 0 ? offHandBases[0] : null;
}
