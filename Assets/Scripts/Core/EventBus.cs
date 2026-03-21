using System;

public static class EventBus
{
    // Economy
    public static event Action<int>                     OnCoinsChanged;

    // Inventory — seeds
    public static event Action<CropData>                OnSeedAdded;
    public static event Action<CropData>                OnSeedUsed;

    // Inventory — harvest
    public static event Action<HarvestedCrop>           OnCropHarvested;
    public static event Action<HarvestedCrop>           OnCropSold;

    // Inventory — tools
    public static event Action<ToolData>                OnToolAdded;
    public static event Action<ToolData>                OnToolUsed;

    // Shop
    public static event Action                          OnShopOpened;
    public static event Action                          OnShopClosed;
    public static event Action                          OnShopStockRefreshed;

    // Farm
    public static event Action<FarmPlot>                OnPlotPlanted;
    public static event Action<FarmPlot>                OnPlotReady;
    public static event Action<FarmPlot>                OnPlotRemoved;
    public static event Action<FarmPlot>                OnMutationOccurred;

    // Character evolution — fired when a plot transitions to PlotState.Evolved
    public static event Action<FarmPlot, CharacterData> OnPlotEvolved;

    // World Events
    public static event Action<WorldEventData>          OnWorldEventStarted;
    public static event Action<WorldEventData>          OnWorldEventEnded;

    // Relationships — keyed to CharacterData (replaces old HarvestedCrop stub)
    public static event Action<CharacterData, int>      OnRelationshipChanged;

    public static void Raise_CoinsChanged(int amount)                               => OnCoinsChanged?.Invoke(amount);
    public static void Raise_SeedAdded(CropData crop)                               => OnSeedAdded?.Invoke(crop);
    public static void Raise_SeedUsed(CropData crop)                                => OnSeedUsed?.Invoke(crop);
    public static void Raise_CropHarvested(HarvestedCrop crop)                      => OnCropHarvested?.Invoke(crop);
    public static void Raise_CropSold(HarvestedCrop crop)                           => OnCropSold?.Invoke(crop);
    public static void Raise_ToolAdded(ToolData tool)                               => OnToolAdded?.Invoke(tool);
    public static void Raise_ToolUsed(ToolData tool)                                => OnToolUsed?.Invoke(tool);
    public static void Raise_ShopOpened()                                           => OnShopOpened?.Invoke();
    public static void Raise_ShopClosed()                                           => OnShopClosed?.Invoke();
    public static void Raise_ShopStockRefreshed()                                   => OnShopStockRefreshed?.Invoke();
    public static void Raise_PlotPlanted(FarmPlot plot)                             => OnPlotPlanted?.Invoke(plot);
    public static void Raise_PlotReady(FarmPlot plot)                               => OnPlotReady?.Invoke(plot);
    public static void Raise_PlotRemoved(FarmPlot plot)                             => OnPlotRemoved?.Invoke(plot);
    public static void Raise_MutationOccurred(FarmPlot plot)                        => OnMutationOccurred?.Invoke(plot);
    public static void Raise_PlotEvolved(FarmPlot plot, CharacterData data)         => OnPlotEvolved?.Invoke(plot, data);
    public static void Raise_WorldEventStarted(WorldEventData data)                 => OnWorldEventStarted?.Invoke(data);
    public static void Raise_WorldEventEnded(WorldEventData data)                   => OnWorldEventEnded?.Invoke(data);
    public static void Raise_RelationshipChanged(CharacterData character, int level) => OnRelationshipChanged?.Invoke(character, level);
}
