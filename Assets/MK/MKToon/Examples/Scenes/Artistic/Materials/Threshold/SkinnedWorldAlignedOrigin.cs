using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class SkinnedWorldAlignedOrigin : MonoBehaviour
{
    static readonly int ObjectOriginWSId = Shader.PropertyToID("_ObjectOriginWS");
    static readonly int ObjectRotRow0Id = Shader.PropertyToID("_ObjectRotRow0");
    static readonly int ObjectRotRow1Id = Shader.PropertyToID("_ObjectRotRow1");
    static readonly int ObjectRotRow2Id = Shader.PropertyToID("_ObjectRotRow2");

    [SerializeField] Transform originSource;

    MaterialPropertyBlock _mpb;
    Renderer _renderer;

    void OnEnable()
    {
        _renderer = GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();
        if (originSource == null) originSource = transform;
    }

    void LateUpdate()
    {
        Vector3 origin = originSource.position;
        Quaternion rot = originSource.rotation;
        Matrix4x4 rotMatrix = Matrix4x4.Rotate(rot);

        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetVector(ObjectOriginWSId, new Vector4(origin.x, origin.y, origin.z, 0));
        _mpb.SetVector(ObjectRotRow0Id, rotMatrix.GetRow(0));
        _mpb.SetVector(ObjectRotRow1Id, rotMatrix.GetRow(1));
        _mpb.SetVector(ObjectRotRow2Id, rotMatrix.GetRow(2));
        _renderer.SetPropertyBlock(_mpb);
    }
}