using UnityEngine;

public class ShopTrigger : MonoBehaviour
{
    void OnMouseDown()
    {
        GameManager.Instance.ToggleShop();
    }
}