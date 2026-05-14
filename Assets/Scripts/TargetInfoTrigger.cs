using UnityEngine;

// Вешается на префаб цели. OnMouseDown → показывает описание узла из CheckpointPopupUI.
// Требует: Collider (не Trigger) на объекте.
public class TargetInfoTrigger : MonoBehaviour
{
    GameController _game;
    CheckpointPopupUI _popup;

    void Awake()
    {
        _game  = FindObjectOfType<GameController>();
        _popup = FindObjectOfType<CheckpointPopupUI>(true); // true = искать в т.ч. неактивные
    }

    void OnMouseDown()
    {
        if (_game == null || _popup == null) return;

        var tp = GetComponent<TargetPoint>();
        if (tp == null) return;

        var info = _game.GetCheckpointInfo(tp.index);
        if (string.IsNullOrWhiteSpace(info.title) && string.IsNullOrWhiteSpace(info.clickBody)) return;

        _popup.Show(info.title, info.clickBody);
    }
}
