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

}
