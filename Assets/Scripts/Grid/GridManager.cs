using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid Settings")]
    public int gridWidth  = 20;
    public int gridHeight = 20;
    public float cellSize = 1f;
    public Vector3 originOffset = Vector3.zero;

    private GridCell[,] cells;

    void Awake()
    {
        Instance = this;
        InitGrid();
    }

    void InitGrid()
    {
        cells = new GridCell[gridWidth, gridHeight];
        for (int x = 0; x < gridWidth; x++)
            for (int z = 0; z < gridHeight; z++)
                cells[x, z] = new GridCell(x, z, GetWorldPos(x, z));
    }

    public Vector3 GetWorldPos(int x, int z)
    {
        return originOffset + new Vector3(x * cellSize, 0, z * cellSize);
    }

    public Vector3 SnapToGrid(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt((worldPos.x - originOffset.x) / cellSize);
        int z = Mathf.RoundToInt((worldPos.z - originOffset.z) / cellSize);
        x = Mathf.Clamp(x, 0, gridWidth  - 1);
        z = Mathf.Clamp(z, 0, gridHeight - 1);
        return GetWorldPos(x, z);
    }

    public bool GetCellCoords(Vector3 worldPos, out int cx, out int cz)
    {
        cx = Mathf.RoundToInt((worldPos.x - originOffset.x) / cellSize);
        cz = Mathf.RoundToInt((worldPos.z - originOffset.z) / cellSize);
        return cx >= 0 && cx < gridWidth && cz >= 0 && cz < gridHeight;
    }

    public bool CanPlace(int x, int z, int w, int h)
    {
        for (int dx = 0; dx < w; dx++)
            for (int dz = 0; dz < h; dz++)
            {
                int nx = x + dx, nz = z + dz;
                if (nx < 0 || nx >= gridWidth || nz < 0 || nz >= gridHeight) return false;
                if (cells[nx, nz].IsOccupied) return false;
            }
        return true;
    }

    public void SetOccupied(int x, int z, int w, int h, GameObject obj)
    {
        for (int dx = 0; dx < w; dx++)
            for (int dz = 0; dz < h; dz++)
                cells[x + dx, z + dz].SetOccupied(obj);
    }

    public void ClearCells(int x, int z, int w, int h)
    {
        for (int dx = 0; dx < w; dx++)
            for (int dz = 0; dz < h; dz++)
                cells[x + dx, z + dz].Clear();
    }

    public GridCell GetCell(int x, int z) => cells[x, z];

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.5f, 1f, 0.5f, 0.25f);
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 pos = originOffset + new Vector3(x * cellSize, 0.01f, z * cellSize);
                Gizmos.DrawWireCube(pos, new Vector3(cellSize * 0.95f, 0f, cellSize * 0.95f));
            }
        }

        Gizmos.color = new Color(0.2f, 0.9f, 0.3f, 0.6f);
        Vector3 center = originOffset + new Vector3(
            (gridWidth  - 1) * cellSize * 0.5f,
            0.01f,
            (gridHeight - 1) * cellSize * 0.5f);
        Gizmos.DrawWireCube(center, new Vector3(gridWidth * cellSize, 0f, gridHeight * cellSize));
    }
}

public class GridCell
{
    public int X, Z;
    public Vector3 WorldPos;
    public bool IsOccupied { get; private set; }
    public GameObject OccupiedBy { get; private set; }

    public GridCell(int x, int z, Vector3 worldPos)
    {
        X = x; Z = z; WorldPos = worldPos;
    }

    public void SetOccupied(GameObject obj) { IsOccupied = true; OccupiedBy = obj; }
    public void Clear() { IsOccupied = false; OccupiedBy = null; }
}