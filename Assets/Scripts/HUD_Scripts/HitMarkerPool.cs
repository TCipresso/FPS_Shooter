using UnityEngine;
using UnityEngine.UI;

public class HitMarkerPool : MonoBehaviour
{
    public static HitMarkerPool Instance { get; private set; }

    [Header("Colors")]
    public Color normalColor = Color.red;
    public Color critColor = Color.yellow;

    [Header("Hit Sounds (2D)")]
    public AudioSource hitSoundSource2D;
    public AudioClip critSound;
    [Range(0f, 1f)] public float critVolume = 1f;
    public AudioClip[] bodyHitSounds;
    [Range(0f, 1f)] public float bodyHitVolume = 1f;

    [Header("Hitmarker Shape")]
    public float armLength = 12f;
    public float gapFromCenter = 5f;
    public float lineThickness = 3f;

    [Header("Animation")]
    public bool noShakeMode = false;
    public float spawnScale = 2.2f;
    public float shrinkSpeed = 18f;
    public float fadeSpeed = 2.5f;
    public float repeatWindow = 0.18f;
    public float repeatScaleAdd = 0.4f;
    public float maxScale = 2.8f;
    public float shakeAddPerHit = 2.5f;
    public float maxShakeMagnitude = 7f;
    public float shakeDecayRate = 0.01f;
    [Range(0f, 15f)] public float rotShakeDegreesPerHit = 6f;
    public float rotShakeDecay = 0.85f;

    float _currentScale = 1f;
    float _baseScale = 1f;
    float _alpha = 0f;
    float _shakeMag = 0f;
    float _shakeX, _shakeY;
    float _rotOffset = 0f;
    float _lastHitTime = -999f;
    Color _activeColor;

    RectTransform _markerRT;
    Image[] _lines = new Image[4];

    int[] _bagOrder;
    int _bagIndex;

    void Awake()
    {
        Instance = this;
        BuildMarker();
        InitShuffleBag();
    }

    void BuildMarker()
    {
        GameObject root = new GameObject("HitMarker", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        _markerRT = root.GetComponent<RectTransform>();
        _markerRT.anchorMin = new Vector2(0.5f, 0.5f);
        _markerRT.anchorMax = new Vector2(0.5f, 0.5f);
        _markerRT.anchoredPosition = Vector2.zero;
        _markerRT.sizeDelta = Vector2.zero;
        _markerRT.localRotation = Quaternion.Euler(0f, 0f, 45f);

        for (int i = 0; i < 4; i++)
        {
            GameObject line = new GameObject("Line_" + i, typeof(RectTransform), typeof(Image));
            line.transform.SetParent(root.transform, false);
            _lines[i] = line.GetComponent<Image>();
            _lines[i].color = normalColor;
        }

        root.SetActive(false);
    }

    void InitShuffleBag()
    {
        if (bodyHitSounds == null || bodyHitSounds.Length == 0) return;
        _bagOrder = new int[bodyHitSounds.Length];
        for (int i = 0; i < _bagOrder.Length; i++) _bagOrder[i] = i;
        ShuffleBag();
    }

    public void Spawn(Vector3 worldHitPoint, bool isCrit = false)
    {
        PlayHitSound(isCrit);

        float now = Time.unscaledTime;
        float dt = now - _lastHitTime;
        _lastHitTime = now;
        _activeColor = isCrit ? critColor : normalColor;

        if (dt < repeatWindow && _alpha > 0.01f)
        {
            _baseScale = Mathf.Min(_baseScale + repeatScaleAdd, maxScale);
            if (!noShakeMode)
            {
                _shakeMag = Mathf.Min(_shakeMag + shakeAddPerHit, maxShakeMagnitude);
                _rotOffset += Random.Range(-rotShakeDegreesPerHit, rotShakeDegreesPerHit);
            }
            _currentScale = _baseScale;
        }
        else
        {
            _baseScale = spawnScale;
            _shakeMag = 0f;
            _rotOffset = 0f;
        }

        _currentScale = _baseScale;
        _alpha = 1f;
        _markerRT.gameObject.SetActive(true);
        ApplyLineLayout();
    }

    void Update()
    {
        if (_alpha <= 0f) return;

        _currentScale = Mathf.Lerp(_currentScale, 1f, Time.unscaledDeltaTime * shrinkSpeed);
        _alpha -= Time.unscaledDeltaTime * fadeSpeed;

        if (!noShakeMode && _shakeMag > 0.05f)
        {
            _shakeMag *= Mathf.Pow(shakeDecayRate, Time.unscaledDeltaTime);
            _shakeX = Random.Range(-1f, 1f) * _shakeMag;
            _shakeY = Random.Range(-1f, 1f) * _shakeMag;
            _rotOffset += Random.Range(-1f, 1f) * (_shakeMag * 0.4f);
            _rotOffset *= rotShakeDecay;
        }
        else
        {
            _shakeX = Mathf.Lerp(_shakeX, 0f, Time.unscaledDeltaTime * 20f);
            _shakeY = Mathf.Lerp(_shakeY, 0f, Time.unscaledDeltaTime * 20f);
            _rotOffset = Mathf.Lerp(_rotOffset, 0f, Time.unscaledDeltaTime * 12f);
        }

        if (_alpha <= 0f)
        {
            _alpha = 0f;
            _markerRT.gameObject.SetActive(false);
            return;
        }

        _markerRT.anchoredPosition = new Vector2(_shakeX, _shakeY);
        _markerRT.localRotation = Quaternion.Euler(0f, 0f, 45f + _rotOffset);
        ApplyLineLayout();
    }

    void ApplyLineLayout()
    {
        float arm = armLength * _currentScale;
        float gap = gapFromCenter * _currentScale;
        float thick = lineThickness * _currentScale;
        Color c = _activeColor;
        c.a = Mathf.Clamp01(_alpha);

        (Vector2 anchor, Vector2 pos, Vector2 size)[] configs =
        {
            (new Vector2(0.5f, 0.5f), new Vector2(-(gap + arm * 0.5f), 0f), new Vector2(arm, thick)),
            (new Vector2(0.5f, 0.5f), new Vector2( (gap + arm * 0.5f), 0f), new Vector2(arm, thick)),
            (new Vector2(0.5f, 0.5f), new Vector2(0f,  (gap + arm * 0.5f)), new Vector2(thick, arm)),
            (new Vector2(0.5f, 0.5f), new Vector2(0f, -(gap + arm * 0.5f)), new Vector2(thick, arm)),
        };

        for (int i = 0; i < 4; i++)
        {
            RectTransform rt = _lines[i].GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = configs[i].anchor;
            rt.anchoredPosition = configs[i].pos;
            rt.sizeDelta = configs[i].size;
            _lines[i].color = c;
        }
    }

    void PlayHitSound(bool isCrit)
    {
        if (hitSoundSource2D == null) return;
        if (isCrit)
        {
            if (critSound != null) hitSoundSource2D.PlayOneShot(critSound, critVolume);
        }
        else if (_bagOrder != null && _bagOrder.Length > 0)
        {
            AudioClip clip = bodyHitSounds[_bagOrder[_bagIndex++]];
            if (_bagIndex >= _bagOrder.Length) { ShuffleBag(); _bagIndex = 0; }
            if (clip != null) hitSoundSource2D.PlayOneShot(clip, bodyHitVolume);
        }
    }

    void ShuffleBag()
    {
        for (int i = _bagOrder.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = _bagOrder[i]; _bagOrder[i] = _bagOrder[j]; _bagOrder[j] = tmp;
        }
    }
}