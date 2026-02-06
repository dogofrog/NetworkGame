using UnityEngine;
using System.Collections.Generic;

public class GameController : MonoBehaviour
{
    [Header("Refs")]
    public GridManager grid;

    [Header("Prefabs")]
    public GameObject characterPrefab; // Префаб персонажа (Character)
    public List<GameObject> targetPrefabs = new(); // 3 префаба целей по порядку

    [Header("Level Settings")]
    public Vector2Int start = new Vector2Int(0, 0);
    public List<Vector2Int> targetCells = new()
    {
        new Vector2Int(3, 3),
        new Vector2Int(5, 5),
        new Vector2Int(7, 7)
    };

    [Header("Spawn Roots (optional)")]
    public Transform charactersRoot;
    public Transform propsRoot;
    public Transform targetsRoot;

    [HideInInspector] public PlayerAgent agent;
    [HideInInspector] public List<Transform> targetTransforms = new();

    private int _currentTargetIndex;
    private bool _reachedTargetThisRun;
    private bool _hasCheckpoint;
    private Vector2Int _checkpointCell;
    private bool _fellIntoPitThisRun;
    private int _currentCheckpointIndex;
    private CommandLimits _remainingLimits;

    public enum RunOutcome
    {
        None,
        ReachedTarget,
        FellIntoPit,
        Mistake,
        AllCompleted
    }

    public RunOutcome LastRunOutcome { get; private set; } = RunOutcome.None;
    public int LastReachedTargetIndex { get; private set; } = -1;

    [System.Serializable]
    public struct CommandLimits
    {
        public int up;
        public int down;
        public int left;
        public int right;
        public int @while;
    }

    [Header("Command Limits (per checkpoint index)")]
    public List<CommandLimits> checkpointLimits = new();

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
        if (targetsRoot == null) targetsRoot = new GameObject("Targets").transform;

        // === СПАВН ПЕРСОНАЖА ===
        var charGO = Instantiate(characterPrefab, charactersRoot);
        agent = charGO.GetComponent<PlayerAgent>();
        if (agent == null) agent = charGO.AddComponent<PlayerAgent>();
        agent.grid = grid;
        agent.PlaceAt(start);
        agent.OnCellChanged += OnAgentCellChanged;
        agent.OnRunFinished += OnAgentRunFinished;
        agent.IsRunComplete = () => _reachedTargetThisRun || AllTargetsCompleted;

        // === СПАВН ЦЕЛЕЙ ===
        SpawnTargets();
        ResetTargetsState();
    }

    void SpawnTargets()
    {
        targetTransforms.Clear();

        for (int i = 0; i < targetCells.Count; i++)
        {
            var prefab = (i < targetPrefabs.Count) ? targetPrefabs[i] : null;
            if (prefab == null) continue;

            var go = Instantiate(prefab, targetsRoot);
            go.transform.position = grid.CellToWorld(targetCells[i]);

            var tp = go.GetComponent<TargetPoint>();
            if (tp == null) tp = go.AddComponent<TargetPoint>();
            tp.index = i;

            targetTransforms.Add(go.transform);
        }
    }

    void OnAgentCellChanged(Vector2Int cell)
    {
        if (grid != null && grid.IsPit(cell))
        {
            _fellIntoPitThisRun = true;
            return;
        }

        if (_currentTargetIndex >= targetCells.Count) return;
        if (cell != targetCells[_currentTargetIndex]) return;

        MarkTargetCompleted(_currentTargetIndex);
        CreateCheckpoint(cell);
        _reachedTargetThisRun = true;
        LastReachedTargetIndex = _currentTargetIndex;
        _currentTargetIndex++;
        _currentCheckpointIndex = _currentTargetIndex;
        ResetLimitsForCurrentCheckpoint();

        if (_currentTargetIndex < targetCells.Count)
            agent.GoalCell = targetCells[_currentTargetIndex];
    }

    void OnAgentRunFinished()
    {
        if (_fellIntoPitThisRun)
        {
            LastRunOutcome = RunOutcome.FellIntoPit;
            return;
        }

        if (_reachedTargetThisRun)
        {
            LastRunOutcome = AllTargetsCompleted ? RunOutcome.AllCompleted : RunOutcome.ReachedTarget;
            return;
        }

        if (AllTargetsCompleted)
        {
            LastRunOutcome = RunOutcome.AllCompleted;
            return;
        }

        // Не достигли нужной цели => ошибка
        if (_currentTargetIndex < targetCells.Count && agent.Cell != targetCells[_currentTargetIndex])
            LastRunOutcome = RunOutcome.Mistake;
    }

    void MarkTargetCompleted(int index)
    {
        if (index < 0 || index >= targetTransforms.Count) return;

        var tp = targetTransforms[index].GetComponent<TargetPoint>();
        if (tp != null)
        {
            tp.SetCompleted(true);
        }
        else
        {
            targetTransforms[index].gameObject.SetActive(false);
        }
    }

    void ResetTargetsState()
    {
        _currentTargetIndex = 0;
        _reachedTargetThisRun = false;
        _fellIntoPitThisRun = false;
        LastRunOutcome = RunOutcome.None;
        LastReachedTargetIndex = -1;
        _hasCheckpoint = false;
        _currentCheckpointIndex = 0;
        ResetLimitsForCurrentCheckpoint();
        grid.ResetTileColors();

        for (int i = 0; i < targetTransforms.Count; i++)
        {
            var tp = targetTransforms[i].GetComponent<TargetPoint>();
            if (tp != null) tp.SetCompleted(false);
            else targetTransforms[i].gameObject.SetActive(true);
        }

        if (targetCells.Count > 0)
            agent.GoalCell = targetCells[0];
    }

    public bool AllTargetsCompleted => _currentTargetIndex >= targetCells.Count && targetCells.Count > 0;

    public void BeginRun()
    {
        _reachedTargetThisRun = false;
        _fellIntoPitThisRun = false;
        LastRunOutcome = RunOutcome.None;
        LastReachedTargetIndex = -1;
    }

    public void ResetLevel()
    {
        if (agent == null) return;

        if (_hasCheckpoint)
        {
            agent.PlaceAt(_checkpointCell);
            ResetLimitsForCurrentCheckpoint();
        }
        else
        {
            agent.PlaceAt(start);
            ResetTargetsState();
        }
    }

    public void FullRestart()
    {
        if (agent == null) return;

        ResetTargetsState();
        agent.PlaceAt(start);
    }

    public bool TryConsume(Command cmd)
    {
        ref int remaining = ref GetRemainingRef(cmd);
        if (remaining <= 0) return false;
        remaining--;
        return true;
    }

    public int GetRemaining(Command cmd)
    {
        return GetRemainingRef(cmd);
    }

    public void ResetLimitsForCurrentCheckpoint()
    {
        _remainingLimits = GetLimitsForCheckpoint(_currentCheckpointIndex);
    }

    CommandLimits GetLimitsForCheckpoint(int index)
    {
        if (checkpointLimits == null || checkpointLimits.Count == 0)
            return UnlimitedLimits();

        if (index < 0) index = 0;
        if (index >= checkpointLimits.Count) index = checkpointLimits.Count - 1;

        return checkpointLimits[index];
    }

    CommandLimits UnlimitedLimits()
    {
        return new CommandLimits
        {
            up = int.MaxValue,
            down = int.MaxValue,
            left = int.MaxValue,
            right = int.MaxValue,
            @while = int.MaxValue
        };
    }

    ref int GetRemainingRef(Command cmd)
    {
        switch (cmd)
        {
            case Command.Up: return ref _remainingLimits.up;
            case Command.Down: return ref _remainingLimits.down;
            case Command.Left: return ref _remainingLimits.left;
            case Command.Right: return ref _remainingLimits.right;
            case Command.While: return ref _remainingLimits.@while;
            default: return ref _remainingLimits.up;
        }
    }

    void CreateCheckpoint(Vector2Int cell)
    {
        _hasCheckpoint = true;
        _checkpointCell = cell;
        grid.SetCellColor(cell, Color.red);
    }
}
