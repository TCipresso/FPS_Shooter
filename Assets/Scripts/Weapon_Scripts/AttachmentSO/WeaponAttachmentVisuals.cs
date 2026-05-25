using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class AttachmentVisual
{
    public string modelName;
    public GameObject model;
}

[System.Serializable]
public class AttachmentMount
{
    public string slotName;
    public List<AttachmentVisual> visuals = new List<AttachmentVisual>();
}

public class WeaponAttachmentVisuals : MonoBehaviour
{
    public List<AttachmentMount> mounts = new List<AttachmentMount>();

    public void ApplyAttachments(List<AttachmentSO> attachments)
    {
        Debug.Log($"[AttachmentVisuals] ApplyAttachments called with {attachments?.Count ?? 0} attachments");

        foreach (AttachmentMount mount in mounts)
            foreach (AttachmentVisual visual in mount.visuals)
                if (visual.model != null)
                    visual.model.SetActive(false);

        if (attachments == null) return;

        foreach (AttachmentSO attachment in attachments)
        {
            if (attachment == null) continue;
            Debug.Log($"[AttachmentVisuals] Trying to match slot:{attachment.slotType} model:{attachment.modelName}");

            AttachmentMount mount = mounts.Find(m => m.slotName == attachment.slotType);
            if (mount == null) { Debug.Log($"[AttachmentVisuals] No mount found for {attachment.slotType}"); continue; }

            AttachmentVisual visual = mount.visuals.Find(v => v.modelName == attachment.modelName);
            if (visual != null && visual.model != null)
            {
                Debug.Log($"[AttachmentVisuals] Enabling {visual.modelName}");
                visual.model.SetActive(true);
            }
        }
    }
}