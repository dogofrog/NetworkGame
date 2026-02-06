using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UICommandBuilder : MonoBehaviour
{
    [Header("Refs")]
    public GameController game;

    [Header("UI")]
    public TMP_InputField commandInput;
    public Button runButton;
    public Button resetButton;
    public Button upBtn;
    public Button downBtn;
    public Button leftBtn;
    public Button rightBtn;
    public Button whileBtn;
    public TextMeshProUGUI upCountText;
    public TextMeshProUGUI downCountText;
    public TextMeshProUGUI leftCountText;
    public TextMeshProUGUI rightCountText;
    public TextMeshProUGUI whileCountText;

    [Header("Feedback (optional)")]
    public TextMeshProUGUI statusText;

    private readonly List<Command> _queue = new();
    private bool _agentHooked;
    private bool _resetHolding;
    private bool _resetHoldTriggered;
    private float _resetHoldStart;
    private const float ResetHoldSeconds = 3f;

    void Start()
    {
        // 1) Фикс "слетающего" wrapping: настраиваем через TMP_InputField, а не руками в дочернем TextInput
        ApplyInputWrapping();

        // Кнопки команд
        upBtn.onClick.AddListener(() => Add(Command.Up));
        downBtn.onClick.AddListener(() => Add(Command.Down));
        leftBtn.onClick.AddListener(() => Add(Command.Left));
        rightBtn.onClick.AddListener(() => Add(Command.Right));
        if (whileBtn) whileBtn.onClick.AddListener(() => Add(Command.While));

        // Run/Reset
        runButton.onClick.AddListener(Run);
        resetButton.onClick.RemoveAllListeners();
        BindResetHoldHandlers();

        // В начале всё доступно
        SetInputLocked(false);
        SetStatus("Введите команды и нажмите Run.");
        RefreshButtonLimits();
    }

    void Update()
    {
        if (_agentHooked) return;
        if (game == null || game.agent == null) return;

        game.agent.OnRunFinished += OnRunFinished;
        _agentHooked = true;
    }

    void LateUpdate()
    {
        if (!_resetHolding || _resetHoldTriggered) return;

        if (Time.unscaledTime - _resetHoldStart >= ResetHoldSeconds)
        {
            _resetHoldTriggered = true;
            FullRestart();
        }
    }

    void ApplyInputWrapping()
    {
        if (commandInput == null) return;

        // Многострочный ввод (чтобы переносы реально работали)
        commandInput.lineType = TMP_InputField.LineType.MultiLineNewline;
        commandInput.readOnly = true;

        // Включаем перенос слов и не режем текст
        if (commandInput.textComponent != null)
        {
            commandInput.textComponent.enableWordWrapping = true;
            commandInput.textComponent.overflowMode = TextOverflowModes.Overflow;
        }
    }

    void Add(Command c)
    {
        if (!commandInput.interactable) return; // если заблокировано — не добавляем

        if (game != null && !game.TryConsume(c))
        {
            SetStatus($"Лимит команды {ToToken(c)} исчерпан.");
            RefreshButtonLimits();
            return;
        }

        _queue.Add(c);
        SyncInputFromQueue();
        RefreshButtonLimits();
    }

    void SyncInputFromQueue()
    {
        commandInput.text = BuildCompactText();
    }

    string ToToken(Command c) => c switch
    {
        Command.Up => "UP",
        Command.Down => "DOWN",
        Command.Left => "LEFT",
        Command.Right => "RIGHT",
        _ => ""
    };

    List<Command> ParseInput()
    {
        var text = commandInput.text ?? "";
        text = text.Replace("while (true):", "WHILE", System.StringComparison.OrdinalIgnoreCase);
        text = text.Replace("while(true):", "WHILE", System.StringComparison.OrdinalIgnoreCase);
        var tokens = text.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        var list = new List<Command>();

        foreach (var t in tokens)
        {
            switch (t.Trim().ToUpperInvariant())
            {
                case "UP": list.Add(Command.Up); break;
                case "DOWN": list.Add(Command.Down); break;
                case "LEFT": list.Add(Command.Left); break;
                case "RIGHT": list.Add(Command.Right); break;
                case "WHILE": list.Add(Command.While); break;
            }
        }

        return list;
    }

    void Run()
    {
        if (game == null || game.agent == null) return;
        if (game.agent.IsBusy) return;

        var cmds = ParseInput();
        if (cmds.Count == 0 && _queue.Count > 0)
            cmds = new List<Command>(_queue);

        if (cmds.Count == 0)
        {
            SetStatus("Добавь команды кнопками или текстом.");
            return;
        }

        // 2) На время выполнения — блокируем ввод, Reset оставляем активным
        SetInputLocked(true);
        SetStatus("Выполняю команды...");

        game.BeginRun();
        game.agent.Run(cmds);
    }

    void OnRunFinished()
    {
        if (game == null) return;

        switch (game.LastRunOutcome)
        {
            case GameController.RunOutcome.ReachedTarget:
            {
                int index = game.LastReachedTargetIndex + 1;
                SetStatus($"Вы достигли цели {index}. Введите новые команды.");

                _queue.Clear();
                SyncInputFromQueue();

                SetInputLocked(false);
                RefreshButtonLimits();
                break;
            }
            case GameController.RunOutcome.AllCompleted:
                SetInputLocked(true);
                SetStatus("🎉 Уровень пройден!");
                break;
            case GameController.RunOutcome.FellIntoPit:
                SetInputLocked(true);
                SetStatus("Вы провалились в яму. Нажми Reset, чтобы попробовать ещё раз.");
                break;
            case GameController.RunOutcome.Mistake:
                SetInputLocked(true);
                SetStatus("Ошибка: неправильная клетка. Нажми Reset, чтобы попробовать ещё раз.");
                break;
            default:
                SetInputLocked(false);
                SetStatus("Команды выполнены. Введите следующие.");
                RefreshButtonLimits();
                break;
        }
    }

    void ResetLevel()
    {
        if (game == null || game.agent == null) return;

        _queue.Clear();
        SyncInputFromQueue();

        game.ResetLevel();

        // Разблокируем ввод обратно
        SetInputLocked(false);
        SetStatus("Сброс. Введите новые команды.");
        RefreshButtonLimits();
    }

    void FullRestart()
    {
        if (game == null || game.agent == null) return;

        _queue.Clear();
        SyncInputFromQueue();

        game.FullRestart();

        SetInputLocked(false);
        SetStatus("Полный рестарт. Введите новые команды.");
        RefreshButtonLimits();
    }

    void BindResetHoldHandlers()
    {
        if (resetButton == null) return;

        var trigger = resetButton.GetComponent<EventTrigger>();
        if (trigger == null) trigger = resetButton.gameObject.AddComponent<EventTrigger>();

        trigger.triggers ??= new List<EventTrigger.Entry>();
        trigger.triggers.Clear();

        var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        down.callback.AddListener(_ => OnResetPointerDown());
        trigger.triggers.Add(down);

        var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        up.callback.AddListener(_ => OnResetPointerUp());
        trigger.triggers.Add(up);
    }

    void OnResetPointerDown()
    {
        _resetHolding = true;
        _resetHoldTriggered = false;
        _resetHoldStart = Time.unscaledTime;
    }

    void OnResetPointerUp()
    {
        if (!_resetHolding) return;

        _resetHolding = false;
        if (_resetHoldTriggered) return;

        ResetLevel();
    }

    void SetInputLocked(bool locked)
    {
        // locked = true => ввод нельзя, только Reset активен
        if (commandInput) commandInput.interactable = !locked;

        if (locked)
        {
            if (upBtn) upBtn.interactable = false;
            if (downBtn) downBtn.interactable = false;
            if (leftBtn) leftBtn.interactable = false;
            if (rightBtn) rightBtn.interactable = false;
            if (whileBtn) whileBtn.interactable = false;
        }
        else
        {
            RefreshButtonLimits();
        }

        if (runButton) runButton.interactable = !locked;
        if (resetButton) resetButton.interactable = true; // всегда можно сбросить
    }

    void SetStatus(string msg)
    {
        if (statusText) statusText.text = msg;
        Debug.Log(msg);
    }

    void RefreshButtonLimits()
    {
        if (game == null) return;

        int up = game.GetRemaining(Command.Up);
        int down = game.GetRemaining(Command.Down);
        int left = game.GetRemaining(Command.Left);
        int right = game.GetRemaining(Command.Right);
        int wh = game.GetRemaining(Command.While);

        if (upBtn) upBtn.interactable = up > 0;
        if (downBtn) downBtn.interactable = down > 0;
        if (leftBtn) leftBtn.interactable = left > 0;
        if (rightBtn) rightBtn.interactable = right > 0;
        if (whileBtn) whileBtn.interactable = wh > 0;

        if (upCountText) upCountText.text = $"x{up}";
        if (downCountText) downCountText.text = $"x{down}";
        if (leftCountText) leftCountText.text = $"x{left}";
        if (rightCountText) rightCountText.text = $"x{right}";
        if (whileCountText) whileCountText.text = $"x{wh}";
    }

    string BuildCompactText()
    {
        var tokens = BuildDisplayTokens();
        var parts = new List<string>(tokens.Count);
        foreach (var t in tokens)
        {
            string icon = t.isWhile
                ? $"⟳{DirGlyph(t.dir)}"
                : DirGlyph(t.cmd);
            if (t.count > 1)
                parts.Add($"{icon}×{t.count}");
            else
                parts.Add(icon);
        }

        return string.Join(" ", parts);
    }

    struct DisplayToken
    {
        public Command cmd;
        public Command dir;
        public bool isWhile;
        public int count;
    }

    List<DisplayToken> BuildDisplayTokens()
    {
        var raw = new List<DisplayToken>();

        for (int i = 0; i < _queue.Count; i++)
        {
            var cmd = _queue[i];
            if (cmd == Command.While && i + 1 < _queue.Count && IsDirection(_queue[i + 1]))
            {
                raw.Add(new DisplayToken { isWhile = true, dir = _queue[i + 1], count = 1 });
                i++;
                continue;
            }

            if (!IsDirection(cmd)) continue;
            raw.Add(new DisplayToken { cmd = cmd, isWhile = false, count = 1 });
        }

        var compressed = new List<DisplayToken>();
        foreach (var t in raw)
        {
            if (compressed.Count == 0)
            {
                compressed.Add(t);
                continue;
            }

            var last = compressed[compressed.Count - 1];
            if (last.isWhile == t.isWhile && last.dir == t.dir && last.cmd == t.cmd)
            {
                last.count += 1;
                compressed[compressed.Count - 1] = last;
            }
            else
            {
                compressed.Add(t);
            }
        }

        return compressed;
    }

    string DirGlyph(Command cmd)
    {
        return cmd switch
        {
            Command.Up => "↑",
            Command.Down => "↓",
            Command.Left => "←",
            Command.Right => "→",
            _ => "?"
        };
    }

    bool IsDirection(Command cmd)
    {
        return cmd == Command.Up || cmd == Command.Down || cmd == Command.Left || cmd == Command.Right;
    }
}
