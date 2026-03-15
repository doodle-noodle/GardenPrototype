using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Economy")]
    public int startingCoins = 50;

    [Header("UI")]
    public TextMeshProUGUI coinText;

    private int coins;

    void Awake()
    {
        Instance = this;
        coins    = startingCoins;
    }

    void Start() => UpdateUI();

    public bool SpendCoins(int amount)
    {
        if (coins < amount)
        {
            TutorialConsole.Error("Insufficient funds.");
            return false;
        }
        coins -= amount;
        UpdateUI();
        EventBus.Raise_CoinsChanged(coins);
        return true;
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        UpdateUI();
        EventBus.Raise_CoinsChanged(coins);
    }

    void UpdateUI()
    {
        if (coinText) coinText.text = $"Coins: {coins}";
    }
}