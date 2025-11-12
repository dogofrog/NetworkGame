using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Команды, которые можно задавать из UI
public enum Command { Up, Down, Left, Right, Wait }

public class PlayerAgent : MonoBehaviour
{
    [Header("Grid")]
    public GridManager grid;         // ссылка на менеджер сетки

    [Header("Move")]
    public float stepDuration = 0.25f;    // время одного шага
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Goal")]
    public Vector2Int GoalCell;      // куда нужно дойти (рыбка)
    private Vector2Int _cell;        // текущая клетка
    private bool _busy;              // выполняет ли сейчас команды

    public Vector2Int Cell => _cell;
    public bool IsBusy => _busy;

    /// Устанавливает персонажа на клетку
    public void PlaceAt(Vector2Int cell)
    {
        _cell = cell;
        if (grid != null)
            transform.position = grid.CellToWorld(cell);
        else
            Debug.LogWarning("PlayerAgent: grid не назначен.");
    }

    /// Проверка, достиг ли персонаж цели
    public bool IsAtGoal() => _cell == GoalCell;

    /// Запуск выполнения команд
    public void Run(List<Command> commands)
    {
        if (!_busy && commands != null && commands.Count > 0)
            StartCoroutine(RunRoutine(commands));
    }

    private IEnumerator RunRoutine(List<Command> commands)
    {
        _busy = true;

        foreach (var cmd in commands)
        {
            if (cmd == Command.Wait)
            {
                yield return new WaitForSeconds(stepDuration);
                continue;
            }

            Vector2Int dir = Vector2Int.zero;
            switch (cmd)
            {
                case Command.Up: dir = Vector2Int.up; break;
                case Command.Down: dir = Vector2Int.down; break;
                case Command.Left: dir = Vector2Int.left; break;
                case Command.Right: dir = Vector2Int.right; break;
            }

            Vector2Int next = _cell + dir;

            // Проверка границ
            if (!InBounds(next))
            {
                Debug.Log("Выход за границу сетки!");
                break;
            }

            // Движение
            yield return MoveTo(next);
        }

        _busy = false;
    }

    private IEnumerator MoveTo(Vector2Int nextCell)
    {
        Vector3 start = transform.position;
        Vector3 end = grid.CellToWorld(nextCell);
        float t = 0f;

        // поворот лицом по направлению движения
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
        int width = grid.width;
        int height = grid.height;
        return c.x >= 0 && c.x < width && c.y >= 0 && c.y < height;
    }
}