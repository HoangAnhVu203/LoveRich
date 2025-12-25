using System;
using System.Collections.Generic;
using UnityEngine;

public static class FlirtBookUnlockStore
{
    const string KEY = "FLIRTBOOK_UNLOCK_GIRLS_V1";

    [Serializable]
    class Wrapper { public List<int> unlocked = new(); }

    static HashSet<int> _cache;
    public static event Action<int> OnUnlocked; 

    static void EnsureLoaded()
    {
        if (_cache != null) return;
        _cache = new HashSet<int>();

        string json = PlayerPrefs.GetString(KEY, "");
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            var w = JsonUtility.FromJson<Wrapper>(json);
            if (w?.unlocked == null) return;
            foreach (var i in w.unlocked) _cache.Add(i);
        }
        catch { }
    }

    public static bool IsUnlocked(int girlIndex)
    {
        EnsureLoaded();
        return girlIndex >= 0 && _cache.Contains(girlIndex);
    }

    public static bool TryUnlock(int girlIndex)
    {
        if (girlIndex < 0) return false;

        EnsureLoaded();
        if (_cache.Contains(girlIndex)) return false;

        _cache.Add(girlIndex);
        Save();

        OnUnlocked?.Invoke(girlIndex); 
        return true;
    }

    static void Save()
    {
        var w = new Wrapper { unlocked = new List<int>(_cache) };
        string json = JsonUtility.ToJson(w);
        PlayerPrefs.SetString(KEY, json);
        PlayerPrefs.Save();
    }
}
