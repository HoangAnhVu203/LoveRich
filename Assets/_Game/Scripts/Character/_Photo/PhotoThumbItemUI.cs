using UnityEngine;
using UnityEngine.UI;

public class PhotoThumbItemUI : MonoBehaviour
{
    [SerializeField] Image thumb;
    // [SerializeField] GameObject selectedFrame;

    int index;
    System.Action<int> onClick;

    public void Bind(Sprite sprite, int idx, System.Action<int> cb)
    {
        thumb.sprite = sprite;
        index = idx;
        onClick = cb;
    }

    public void OnClick()
    {
        onClick?.Invoke(index);
    }

    // public void SetSelected(bool value)
    // {
    //     if (selectedFrame != null)
    //         selectedFrame.SetActive(value);
    // }
}
