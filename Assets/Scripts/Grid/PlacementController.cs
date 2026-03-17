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

    public static Material GhostValid   => Instance.ghostValidMaterial;
    public static Material GhostInvalid => Instance.ghostInvalidMaterial;

    private LayerMask     groundLayer;
    private PlaceableData currentData;
    private GameObject    ghostObject;
    private bool          isPlacing       = false;
    private bool          costAlreadyPaid = false;
    private int           ghostX, ghostZ;
    private bool          placementValid;
    private int           framesToSkip    = 0;
    private Camera        mainCamera;

    private int _lastPlacedX = -1;
    private int _lastPlacedZ = -1;

    public bool IsPlacing => isPlacing;

    // ── Setup ─────────────────────────────────────────────────

    void Awake()
    {
        Instance    = this;
        groundLayer = LayerMask.GetMask("Ground");
        mainCamera  = Camera.main;
    }

    // ── Public API ────────────────────────────────────────────

    public void BeginPlacement(PlaceableData data, bool paid = false)
    {
        if (isPlacing) CancelPlacement();

        _lastPlacedX    = -1;
        _lastPlacedZ    = -1;
        currentData     = data;
        isPlacing       = true;
        costAlreadyPaid = paid;
        framesToSkip    = 2;

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
        ghostObject     = null;
        isPlacing       = false;
        costAlreadyPaid = false;
        currentData     = null;
        _lastPlacedX    = -1;
        _lastPlacedZ    = -1;
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

        if (framesToSkip > 0)
        {
            framesToSkip--;
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

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

        // Hold to draw — only place if mouse has moved to a new cell
        if (Input.GetMouseButton(0) && placementValid)
        {
            if (ghostX != _lastPlacedX || ghostZ != _lastPlacedZ)
                ConfirmPlacement();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (costAlreadyPaid)
            {
                GameManager.Instance.AddCoins(currentData.unlockCost);
                TutorialConsole.Log($"Placement cancelled. {currentData.unlockCost} coins refunded.");
            }
            else
            {
                TutorialConsole.Log("Placement cancelled.");
            }
            CancelPlacement();
        }
    }

    // ── Confirm ───────────────────────────────────────────────

    void ConfirmPlacement()
    {
        if (!costAlreadyPaid)
        {
            if (!GameManager.Instance.SpendCoins(currentData.unlockCost)) return;
        }

        // Store placed coords before anything else so we can restore them
        // after BeginPlacement resets them
        int savedX = ghostX;
        int savedZ = ghostZ;

        _lastPlacedX = savedX;
        _lastPlacedZ = savedZ;

        GameObject placed = Instantiate(currentData.prefab,
            ghostObject.transform.position, Quaternion.identity);

        if (placedObjectsParent != null)
            placed.transform.SetParent(placedObjectsParent);

        GridManager.Instance.SetOccupied(
            ghostX, ghostZ,
            currentData.gridWidth,
            currentData.gridHeight,
            placed);

        var plot = placed.GetComponent<FarmPlot>();
        if (plot != null)
        {
            plot.GridX = ghostX;
            plot.GridZ = ghostZ;
        }

        foreach (var mb in placed.GetComponentsInChildren<MonoBehaviour>())
            mb.enabled = true;

        TutorialConsole.Log($"{currentData.placeableName} placed!");
        AudioManager.Play(SoundEvent.PlotPlaced);

        // Fully destroy ghost before continuing
        Destroy(ghostObject);
        ghostObject = null;

        PlaceableData justPlaced = currentData;
        isPlacing       = false;
        costAlreadyPaid = false;
        currentData     = null;

        if (GameManager.Instance.CanAfford(justPlaced.unlockCost))
        {
            BeginPlacement(justPlaced, paid: false);

            // BeginPlacement resets _lastPlaced to -1 which would allow
            // the same cell to fire again immediately — restore saved coords
            _lastPlacedX = savedX;
            _lastPlacedZ = savedZ;
        }
        else
        {
            TutorialConsole.Warn($"Not enough coins for another {justPlaced.placeableName}.");
        }
    }

    // ── Helpers ───────────────────────────────────────────────

    void SetGhostMaterials(Material mat)
    {
        if (ghostObject == null || mat == null) return;
        foreach (var r in ghostObject.GetComponentsInChildren<Renderer>())
            r.material = mat;
    }
}