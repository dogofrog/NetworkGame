using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Независимый попап памятки уровня. Вешается на любой GameObject в сцене.
// LevelIntroTrigger вызывает Show() при клике на button_Game.
public class LevelIntroUI : MonoBehaviour
{
    [Header("Refs")]
    public GameController game;
    public GameObject panel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;
    public Button closeButton;

    void Start()
    {
        if (closeButton) closeButton.onClick.AddListener(Hide);
        Hide();
    }

    public void Show()
    {
        if (game == null || !game.HasLevelIntro()) return;

        if (titleText) titleText.text = game.GetLevelIntroTitle();
        if (bodyText)  bodyText.text  = game.BuildLevelIntroBody();

        if (panel) panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel) panel.SetActive(false);
    }
}
