using UnityEngine;
using UnityEngine.UI;

public class PanelNewHeart : UICanvas
{
    public Image heartIcon;
    public Text levelText;
    public Text incomeText;
    public Button closeButton;
    public GameObject blocker; 

    void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        Hide();
    }

    public void Show(Sprite icon, int level, long oldMoney, long newMoney)
    {

        if (heartIcon != null) heartIcon.sprite = icon;
        if (levelText != null) levelText.text = $"Level {level}";
        if (incomeText != null) incomeText.text = $"{oldMoney} -> {newMoney}";

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        UIManager.Instance.CloseUIDirectly<PanelNewHeart>();
        if (blocker != null) blocker.SetActive(false);
    }

    public void OnDimClick()
    {
        gameObject.SetActive(false);
    }
}
