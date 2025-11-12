using UnityEngine;

public class GameController : MonoBehaviour
{
    [Header("Refs")]
    public GridManager grid;

    [Header("Prefabs")]
    public GameObject characterPrefab; // Префаб персонажа (Character)
    public GameObject fishPrefab;      // Префаб цели (Fish)

    [Header("Level Settings")]
    public Vector2Int start = new Vector2Int(0, 0);
    public Vector2Int goal = new Vector2Int(7, 7);

    [Header("Spawn Roots (optional)")]
    public Transform charactersRoot;
    public Transform propsRoot;

    [HideInInspector] public PlayerAgent agent;
    [HideInInspector] public Transform fishTransform;

    void Start()
    {
        if (grid == null)
        {
            Debug.LogError("GameController: не назначен GridManager!");
            return;
        }

        // создаём контейнеры (если их нет)
        if (charactersRoot == null) charactersRoot = new GameObject("Characters").transform;
        if (propsRoot == null) propsRoot = new GameObject("Props").transform;

        // === СПАВН ПЕРСОНАЖА ===
        var charGO = Instantiate(characterPrefab, charactersRoot);
        agent = charGO.GetComponent<PlayerAgent>();
        if (agent == null) agent = charGO.AddComponent<PlayerAgent>();
        agent.grid = grid;
        agent.PlaceAt(start);
        agent.GoalCell = goal;

        // === СПАВН РЫБКИ ===
        var fishGO = Instantiate(fishPrefab, propsRoot);
        fishTransform = fishGO.transform;
        fishTransform.position = grid.CellToWorld(goal);
    }
}