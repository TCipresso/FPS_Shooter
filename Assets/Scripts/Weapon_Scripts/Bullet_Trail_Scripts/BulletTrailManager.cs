using System.Collections.Generic;
using UnityEngine;

public class BulletTrailManager : MonoBehaviour
{
    static BulletTrailManager _instance;
    public static BulletTrailManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("BulletTrailManager");
                _instance = go.AddComponent<BulletTrailManager>();
            }
            return _instance;
        }
    }

    class ActiveTrail
    {
        public Transform Transform;
        public TrailRenderer TrailRenderer;
        public GameObject GameObject;
        public string PoolKey;
        public Vector3 Start;
        public Vector3 End;
        public float TravelTime;
        public float Elapsed;
        public float FadeTimer;
        public bool Traveling;
    }

    List<ActiveTrail> activeTrails = new List<ActiveTrail>();
    Dictionary<GameObject, ActiveTrail> lookup = new Dictionary<GameObject, ActiveTrail>();
    Stack<ActiveTrail> freeList = new Stack<ActiveTrail>();

    public void Register(GameObject go, Transform t, TrailRenderer tr, string poolKey, Vector3 start, Vector3 end, float travelTime)
    {
        if (!lookup.TryGetValue(go, out ActiveTrail active))
        {
            active = freeList.Count > 0 ? freeList.Pop() : new ActiveTrail();
            active.Transform = t;
            active.TrailRenderer = tr;
            active.GameObject = go;
            lookup[go] = active;
            activeTrails.Add(active);
        }

        active.PoolKey = poolKey;
        active.Start = start;
        active.End = end;
        active.TravelTime = travelTime;
        active.Elapsed = 0f;
        active.Traveling = true;
        active.FadeTimer = 0f;
    }

    public void Unregister(GameObject go)
    {
        if (!lookup.TryGetValue(go, out ActiveTrail active))
            return;

        lookup.Remove(go);
        int index = activeTrails.IndexOf(active);
        if (index >= 0)
        {
            int lastIndex = activeTrails.Count - 1;
            activeTrails[index] = activeTrails[lastIndex];
            activeTrails.RemoveAt(lastIndex);
        }
        freeList.Push(active);
    }

    void Update()
    {
        float dt = Time.deltaTime;

        for (int i = activeTrails.Count - 1; i >= 0; i--)
        {
            ActiveTrail active = activeTrails[i];

            if (active.Traveling)
            {
                active.Elapsed += dt;
                float t = active.Elapsed / active.TravelTime;

                if (t >= 1f)
                {
                    active.Transform.position = active.End;
                    active.Traveling = false;
                    active.FadeTimer = active.TrailRenderer.time;
                }
                else
                {
                    active.Transform.position = Vector3.Lerp(active.Start, active.End, t);
                }
            }
            else
            {
                active.FadeTimer -= dt;
                if (active.FadeTimer <= 0f)
                {
                    GameObject go = active.GameObject;
                    string key = active.PoolKey;

                    lookup.Remove(go);
                    int lastIndex = activeTrails.Count - 1;
                    activeTrails[i] = activeTrails[lastIndex];
                    activeTrails.RemoveAt(lastIndex);
                    freeList.Push(active);

                    BulletPool.Instance.Return(key, go);
                }
            }
        }
    }
}