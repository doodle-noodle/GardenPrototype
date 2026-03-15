using UnityEngine;
using UnityEngine.EventSystems;

public class PlacementController : MonoBehaviour
{
    public static PlacementController Instance;

    [Header("References")]
    public Material ghostValidMaterial;
    public Material ghostInvalidMaterial;

    [Header("Hierarchy organisation")]
    public Transform placedObjectsParent;

    // Static accessors so FarmPlotVisual can grab the ghost materials
    public static Material GhostValid   => Instance.ghostValidMaterial;
    public static Material GhostInvalid => Instance.ghostInvalidMaterial;

    private LayerMask    groundLayer;
    private PlaceableData currentData;
    private GameObject   ghostObject;
    private bool         isPlacing    = false;
    private int          ghostX, ghostZ;
    private bool         placementValid;
    private int          framesToSkip = 0;

    // ── Setup ─────────────────────────────────────────────────

    void Awake()
    {
        Instance   = this;
        groundLayer = LayerMask.GetMask("Ground");
    }

    // ── Public API ────────────────────────────────────────────

    public void BeginPlacement(PlaceableData data)
    {
        if (isPlacing) CancelPlacement();

        currentData  = data;
        isPlacing    = true;
        framesToSkip = 2;

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
        isPlacing   = false;
        currentData = null;
    }

    // ── Update ────────────────────────────────────────────────

    void Update()
    {
        if (ShopUI.IsOpen)
        {
            if (ghostObject) ghostObject.SetActive(false);
            return;
        }

        if (ghostObject) ghostObject.SetActive(true);
        if (!isPlacing) return;

        // Wait a couple of frames so the click that opened placement
        // doesn't immediately confirm it
        if (framesToSkip > 0)
        {
            framesToSkip--;
            return;
        }

        // Don't process clicks that land on a UI element
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 200f, groundLayer))
        {
            Vector3 snapped = GridManager.Instance.SnapToGrid(hit.point);
            ghostObject.transform.position = snapped;

            GridManager.Instance.GetCellCoords(snapped, out ghostX, out ghostZ);
            placementValid = GridManager.Instance.CanPlace(
                ghostX, ghostZ,
                currentData.gridWidth,
                currentData.gridHeight);

            SetGhostMaterials(placementValid ? ghostValidMaterial : ghostInvalidMaterial);
        }

        if (Input.GetMouseButtonDown(0) && placementValid)
            ConfirmPlacement();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
            TutorialConsole.Log("Placement cancelled.");
        }
    }

    // ── Placement ─────────────────────────────────────────────

    void ConfirmPlacement()
    {
        if (!GameManager.Instance.SpendCoins(currentData.unlockCost)) return;

        GameObject placed = Instantiate(currentData.prefab,
            ghostObject.transform.position, Quaternion.identity);

        // Keep the Hierarchy tidy by parenting under the designated group
        if (placedObjectsParent != null)
            placed.transform.SetParent(placedObjectsParent);

        GridManager.Instance.SetOccupied(
            ghostX, ghostZ,
            currentData.gridWidth,
            currentData.gridHeight,
            placed);

        foreach (var mb in placed.GetComponentsInChildren<MonoBehaviour>())
            mb.enabled = true;

        TutorialConsole.Log($"{currentData.placeableName} placed!");

        Destroy(ghostObject);
        isPlacing   = false;
        currentData = null;
    }

    // ── Helpers ───────────────────────────────────────────────

    void SetGhostMaterials(Material mat)
    {
        if (ghostObject == null || mat == null) return;
        foreach (var r in ghostObject.GetComponentsInChildren<Renderer>())
            r.material = mat;
    }
}