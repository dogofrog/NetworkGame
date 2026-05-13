using UnityEngine;

public class LevelIntroTrigger : MonoBehaviour
{
    [Header("Refs")]
    public UICommandBuilder uiCommandBuilder;
    public Animator animator;
    public Camera targetCamera;

    [Header("Animation")]
    public string openTriggerName = "Open";
    public bool debugClicks = true;

    Collider _collider;

    void Awake()
    {
        _collider = GetComponent<Collider>();
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (debugClicks)
            Debug.Log($"LevelIntroTrigger: Awake on {name}. Collider={_collider != null}, Camera={(targetCamera != null ? targetCamera.name : "null")}, UI={(uiCommandBuilder != null ? uiCommandBuilder.name : "null")}");
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        if (targetCamera == null || _collider == null)
        {
            if (debugClicks)
                Debug.LogWarning($"LevelIntroTrigger: click skipped on {name}. Camera={targetCamera != null}, Collider={_collider != null}");
            return;
        }

        Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
        bool hit = _collider.Raycast(ray, out var hitInfo, 1000f);
        if (debugClicks)
            Debug.Log($"LevelIntroTrigger: click raycast on {name}. Hit={hit}, Mouse={Input.mousePosition}");

        if (!hit)
            return;

        if (debugClicks)
            Debug.Log($"LevelIntroTrigger: collider hit on {name} at {hitInfo.point}");

        OpenIntro();
    }

    void OnMouseDown()
    {
        if (debugClicks)
            Debug.Log($"LevelIntroTrigger: OnMouseDown on {name}");

        OpenIntro();
    }

    public void OpenIntro()
    {
        if (debugClicks)
            Debug.Log($"LevelIntroTrigger: OpenIntro on {name}. UI assigned={uiCommandBuilder != null}");

        if (uiCommandBuilder == null)
        {
            Debug.LogWarning($"LevelIntroTrigger: UICommandBuilder is not assigned on {name}");
            return;
        }

        if (animator != null && !string.IsNullOrWhiteSpace(openTriggerName))
            animator.SetTrigger(openTriggerName);

        uiCommandBuilder.ShowLevelIntro();

        if (debugClicks)
            Debug.Log($"LevelIntroTrigger: ShowLevelIntro called from {name}");
    }
}
