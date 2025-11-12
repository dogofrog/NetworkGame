using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid")]
    public int width = 8;
    public int height = 8;
    public float cellSize = 1f;
    public Vector3 origin = Vector3.zero;

    [Header("Level")]
    public Vector2Int start = new Vector2Int(0, 0);
    public Vector2Int goal  = new Vector2Int(7, 7);
    [Tooltip("Координаты стен (x,z) в пределах width/height")]
    public List<Vector2Int> walls = new List<Vector2Int>();

    [Header("Prefabs")]
    public GameObject tilePrefab;
    public GameObject wallPrefab;     // куб 1x2x1
    public GameObject goalPrefab;     // «компьютер»
    public GameObject startMarkerPrefab;

    [Header("Hierarchy")]
    public Transform tilesRoot;
    public Transform wallsRoot;
    public Transform propsRoot;

    private GameObject[,] tiles;
    private HashSet<Vector2Int> wallSet = new HashSet<Vector2Int>();

    void Start()
    {
        Build();
    }

    public void Build()
    {
        ClearAll();

        if (tilesRoot == null) tilesRoot = new GameObject("Tiles").transform;
        if (wallsRoot == null) wallsRoot = new GameObject("Walls").transform;
        if (propsRoot == null) propsRoot = new GameObject("Props").transform;

        tiles = new GameObject[width, height];
        wallSet.Clear();
        foreach (var w in walls) wallSet.Add(w);

        // тайлы
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                var pos = CellToWorld(new Vector2Int(x, z));
                tiles[x, z] = Instantiate(tilePrefab, pos, Quaternion.identity, tilesRoot);
            }
        }

        // стены
        foreach (var w in wallSet)
        {
            var wall = Instantiate(wallPrefab, wallsRoot);
            wall.transform.position = CellToWorld(w) + Vector3.up; // центр куба на высоте 1
        }

        // цель
        if (goalPrefab != null)
        {
            var g = Instantiate(goalPrefab, propsRoot);
            g.transform.position = CellToWorld(goal);
        }

        // старт
        if (startMarkerPrefab != null)
        {
            var s = Instantiate(startMarkerPrefab, propsRoot);
            s.transform.position = CellToWorld(start);
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

    public bool IsWall(Vector2Int c) => wallSet.Contains(c);

    public bool IsWalkable(Vector2Int c) => InBounds(c) && !IsWall(c);
}