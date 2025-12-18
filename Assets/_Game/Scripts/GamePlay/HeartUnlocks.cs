using UnityEngine;

public class HeartUnlocks : MonoBehaviour
{
    public static HeartUnlocks Instance { get; private set; }
    const string KEY = "HEART_UNLOCKED_";

    const string KEY_MAX_LEVEL = "HEART_MAX_LEVEL";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool IsUnlocked(HeartType type)
        => PlayerPrefs.GetInt(KEY + type, 0) == 1;

    public void MarkUnlocked(HeartType type)
        => PlayerPrefs.SetInt(KEY + type, 1);

    public int GetMaxUnlockedLevel()
    {
        return PlayerPrefs.GetInt(KEY_MAX_LEVEL, 1);
    }

    public void TryUpdateMaxLevel(int level)
    {
        int cur = GetMaxUnlockedLevel();
        if (level > cur)
        {
            PlayerPrefs.SetInt(KEY_MAX_LEVEL, level);
            PlayerPrefs.Save();
        }
    }

}
