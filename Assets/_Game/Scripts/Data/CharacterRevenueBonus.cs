using UnityEngine;

public static class CharacterRevenueBonus
{
    // Lv1 = 0%, Lv2 = 10%, Lv3 = 20% ...
    public static float GetBonusPercentByLevel(int level)
    {
        level = Mathf.Max(1, level);
        return (level - 1) * 0.10f;
    }

    public static float GetMultiplierByLevel(int level)
    {
        return 1f + GetBonusPercentByLevel(level);
    }
}
