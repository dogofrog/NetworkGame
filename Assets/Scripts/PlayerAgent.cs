using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Доступные команды (БЕЗ WAIT)
public enum Command
{
    Up,
    Down,
    Left,
    Right,
    While
}

public class PlayerAgent : MonoBehaviour
{
    [Header("Grid")]
    public GridManager grid;

    [Header("Move")]
    public float stepDuration = 0.25f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Goal")]
    public Vector2Int GoalCell;

    private Vector2Int _cell;
    private bool _busy;

    public System.Action<Vector2Int> OnCellChanged;
    public System.Action OnRunFinished;
    public System.Func<bool> IsRunComplete;

    public Vector2Int Cell => _cell;
    public bool IsBusy => _busy;

    // Установка персонажа на клетку
    public void PlaceAt(Vector2Int cell)
    {
        _cell = cell;
        transform.position = grid.CellToWorld(cell);
    }

    // Проверка победы
    public bool IsAtGoal() => _cell == GoalCell;

    // Запуск команд
    public void Run(List<Command> commands)
    {
        if (_busy || commands == null || commands.Count == 0)
            return;

        StartCoroutine(RunRoutine(commands));
    }

    private IEnumerator RunRoutine(List<Command> commands)
    {
        _busy = true;
        bool stopAll = false;

        for (int i = 0; i < commands.Count; i++)
        {
            var cmd = commands[i];

            if (cmd == Command.While)
            {
                if (i + 1 < commands.Count && IsDirection(commands[i + 1]))
                {
                    var whileDir = ToDir(commands[i + 1]);
                    i++;
                    yield return MoveUntilBlocked(whileDir, () => stopAll = true);
                    if (stopAll) break;
                }
                continue;
            }

            if (!IsDirection(cmd))
                continue;

            Vector2Int dir = ToDir(cmd);

            Vector2Int next = _cell + dir;

            // Проверка границ
            if (!InBounds(next))
            {
                Debug.Log("Выход за границу сетки!");
                break;
            }

            if (grid != null && grid.IsBlockedByWall(_cell, next))
            {
                Debug.Log("Стена: движение заблокировано!");
                continue;
            }

            yield return MoveTo(next);

            if (IsRunComplete != null && IsRunComplete())
                break;
        }

        _busy = false;
        OnRunFinished?.Invoke();
    }

    private bool IsDirection(Command cmd)
    {
        return cmd == Command.Up || cmd == Command.Down || cmd == Command.Left || cmd == Command.Right;
    }

    private Vector2Int ToDir(Command cmd)
    {
        return cmd switch
        {
            Command.Up => Vector2Int.up,
            Command.Down => Vector2Int.down,
            Command.Left => Vector2Int.left,
            Command.Right => Vector2Int.right,
            _ => Vector2Int.zero
        };
    }

    private IEnumerator MoveUntilBlocked(Vector2Int dir, System.Action onStopAll)
    {
        while (true)
        {
            Vector2Int next = _cell + dir;

            if (!InBounds(next))
                yield break;

            if (grid != null && grid.IsBlockedByWall(_cell, next))
                yield break;

            yield return MoveTo(next);

            if (IsRunComplete != null && IsRunComplete())
            {
                onStopAll?.Invoke();
                yield break;
            }
        }
    }

    private IEnumerator MoveTo(Vector2Int nextCell)
    {
        Vector3 start = transform.position;
        Vector3 end = grid.CellToWorld(nextCell);
        float t = 0f;

        // Поворот по направлению движения
        Vector3 look = end - start;
        look.y = 0;
        if (look.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(look);

        while (t < 1f)
        {
            t += Time.deltaTime / stepDuration;
            transform.position = Vector3.Lerp(start, end, moveCurve.Evaluate(t));
            yield return null;
        }

        _cell = nextCell;
        OnCellChanged?.Invoke(_cell);
    }

    private bool InBounds(Vector2Int c)
    {
        return c.x >= 0 && c.x < grid.width &&
               c.y >= 0 && c.y < grid.height;
    }
}
