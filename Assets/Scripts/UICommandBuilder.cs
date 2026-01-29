using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Feedback (optional)")]
    public TextMeshProUGUI statusText;

    private readonly List<Command> _queue = new();

    void Start()
    {
        // 1) Фикс "слетающего" wrapping: настраиваем через TMP_InputField, а не руками в дочернем TextInput
        ApplyInputWrapping();

        // Кнопки команд
        upBtn.onClick.AddListener(() => Add(Command.Up));
        downBtn.onClick.AddListener(() => Add(Command.Down));
        leftBtn.onClick.AddListener(() => Add(Command.Left));
        rightBtn.onClick.AddListener(() => Add(Command.Right));

        // Run/Reset
        runButton.onClick.AddListener(Run);
        resetButton.onClick.AddListener(ResetLevel);

        // В начале всё доступно
        SetInputLocked(false);
        SetStatus("Введите команды и нажмите Run.");
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
        commandInput.text = string.Join(" ", _queue.Select(ToToken));
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

        game.agent.Run(cmds);

        // Ждём окончание по времени (позже можем сделать событие onDone, но сейчас просто)
        StartCoroutine(FinishAfter(cmds.Count * game.agent.stepDuration));
    }

    IEnumerator FinishAfter(float waitTime)
    {
        yield return new WaitForSeconds(waitTime + 0.1f);

        // После выполнения: ввод всё ещё заблокирован, активна только Reset
        SetInputLocked(true);

        if (game.agent.IsAtGoal())
            SetStatus("🎉 Уровень пройден!");
        else
            SetStatus("Команды выполнены. Нажми Reset, чтобы попробовать ещё раз.");
    }

    void ResetLevel()
    {
        if (game == null || game.agent == null) return;

        _queue.Clear();
        SyncInputFromQueue();

        game.agent.PlaceAt(game.start);

        // Разблокируем ввод обратно
        SetInputLocked(false);
        SetStatus("Сброс. Введите новые команды.");
    }

    void SetInputLocked(bool locked)
    {
        // locked = true => ввод нельзя, только Reset активен
        if (commandInput) commandInput.interactable = !locked;

        if (upBtn) upBtn.interactable = !locked;
        if (downBtn) downBtn.interactable = !locked;
        if (leftBtn) leftBtn.interactable = !locked;
        if (rightBtn) rightBtn.interactable = !locked;

        if (runButton) runButton.interactable = !locked;
        if (resetButton) resetButton.interactable = true; // всегда можно сбросить
    }

    void SetStatus(string msg)
    {
        if (statusText) statusText.text = msg;
        Debug.Log(msg);
    }
}