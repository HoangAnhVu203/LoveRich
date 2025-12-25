using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FlirtBookPanel : UICanvas
{
    [Header("List")]
    [SerializeField] RectTransform content;
    [SerializeField] CharacterThumbItemUI thumbPrefab;
    [SerializeField] List<CharacterData> characters;
    [SerializeField] ScrollSnapToCenter centerSelector;

    [Header("UI Detail")]
    [SerializeField] CharacterInfoPanelUI infoPanel;

    [Header("Photo Button (Lock at Lv4)")]
    [SerializeField] Button cameraBtn;
    [SerializeField] CanvasGroup cameraBtnGroup;
    [SerializeField, Range(0f, 1f)] float lockedAlpha = 0.4f;
    [SerializeField, Range(0f, 1f)] float unlockedAlpha = 1f;
    [SerializeField] int photoUnlockLevel = 4;
    [SerializeField] Text revenueBonusText;


    CharacterData currentCharacter;

    readonly List<CharacterThumbItemUI> spawnedThumbs = new();

    void OnEnable()
    {
        FlirtBookUnlockStore.OnUnlocked += OnUnlockedGirl;
        Build();               
        if (currentCharacter == null && characters != null && characters.Count > 0)
            currentCharacter = characters[0];
        RefreshCurrentUI();
    }

    void OnDisable()
    {
        FlirtBookUnlockStore.OnUnlocked -= OnUnlockedGirl;
    }

    void OnUnlockedGirl(int girlIndex)
    {
        Build();

        RefreshCurrentUI();
    }

    void Start()
    {
        Build();

        if (currentCharacter == null && characters != null && characters.Count > 0)
            currentCharacter = characters[0];

        RefreshCurrentUI();
    }

    void Build()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        spawnedThumbs.Clear();

        if (characters == null) characters = new List<CharacterData>();

        for (int i = 0; i < characters.Count; i++)
        {
            var c = characters[i];
            if (c == null) continue;

            if (!FlirtBookUnlockStore.IsUnlocked(i))
                continue;

            var item = Instantiate(thumbPrefab, content);
            item.Bind(c);
            spawnedThumbs.Add(item);

            if (currentCharacter == null)
                currentCharacter = c;
        }

        if (spawnedThumbs.Count == 0)
        {
            if (centerSelector != null)
                centerSelector.SetItems(spawnedThumbs); 
            return;
        }


        if (centerSelector != null)
        {
            centerSelector.SetItems(spawnedThumbs);
            centerSelector.OnCenteredChanged = (c) =>
            {
                currentCharacter = c;
                RefreshCurrentUI();
            };
        }

        if (characters.Count > 0)
            currentCharacter = characters[0];

    }

    int GetCurrentLevel(CharacterData character)
    {
        if (character == null) return 1;

        int defaultLv = character.level <= 0 ? 1 : character.level;
        return CharacterProgressStore.GetLevel(character.characterId, defaultLv);
    }

    void RefreshCurrentUI()
    {
        if (currentCharacter == null) return;

        int lv = GetCurrentLevel(currentCharacter);
        if (infoPanel != null)
            infoPanel.Show(currentCharacter, lv);

        RefreshPhotoButtonState(lv);

        RefreshLevelUpButtonState(lv);

        RefreshRevenueBonusUI(lv);
    }

    void RefreshPhotoButtonState(int lv)
    {
        bool unlocked = lv >= photoUnlockLevel;

        if (cameraBtn != null)
            cameraBtn.interactable = unlocked;

        if (cameraBtnGroup != null)
        {
            cameraBtnGroup.alpha = unlocked ? unlockedAlpha : lockedAlpha;
            cameraBtnGroup.interactable = unlocked;
            cameraBtnGroup.blocksRaycasts = unlocked;
        }
    }

    void RefreshLevelUpButtonState(int lv)
    {
        bool canUp = lv < CharacterProgressStore.MAX_LEVEL;
    }

    void RefreshRevenueBonusUI(int lv)
    {
        if (revenueBonusText == null) return;

        // Lv1=0%, Lv2=10%, Lv3=20%...
        float bonusPercent = (Mathf.Max(1, lv) - 1) * 10f;

        // Ví dụ text: "+20% revenue per pass"
        revenueBonusText.text = $"REVENUE +{bonusPercent:0}%";
    }


    public void OnLevelUpClicked()
    {
        if (currentCharacter == null) return;

        int lv = GetCurrentLevel(currentCharacter);
        if (lv >= CharacterProgressStore.MAX_LEVEL) return;

        int defaultLv = currentCharacter.level <= 0 ? 1 : currentCharacter.level;
        int newLv = CharacterProgressStore.LevelUp(currentCharacter.characterId, defaultLv);

        if (infoPanel != null)
            infoPanel.Refresh(newLv);

        RefreshPhotoButtonState(newLv);

        RefreshLevelUpButtonState(newLv);

        RefreshRevenueBonusUI(newLv);


        Debug.Log($"[LevelUp] {currentCharacter.characterId} -> LEVEL {newLv}");
    }

    public void OpenPhotoBookBTN()
    {
        if (currentCharacter == null) return;

        int lv = GetCurrentLevel(currentCharacter);

        if (lv < photoUnlockLevel)
        {
            Debug.Log($"Photo locked: Reach level {photoUnlockLevel} to unlock.");
            return;
        }

        var panel = UIManager.Instance.OpenUI<PhotoViewerPanelUI>();
        panel.Show(currentCharacter);
    }

    public void CloseBTN()
    {
        UIManager.Instance.CloseUIDirectly<FlirtBookPanel>();
    }

    public void OnDimClick()
    {
        UIManager.Instance.CloseUIDirectly<FlirtBookPanel>();
    }
}
