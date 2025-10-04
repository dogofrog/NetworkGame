using UnityEngine;

public class GridManager : MonoBehaviour
{
    public int width = 8;
    public int height = 8;
    public GameObject tilePrefab;

    public GameObject[,] gridArray;

    void Start()
    {
        gridArray = new GameObject[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 pos = new Vector3(x, 0, z);
                gridArray[x, z] = Instantiate(tilePrefab, pos, Quaternion.identity);
            }
        }
    }
}
