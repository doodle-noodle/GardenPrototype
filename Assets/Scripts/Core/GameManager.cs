using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Economy")]
    public int startingCoins = 50;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI coinText;

    private int coins;

    void Awake()
    {
        Instance = this;
        coins    = startingCoins;
        ResolveCoinText();
    }

    void Start() => UpdateUI();

    // ── Economy ───────────────────────────────────────────────

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

    public bool CanAfford(int amount) => coins >= amount;

    // ── UI ────────────────────────────────────────────────────

    void UpdateUI()
    {
        if (coinText != null)
            coinText.text = $"Coins: {coins}";
    }

    // Assign in Inspector for reliability. Auto-search is a fallback only.
    void ResolveCoinText()
    {
        if (coinText != null) return;

        foreach (var tmp in FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
        {
            if (tmp.gameObject.name.ToLower().Contains("coin"))
            {
                coinText = tmp;
                Debug.LogWarning("GameManager: coinText found by name search. " +
                    "Assign it directly in the Inspector to avoid this.");
                return;
            }
        }

        Debug.LogError("GameManager: coinText not found. Coin UI will not update.");
    }
}