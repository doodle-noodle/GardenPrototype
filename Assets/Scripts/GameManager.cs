using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int coins = 20;
    public int seedCost = 5;
    public int cropValue = 15;

    public TextMeshProUGUI coinText;
    public GameObject shopPanel;
    public bool shopOpen = false;

    void Awake() => Instance = this;

    void Start() => UpdateUI();

    public void UpdateUI()
    {
        if (coinText != null)
            coinText.text = "Coins: " + coins;
    }

    public bool BuySeed()
    {
        if (coins >= seedCost)
        {
            coins -= seedCost;
            UpdateUI();
            return true;
        }
        Debug.Log("Not enough coins!");
        return false;
    }

    public void SellCrop()
    {
        coins += cropValue;
        UpdateUI();
        Debug.Log("Sold crop! +" + cropValue + " coins");
    }

    public void ToggleShop()
    {
        shopOpen = !shopOpen;
        if (shopPanel != null)
            shopPanel.SetActive(shopOpen);
    }
}