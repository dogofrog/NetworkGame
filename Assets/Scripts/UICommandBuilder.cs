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

        _queue.Add(c);
        SyncInputFromQueue();
    }

    void SyncInputFromQueue()
    {
        var parts = new List<string>(_queue.Count);
        foreach (var cmd in _queue)
        {
            if (cmd == Command.While)
                parts.Add("while (true):");
            else
                parts.Add(ToToken(cmd));
        }

        commandInput.text = string.Join(" ", parts);
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
    }

    void FullRestart()
    {
        if (game == null || game.agent == null) return;

        _queue.Clear();
        SyncInputFromQueue();

        game.FullRestart();

        SetInputLocked(false);
        SetStatus("Полный рестарт. Введите новые команды.");
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

        if (upBtn) upBtn.interactable = !locked;
        if (downBtn) downBtn.interactable = !locked;
        if (leftBtn) leftBtn.interactable = !locked;
        if (rightBtn) rightBtn.interactable = !locked;
        if (whileBtn) whileBtn.interactable = !locked;

        if (runButton) runButton.interactable = !locked;
        if (resetButton) resetButton.interactable = true; // всегда можно сбросить
    }

    void SetStatus(string msg)
    {
        if (statusText) statusText.text = msg;
        Debug.Log(msg);
    }
}
