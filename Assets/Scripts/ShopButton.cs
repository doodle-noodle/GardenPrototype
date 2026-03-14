using UnityEngine;

public class ShopButton : MonoBehaviour
{
    public PlaceableData placeableData;

    public void OnBuyClicked()
    {
        PlacementController.Instance.BeginPlacement(placeableData);
    }
}