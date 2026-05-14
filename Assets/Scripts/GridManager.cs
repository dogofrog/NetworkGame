using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public enum WallOrientation
    {
        Vertical,   // стена между (x,y) и (x+1,y)
        Horizontal  // стена между (x,y) и (x,y+1)
    }

    [System.Serializable]
    public struct EdgeWall
    {
        public Vector2Int cell;
        public WallOrientation orientation;
    }

    [Header("Grid")]
    public int width = 8;
    public int height = 8;
    public float cellSize = 1f;
    public Vector3 origin = Vector3.zero;

    [Header("Level")]
    [Tooltip("Стенки между клетками (edge-walls)")]
    public List<EdgeWall> edgeWalls = new List<EdgeWall>();
    [Tooltip("Координаты ям (x,y) в пределах width/height")]
    public List<Vector2Int> pits = new List<Vector2Int>();

    [Header("Prefabs")]
    public GameObject tilePrefab;
    public GameObject wallPrefab;
    public float wallYOffset = 0.5f;
    public Material pitMaterial;      // материал для клетки-ямы
    public Color pitFallbackColor = Color.yellow;

    [Header("Hierarchy")]
    public Transform tilesRoot;
    public Transform wallsRoot;
    public Transform propsRoot;

    private GameObject[,] tiles;
    private readonly HashSet<EdgeKey> edgeWallSet = new();
    private readonly Dictionary<Vector2Int, Color> defaultTileColors = new();
    private readonly Dictionary<Vector2Int, Material[][]> defaultTileMaterials = new();
    private readonly HashSet<Vector2Int> pitSet = new();

    [Header("Editor")]
    public bool buildInEditMode = true;

    private struct EdgeKey
    {
        public Vector2Int a;
        public Vector2Int b;

        public EdgeKey(Vector2Int a, Vector2Int b)
        {
            if (a.x < b.x || (a.x == b.x && a.y <= b.y))
            {
                this.a = a;
                this.b = b;
            }
            else
            {
                this.a = b;
                this.b = a;
            }
        }
    }

    void Start()
    {
        Build();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying) return;
        if (!buildInEditMode) return;
        if (tilePrefab == null) return;
        UnityEditor.EditorApplication.delayCall += () => { if (this != null) Build(); };
    }

    [ContextMenu("Build Grid")]
    void BuildFromMenu()
    {
        if (Application.isPlaying) return;
        Build();
    }
#endif

    public void Build()
    {
        ClearAll();

        if (tilesRoot == null) tilesRoot = new GameObject("Tiles").transform;
        if (wallsRoot == null) wallsRoot = new GameObject("Walls").transform;
        if (propsRoot == null) propsRoot = new GameObject("Props").transform;

        tiles = new GameObject[width, height];
        edgeWallSet.Clear();
        defaultTileColors.Clear();
        defaultTileMaterials.Clear();
        pitSet.Clear();

        // тайлы
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                var pos = CellToWorld(new Vector2Int(x, z));
                tiles[x, z] = Instantiate(tilePrefab, pos, Quaternion.identity, tilesRoot);
                var cell = new Vector2Int(x, z);
                var renderers = GetTileRenderers(cell);
                if (renderers != null && renderers.Length > 0)
                {
                    defaultTileMaterials[cell] = CaptureSharedMaterials(renderers);
                    defaultTileColors[cell] = renderers[0].sharedMaterial.color;
                }
            }
        }

        foreach (var p in pits)
        {
            if (!InBounds(p)) continue;
            pitSet.Add(p);
            ApplyPitMaterial(p);
        }

        // стены между клетками
        foreach (var w in edgeWalls)
        {
            AddEdgeToSet(w);
            if (wallPrefab == null) continue;

            var wall = Instantiate(wallPrefab, wallsRoot);
            float yOffset = GetWallYOffset(wall);
            wall.transform.position = EdgeWallPosition(w, yOffset);
            wall.transform.rotation = EdgeWallRotation(w);
        }

    }

    public void ClearAll()
    {
        void Clear(Transform t)
        {
            if (t == null) return;
            for (int i = t.childCount - 1; i >= 0; i--)
                DestroyImmediate(t.GetChild(i).gameObject);
        }

        Clear(tilesRoot);
        Clear(wallsRoot);
        Clear(propsRoot);
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        return origin + new Vector3(cell.x * cellSize, 0f, cell.y * cellSize);
    }

    public bool InBounds(Vector2Int c) => c.x >= 0 && c.x < width && c.y >= 0 && c.y < height;

    public bool IsWalkable(Vector2Int c) => InBounds(c);

    public bool IsPit(Vector2Int c) => pitSet.Contains(c);

    void ApplyPitMaterial(Vector2Int cell)
    {
        var renderers = GetTileRenderers(cell);
        if (renderers == null) return;

        foreach (var r in renderers)
        {
            if (pitMaterial != null)
            {
                var mats = r.materials;
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = pitMaterial;
                r.materials = mats;

                var block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", pitMaterial.color);
                block.SetColor("_Color", pitMaterial.color);
                r.SetPropertyBlock(block);
            }
            else
            {
                var block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", pitFallbackColor);
                block.SetColor("_Color", pitFallbackColor);
                r.SetPropertyBlock(block);
            }
        }
    }

    public void SetCellColor(Vector2Int cell, Color color)
    {
        var renderers = GetTileRenderers(cell);
        if (renderers == null) return;
        foreach (var r in renderers)
            r.material.color = color;
    }

    public void ResetTileColors()
    {
        foreach (var pair in defaultTileMaterials)
            RestoreCellMaterials(pair.Key, pair.Value);

        foreach (var pair in defaultTileColors)
            SetCellColor(pair.Key, pair.Value);
    }

    Renderer GetTileRenderer(Vector2Int cell)
    {
        var renderers = GetTileRenderers(cell);
        if (renderers == null || renderers.Length == 0) return null;
        return renderers[0];
    }

    void SetCellMaterial(Vector2Int cell, Material material)
    {
        var renderers = GetTileRenderers(cell);
        if (renderers == null) return;
        foreach (var r in renderers)
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                mats[i] = material;
            r.sharedMaterials = mats;
        }
    }

    Renderer[] GetTileRenderers(Vector2Int cell)
    {
        if (!InBounds(cell) || tiles == null) return null;

        var tile = tiles[cell.x, cell.y];
        if (tile == null) return null;

        return tile.GetComponentsInChildren<Renderer>();
    }

    Material[][] CaptureSharedMaterials(Renderer[] renderers)
    {
        var captured = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
            captured[i] = (Material[])renderers[i].sharedMaterials.Clone();
        return captured;
    }

    void RestoreCellMaterials(Vector2Int cell, Material[][] materialsPerRenderer)
    {
        var renderers = GetTileRenderers(cell);
        if (renderers == null) return;

        int count = Mathf.Min(renderers.Length, materialsPerRenderer.Length);
        for (int i = 0; i < count; i++)
            renderers[i].sharedMaterials = materialsPerRenderer[i];
    }

    public bool IsBlockedByWall(Vector2Int from, Vector2Int to)
    {
        if (!InBounds(from) || !InBounds(to)) return true;
        if ((from - to).sqrMagnitude != 1) return false;

        var key = new EdgeKey(from, to);
        return edgeWallSet.Contains(key);
    }

    void AddEdgeToSet(EdgeWall w)
    {
        var a = w.cell;
        var b = w.orientation == WallOrientation.Vertical
            ? new Vector2Int(w.cell.x + 1, w.cell.y)
            : new Vector2Int(w.cell.x, w.cell.y + 1);

        if (!InBounds(a) || !InBounds(b)) return;
        edgeWallSet.Add(new EdgeKey(a, b));
    }

    Vector3 EdgeWallPosition(EdgeWall w, float yOffset)
    {
        var basePos = CellToWorld(w.cell);
        var offset = w.orientation == WallOrientation.Vertical
            ? new Vector3(cellSize * 0.5f, 0f, 0f)
            : new Vector3(0f, 0f, cellSize * 0.5f);

        return basePos + offset + new Vector3(0f, yOffset, 0f);
    }

    Quaternion EdgeWallRotation(EdgeWall w)
    {
        return w.orientation == WallOrientation.Vertical
            ? Quaternion.Euler(0f, 90f, 0f)
            : Quaternion.identity;
    }

    float GetWallYOffset(GameObject wall)
    {
        var rend = wall.GetComponentInChildren<Renderer>();
        if (rend != null)
            return rend.bounds.size.y * 0.5f;

        return wallYOffset;
    }
}
