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

    public bool CanAfford(int amount) => coins >= amount;

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
    private int     displayedCoins = 0;
    private int     targetCoins    = 0;
    private float   coinAnimTimer  = 0f;
    private const float CoinAnimDuration = 0.4f;

    void UpdateUI()
    {
        targetCoins = coins;
        coinAnimTimer = 0f;
    }

    void Update()
    {
        if (displayedCoins != targetCoins)
        {
            coinAnimTimer += Time.deltaTime;
            float t = Mathf.Clamp01(coinAnimTimer / CoinAnimDuration);
            displayedCoins = (int)Mathf.Lerp(displayedCoins, targetCoins, t);

            if (coinText) coinText.text = $"Coins: {displayedCoins}";

            // Snap when close enough
            if (Mathf.Abs(displayedCoins - targetCoins) <= 1)
            {
                displayedCoins = targetCoins;
                if (coinText) coinText.text = $"Coins: {displayedCoins}";
            }
        }
    }
}