using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Центральный контроллер ввода команд для 3D-интерфейса.
// Физические кнопки вызывают публичные методы через UnityEvent.
public class CommandStation : MonoBehaviour
{
    [Header("Core")]
    public GameController game;

    [Header("Displays (World Space TextMeshPro)")]
    public TextMeshPro commandsDisplay;
    public TextMeshPro logsDisplay;

    [Header("Physical Buttons (для блокировки)")]
    public PhysicalButton3D[] commandButtons;
    public PhysicalButton3D runButton;
    public PhysicalButton3D resetButton;

    readonly List<Command> _queue = new();
    bool _inputLocked;
    bool _agentHooked;

    void Update()
    {
        if (_agentHooked || game == null || game.agent == null) return;
        game.agent.OnRunFinished += OnRunFinished;
        _agentHooked = true;
    }

    // ── Публичные методы для физических кнопок ──────────────────────────

    public void AddUp()    => AddCommand(Command.Up);
    public void AddDown()  => AddCommand(Command.Down);
    public void AddLeft()  => AddCommand(Command.Left);
    public void AddRight() => AddCommand(Command.Right);
    public void AddWhile() => AddCommand(Command.While);

    public void TriggerRun()
    {
        Debug.Log($"[CommandStation] TriggerRun: locked={_inputLocked} queue={_queue.Count} agent={game?.agent != null}");
        if (_inputLocked || game == null || game.agent == null) return;
        if (game.agent.IsBusy || _queue.Count == 0) return;

        SetInputLocked(true);
        Log("Выполняю команды...");

        game.BeginRun();
        game.agent.Run(new List<Command>(_queue));
    }

    public void TriggerReset()
    {
        if (game == null || game.agent == null) return;

        _queue.Clear();
        game.ResetLevel();

        SetInputLocked(false);
        RefreshCommandDisplay();
        RefreshButtonStates();
        Log("Сброс. Введите новые команды.");
    }

    public void TriggerFullRestart()
    {
        if (game == null || game.agent == null) return;

        _queue.Clear();
        game.FullRestart();

        SetInputLocked(false);
        RefreshCommandDisplay();
        RefreshButtonStates();
        Log("Полный рестарт.");
    }

    // ── Внутренняя логика ────────────────────────────────────────────────

    void AddCommand(Command cmd)
    {
        if (_inputLocked) return;

        if (game != null && !game.TryConsume(cmd))
        {
            Log($"Лимит команды исчерпан.");
            RefreshButtonStates();
            return;
        }

        _queue.Add(cmd);
        RefreshCommandDisplay();
        RefreshButtonStates();
    }

    void OnRunFinished()
    {
        if (game == null) return;

        switch (game.LastRunOutcome)
        {
            case GameController.RunOutcome.ReachedTarget:
                int idx = game.LastReachedTargetIndex + 1;
                Log($"Цель {idx} достигнута. Введите новые команды.");
                _queue.Clear();
                SetInputLocked(false);
                RefreshCommandDisplay();
                RefreshButtonStates();
                break;

            case GameController.RunOutcome.AllCompleted:
                Log("Уровень пройден!");
                SetInputLocked(true);
                break;

            case GameController.RunOutcome.FellIntoPit:
                Log("Вы провалились в яму. Нажмите Reset.");
                SetInputLocked(true);
                break;

            case GameController.RunOutcome.Mistake:
                Log("Неверная клетка. Нажмите Reset.");
                SetInputLocked(true);
                break;

            default:
                SetInputLocked(false);
                RefreshButtonStates();
                break;
        }
    }

    void SetInputLocked(bool locked)
    {
        _inputLocked = locked;
        RefreshButtonStates();
    }

    void RefreshButtonStates()
    {
        if (runButton) runButton.SetInteractable(!_inputLocked && _queue.Count > 0);

        if (commandButtons == null) return;
        foreach (var btn in commandButtons)
        {
            if (btn == null) continue;
            btn.SetInteractable(!_inputLocked);
        }
    }

    void RefreshCommandDisplay()
    {
        if (commandsDisplay == null) return;
        commandsDisplay.text = BuildCompactText();
    }

    void Log(string msg)
    {
        if (logsDisplay) logsDisplay.text = msg;
        Debug.Log(msg);
    }

    string BuildCompactText()
    {
        var parts = new List<string>();
        for (int i = 0; i < _queue.Count; i++)
        {
            var cmd = _queue[i];
            if (cmd == Command.While && i + 1 < _queue.Count)
            {
                parts.Add("⟳" + Glyph(_queue[i + 1]));
                i++;
            }
            else
            {
                parts.Add(Glyph(cmd));
            }
        }

        // Сжатие: →→→ → →×3
        var compressed = new List<string>();
        int count = 1;
        for (int i = 1; i <= parts.Count; i++)
        {
            if (i < parts.Count && parts[i] == parts[i - 1])
            {
                count++;
            }
            else
            {
                compressed.Add(count > 1 ? $"{parts[i - 1]}×{count}" : parts[i - 1]);
                count = 1;
            }
        }

        return string.Join(" ", compressed);
    }

    string Glyph(Command cmd) => cmd switch
    {
        Command.Up    => "↑",
        Command.Down  => "↓",
        Command.Left  => "←",
        Command.Right => "→",
        _             => "?"
    };
}
