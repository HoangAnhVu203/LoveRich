using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PhotoViewerPanelUI : UICanvas
{
    [Header("Main")]
    [SerializeField] Image mainImage;
    [SerializeField] TMP_Text commentText;

    [Header("Thumbs")]
    [SerializeField] Transform content;
    [SerializeField] PhotoThumbItemUI thumbPrefab;

    List<PhotoThumbItemUI> thumbs = new();
    CharacterData current;
    int currentIndex;

    public void Show(CharacterData data)
    {
        current = data;
        BuildThumbs();
        Select(0);
        gameObject.SetActive(true);
    }

    void BuildThumbs()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        thumbs.Clear();

        for (int i = 0; i < current.photos.Count; i++)
        {
            var entry = current.photos[i];
            if (entry == null || entry.photo == null) continue;

            var t = Instantiate(thumbPrefab);
            t.transform.SetParent(content, false);

            int idx = i;
            t.Bind(entry.photo, idx, Select);
            thumbs.Add(t);
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)content);
    }


    void Select(int index)
    {
        currentIndex = index;
        mainImage.sprite = current.photos[index].photo;
        commentText.text = current.photos[index].comment;

        // for (int i = 0; i < thumbs.Count; i++)
        //     thumbs[i].SetSelected(i == index);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void OnDimClick()
    {
        Close();
    }
}
