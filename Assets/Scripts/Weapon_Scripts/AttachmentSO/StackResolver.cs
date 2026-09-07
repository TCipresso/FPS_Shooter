using UnityEngine;

// How a single modifier's value combines when a weapon has multiple copies of the same
// attachment. Chosen per modifier row, so one attachment can stack some of its properties
// and leave others flat.
public enum StackMode
{
    Linear,                // base * count            - e.g. 2 scopes = +10% crit
    NoScale,               // base (count ignored)    - cooldowns, one-time properties
    DiminishingChance,     // 1 - (1-base)^count      - probabilities approaching 100% (crit, proc chance)
    DiminishingGeometric,  // base * (1-p^count)/(1-p) - each extra copy worth `param` of the last (0..1)
    Capped,                // min(base * count, param) - linear up to a ceiling
}

public static class StackResolver
{
    // Total contribution of `count` copies of a modifier.
    public static float Resolve(float baseValue, int count, StackMode mode, float param)
    {
        if (count <= 0) return 0f;

        switch (mode)
        {
            case StackMode.NoScale:
                return baseValue;

            case StackMode.DiminishingChance:
                return 1f - Mathf.Pow(1f - baseValue, count);

            case StackMode.DiminishingGeometric:
            {
                float p = Mathf.Clamp(param, 0f, 0.999f);
                return baseValue * (1f - Mathf.Pow(p, count)) / (1f - p);
            }

            case StackMode.Capped:
                return Mathf.Min(baseValue * count, param);

            case StackMode.Linear:
            default:
                return baseValue * count;
        }
    }
}
