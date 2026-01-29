using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Доступные команды (БЕЗ WAIT)
public enum Command
{
    Up,
    Down,
    Left,
    Right
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

        foreach (var cmd in commands)
        {
            Vector2Int dir = cmd switch
            {
                Command.Up => Vector2Int.up,
                Command.Down => Vector2Int.down,
                Command.Left => Vector2Int.left,
                Command.Right => Vector2Int.right,
                _ => Vector2Int.zero
            };

            Vector2Int next = _cell + dir;

            // Проверка границ
            if (!InBounds(next))
            {
                Debug.Log("Выход за границу сетки!");
                break;
            }

            yield return MoveTo(next);

            if (IsAtGoal())
                break;
        }

        _busy = false;
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
    }

    private bool InBounds(Vector2Int c)
    {
        return c.x >= 0 && c.x < grid.width &&
               c.y >= 0 && c.y < grid.height;
    }
}