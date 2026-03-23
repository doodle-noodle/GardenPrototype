using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShopStock : MonoBehaviour
{
    public static ShopStock Instance;

    [Header("Stock settings")]
    public int   itemsPerRefresh = 6;
    public float refreshInterval = 300f;

    [Header("All shop items — drag any CropData, PlaceableData, or ToolData here")]
    public List<ScriptableObject> allShopItems;

    public List<ShopItem> CurrentStock { get; private set; } = new List<ShopItem>();

    private float _refreshTimer = 0f;

    void Awake() => Instance = this;
    void Start()  => Refresh();

    void Update()
    {
        _refreshTimer += Time.deltaTime;
        if (_refreshTimer >= refreshInterval)
        {
            _refreshTimer = 0f;
            Refresh();
        }
    }

    public float TimeUntilRefresh => refreshInterval - _refreshTimer;

    public void Refresh()
    {
        CurrentStock.Clear();

        var pool = allShopItems
            .OfType<IShopable>()
            .Where(item => item is ToolData || Random.value <= item.StockChance)
            .Select(item => item.CreateShopItem())
            .ToList();

        // Fisher-Yates shuffle
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        int count = Mathf.Min(itemsPerRefresh, pool.Count);
        for (int i = 0; i < count; i++)
            CurrentStock.Add(pool[i]);

        EventBus.Raise_ShopStockRefreshed();
    }
}
