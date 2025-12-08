using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerMoney : MonoBehaviour
{
    public static PlayerMoney Instance { get; private set; }

    [Header("UI")]
    public Text moneyText;

    [Header("Money Settings")]
    public int currentMoney = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        UpdateUI();
    }

    public void AddMoney(int amount)
    {
        currentMoney += amount;
        if (currentMoney < 0) currentMoney = 0;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (moneyText != null)
            moneyText.text = currentMoney.ToString("$ " + currentMoney);
    }
}
