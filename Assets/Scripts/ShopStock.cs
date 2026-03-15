using System.Collections.Generic;
using UnityEngine;

public class ShopStock : MonoBehaviour
{
    public static ShopStock Instance;

    [Header("Stock settings")]
    public int    itemsPerRefresh   = 6;
    public float  refreshInterval   = 300f;  // 5 minutes

    [Header("All possible items")]
    public CropData[]      allCrops;
    public PlaceableData[] allPlaceables;

    public List<ShopItem>  CurrentStock { get; private set; } = new();

    private float refreshTimer = 0f;

    void Awake() => Instance = this;

    void Start()
    {
        Refresh();
    }

    void Update()
    {
        refreshTimer += Time.deltaTime;
        if (refreshTimer >= refreshInterval)
        {
            refreshTimer = 0f;
            Refresh();
        }
    }

    public float TimeUntilRefresh => refreshInterval - refreshTimer;

    void Refresh()
    {
        CurrentStock.Clear();

        // Build a pool of all possible items
        var pool = new List<ShopItem>();

        foreach (var crop in allCrops)
            pool.Add(ShopItem.MakeSeed(crop, RankUtility.RollRank()));

        foreach (var placeable in allPlaceables)
            pool.Add(ShopItem.MakePlaceable(placeable, RankUtility.RollRank()));

        // Shuffle and pick itemsPerRefresh items
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        int count = Mathf.Min(itemsPerRefresh, pool.Count);
        for (int i = 0; i < count; i++)
            CurrentStock.Add(pool[i]);

        EventBus.Raise_ShopStockRefreshed();
        Debug.Log($"Shop refreshed with {CurrentStock.Count} items.");
    }
}