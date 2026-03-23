public interface IShopable
{
    string   DisplayName  { get; }
    int      BasePrice    { get; }
    Rarity   ItemRarity   { get; }

    // Probability (0–1) that this item passes the stock check on each shop refresh.
    // 1.0 = always in pool. 0.0 = never appears.
    float    StockChance  { get; }

    ShopItem CreateShopItem();
}
