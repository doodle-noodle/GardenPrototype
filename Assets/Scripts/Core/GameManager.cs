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

        // Auto-find coinText if not assigned in Inspector
        if (coinText == null)
        {
            foreach (var tmp in FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
            {
                if (tmp.gameObject.name.ToLower().Contains("coin"))
                {
                    coinText = tmp;
                    break;
                }
            }
        }

        if (coinText == null)
            Debug.LogWarning("GameManager: coinText not found. Assign it in the Inspector.");
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
}