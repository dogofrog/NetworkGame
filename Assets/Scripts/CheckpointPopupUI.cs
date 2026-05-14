using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Попап с описанием устройства, появляется автоматически при достижении чекпоинта.
// CommandStation вызывает Show(title, body) и передаёт OnClosed-коллбэк.
public class CheckpointPopupUI : MonoBehaviour
{
    [Header("UI Refs")]
    public GameObject panel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;
    public Button closeButton;

    public System.Action OnClosed;

    void Awake()
    {
        if (closeButton) closeButton.onClick.AddListener(Hide);
        if (panel) panel.SetActive(false);
    }

    public void Show(string title, string body)
    {
        if (titleText) titleText.text = title;
        if (bodyText)  bodyText.text  = body;
        if (panel) panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel) panel.SetActive(false);
        var cb = OnClosed;
        OnClosed = null;
        cb?.Invoke();
    }
}
