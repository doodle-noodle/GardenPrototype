public interface IShopable
{
    string   DisplayName  { get; }
    int      BasePrice    { get; }
    Rarity   ItemRarity   { get; }
    ShopItem CreateShopItem();
}