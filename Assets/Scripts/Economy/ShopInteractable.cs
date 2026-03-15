using UnityEngine;

public class ShopInteractable : MonoBehaviour
{
    void OnMouseDown()
    {
        ShopUI.Instance.ToggleShop();
    }
}