using UnityEngine;

public class PlacementController : MonoBehaviour
{
    public static PlacementController Instance;

    [Header("References")]
    public Material ghostValidMaterial;
    public Material ghostInvalidMaterial;

    private LayerMask groundLayer;
    private PlaceableData currentData;
    private GameObject ghostObject;
    private bool isPlacing = false;
    private int ghostX, ghostZ;
    private bool placementValid;

    void Awake()
    {
        Instance = this;
        groundLayer = LayerMask.GetMask("Ground");
        Debug.Log($"Ground layer mask: {groundLayer.value}");

        foreach (var col in FindObjectsByType<Collider>(FindObjectsSortMode.None))
            Debug.Log($"Collider: {col.gameObject.name} — layer: {LayerMask.LayerToName(col.gameObject.layer)}");
    }

    public void BeginPlacement(PlaceableData data)
    {
        if (isPlacing) CancelPlacement();
        currentData = data;
        isPlacing = true;

        ghostObject = Instantiate(data.prefab);
        SetGhostMaterials(ghostValidMaterial);

        foreach (var mb in ghostObject.GetComponentsInChildren<MonoBehaviour>())
            mb.enabled = false;

        foreach (var col in ghostObject.GetComponentsInChildren<Collider>())
            col.enabled = false;
    }

    public void CancelPlacement()
    {
        if (ghostObject) Destroy(ghostObject);
        isPlacing = false;
        currentData = null;
    }

    void Update()
    {
        if (ShopUI.IsOpen)
    {
        if (ghostObject) ghostObject.SetActive(false);
        return;
    }

    if (ghostObject) ghostObject.SetActive(true);

        if (!isPlacing) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(ray.origin, ray.direction * 200f, Color.red);
        Debug.Log($"Ground layer mask value: {groundLayer.value}");

        if (Physics.Raycast(ray, out RaycastHit hit, 200f, groundLayer))
        {
            Debug.Log($"Hit: {hit.collider.gameObject.name}");
            Vector3 snapped = GridManager.Instance.SnapToGrid(hit.point);
            ghostObject.transform.position = snapped;

            GridManager.Instance.GetCellCoords(snapped, out ghostX, out ghostZ);
            placementValid = GridManager.Instance.CanPlace(ghostX, ghostZ,
                currentData.gridWidth, currentData.gridHeight);

            SetGhostMaterials(placementValid ? ghostValidMaterial : ghostInvalidMaterial);
        }
        else
        {
            Debug.Log("Raycast hit nothing");
        }

        if (Input.GetMouseButtonDown(0) && placementValid)
            ConfirmPlacement();

        if (Input.GetKeyDown(KeyCode.Escape))
            CancelPlacement();
    }

    void ConfirmPlacement()
    {
        if (!GameManager.Instance.SpendCoins(currentData.unlockCost)) return;

        GameObject placed = Instantiate(currentData.prefab,
            ghostObject.transform.position, Quaternion.identity);

        GridManager.Instance.SetOccupied(ghostX, ghostZ,
            currentData.gridWidth, currentData.gridHeight, placed);

        foreach (var mb in placed.GetComponentsInChildren<MonoBehaviour>())
            mb.enabled = true;

        Destroy(ghostObject);
        isPlacing = false;
        currentData = null;
    }

    void SetGhostMaterials(Material mat)
    {
        if (ghostObject == null || mat == null) return;
        foreach (var r in ghostObject.GetComponentsInChildren<Renderer>())
            r.material = mat;
    }
}