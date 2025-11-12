using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICommandBuilder : MonoBehaviour
{
    [Header("Refs")]
    public GameController game;   // перетащи сюда объект GameController

    [Header("UI")]
    public TMP_InputField commandInput;
    public Button runButton;
    public Button resetButton;
    public Button upBtn, downBtn, leftBtn, rightBtn, waitBtn;

    [Header("Feedback (optional)")]
    public TextMeshProUGUI statusText;

    private readonly List<Command> _queue = new();

    void Start()
    {
        upBtn.onClick.AddListener(()=> Add(Command.Up));
        downBtn.onClick.AddListener(()=> Add(Command.Down));
        leftBtn.onClick.AddListener(()=> Add(Command.Left));
        rightBtn.onClick.AddListener(()=> Add(Command.Right));
        waitBtn.onClick.AddListener(()=> Add(Command.Wait));

        runButton.onClick.AddListener(Run);
        resetButton.onClick.AddListener(ResetLevel);

        SetStatus("Введите команды и нажмите Run.");
    }

    void Add(Command c)
    {
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
        Command.Wait => "WAIT",
        _ => "WAIT"
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
                case "WAIT": list.Add(Command.Wait); break;
            }
        }
        return list;
    }

    void Run()
    {
        if (game == null || game.agent == null) return;

        var cmds = ParseInput();
        if (cmds.Count == 0 && _queue.Count > 0)
            cmds = new List<Command>(_queue);

        if (cmds.Count == 0)
        {
            SetStatus("Добавь команды через кнопки или введи текстом.");
            return;
        }

        runButton.interactable = false;
        resetButton.interactable = false;
        SetStatus("Выполняю команды...");

        game.agent.Run(cmds);
        StartCoroutine(WaitAndCheckEnd(cmds.Count * game.agent.stepDuration));
    }

    System.Collections.IEnumerator WaitAndCheckEnd(float wait)
    {
        yield return new WaitForSeconds(wait + 0.1f);
        runButton.interactable = true;
        resetButton.interactable = true;

        if (game.agent.IsAtGoal())
            SetStatus("🎉 Уровень пройден! Рыбка достигнута!");
        else
            SetStatus("Команды выполнены, но цель не достигнута.");
    }

    void ResetLevel()
    {
        if (game == null || game.agent == null) return;

        _queue.Clear();
        SyncInputFromQueue();
        game.agent.PlaceAt(game.start);
        SetStatus("Сброс. Готово к запуску.");
    }

    void SetStatus(string msg)
    {
        if (statusText) statusText.text = msg;
        Debug.Log(msg);
    }
}