using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlirtBookPanel : UICanvas
{
    [SerializeField] RectTransform content;
    [SerializeField] CharacterThumbItemUI thumbPrefab;
    [SerializeField] List<CharacterData> characters;
    [SerializeField] ScrollSnapToCenter centerSelector;
    [SerializeField] Button cameraBtn;
    CharacterData currentCharacter;

    void Awake()
    {
    }

    void Start()
    {
        Build();
    }

    void Build()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        var spawned = new List<CharacterThumbItemUI>();

        foreach (var c in characters)
        {
            var item = Instantiate(thumbPrefab, content);
            item.Bind(c);
            spawned.Add(item);
        }

        centerSelector.SetItems(spawned);

        centerSelector.OnCenteredChanged = (c) => currentCharacter = c;

        // set mặc định ngay lập tức (để bấm photo không bị null)
        if (characters != null && characters.Count > 0)
            currentCharacter = characters[0];

    }

    public void CloseBTN()
    {
        UIManager.Instance.CloseUIDirectly<FlirtBookPanel>();
    }

    public void OpenPhotoBookBTN()
    {
        if (currentCharacter == null)
        {
            Debug.LogError("currentCharacter == null");
            return;
        }

        // Lấy instance vừa được mở
        var panel = UIManager.Instance.OpenUI<PhotoViewerPanelUI>();
        panel.Show(currentCharacter);
    }

    public void OnDimClick()
    {
        UIManager.Instance.CloseUIDirectly<FlirtBookPanel>();
    }

}
