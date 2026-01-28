using UnityEngine;

public static class CharacterProgressStore
{
    private const string KEY_PREFIX = "CHAR_LEVEL_";
    public const int MAX_LEVEL = 12;

    public static int GetLevel(string characterId, int defaultLevel = 1)
    {
        if (string.IsNullOrEmpty(characterId)) return defaultLevel;
        return PlayerPrefs.GetInt(KEY_PREFIX + characterId, defaultLevel);
    }

    public static void SetLevel(string characterId, int level)
    {
        if (string.IsNullOrEmpty(characterId)) return;
        level = Mathf.Clamp(level, 1, MAX_LEVEL);
        PlayerPrefs.SetInt(KEY_PREFIX + characterId, level);
        PlayerPrefs.Save();
    }

    public static bool CanLevelUp(string characterId, int defaultLevel = 1)
    {
        int lv = GetLevel(characterId, defaultLevel);
        return lv < MAX_LEVEL;
    }

    public static int LevelUp(string characterId, int defaultLevel = 1)
    {
        int lv = GetLevel(characterId, defaultLevel);
        if (lv >= MAX_LEVEL) return lv;

        lv++;
        SetLevel(characterId, lv);
        return lv;
    }

    public static class CharacterLevelUtil
    {
        public static int GetCurrentLevel(string characterId, int defaultLv = 1)
        {
            if (string.IsNullOrEmpty(characterId)) return 1;
            return CharacterProgressStore.GetLevel(characterId, Mathf.Max(1, defaultLv));
        }
    }

    public static long GetLevelUpCost(int currentLevel, long baseCost = 250, int multiplier = 6)
    {
        currentLevel = Mathf.Clamp(currentLevel, 1, MAX_LEVEL);
        int exp = Mathf.Max(0, currentLevel - 1);

        double cost = baseCost * System.Math.Pow(multiplier, exp);

        if (cost > long.MaxValue) return long.MaxValue;
        return (long)System.Math.Round(cost);
    }

    public static bool TryLevelUpWithMoney(
        string characterId,
        int defaultLevel,
        long baseCost,
        int multiplier,
        out int newLevel,
        out long costPaid)
    {
        newLevel = GetLevel(characterId, defaultLevel);
        costPaid = 0;

        if (newLevel >= MAX_LEVEL) return false;

        long cost = GetLevelUpCost(newLevel, baseCost, multiplier);

        if (PlayerMoney.Instance == null) return false;

        if (PlayerMoney.Instance.currentMoney < cost) return false;

        PlayerMoney.Instance.AddMoney(-cost);

        int lvAfter = LevelUp(characterId, defaultLevel);

        newLevel = lvAfter;
        costPaid = cost;
        return true;
    }


}
