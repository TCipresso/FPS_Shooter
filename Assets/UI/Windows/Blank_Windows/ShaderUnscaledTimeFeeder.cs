using UnityEngine;

public class ShaderUnscaledTimeFeeder : MonoBehaviour
{
    static readonly int UnscaledTimeId = Shader.PropertyToID("_UnscaledTime");

    void Update()
    {
        Shader.SetGlobalFloat(UnscaledTimeId, Time.unscaledTime);
    }
}