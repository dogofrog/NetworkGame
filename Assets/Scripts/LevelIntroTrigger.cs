using UnityEngine;

public class LevelIntroTrigger : MonoBehaviour
{
    public LevelIntroUI ui;

    void OnMouseDown() => ui?.Show();
}
