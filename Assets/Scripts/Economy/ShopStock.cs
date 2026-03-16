using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShopStock : MonoBehaviour
{
    public static ShopStock Instance;

    [Header("Stock settings")]
    public int   itemsPerRefresh = 6;
    public float refreshInterval = 300f;

    [Header("All shop items — drag any CropData or PlaceableData here")]
    public List<ScriptableObject> allShopItems;

    public List<ShopItem> CurrentStock { get; private set; } = new List<ShopItem>();

    private float refreshTimer = 0f;

    void Awake() => Instance = this;
    void Start()  => Refresh();

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

        // Any ScriptableObject implementing IShopable is valid stock
        var pool = allShopItems
            .OfType<IShopable>()
            .Select(item => item.CreateShopItem())
            .ToList();

        // Shuffle
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        int count = Mathf.Min(itemsPerRefresh, pool.Count);
        for (int i = 0; i < count; i++)
            CurrentStock.Add(pool[i]);

        EventBus.Raise_ShopStockRefreshed();
        Debug.Log($"Shop refreshed — {CurrentStock.Count} items available.");
    }
}