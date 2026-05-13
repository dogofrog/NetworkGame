using UnityEngine;
using UnityEngine.Events;

public class PhysicalButton3D : MonoBehaviour
{
    [SerializeField] float pressDepth = 0.08f;
    [SerializeField] float animSpeed = 20f;
    [SerializeField] UnityEvent onClick;

    [Header("Hold (необязательно)")]
    [SerializeField] UnityEvent onHold;
    [SerializeField] float holdTime = 3f;

    Vector3 idleLocalPos;
    Vector3 targetLocalPos;
    bool interactable = true;
    bool mouseOver;
    bool pressing;
    bool holdFired;
    float pressStartTime;

    void Start()
    {
        idleLocalPos = transform.localPosition;
        targetLocalPos = idleLocalPos;
    }

    void Update()
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, Time.deltaTime * animSpeed);

        if (pressing && !holdFired && onHold != null && onHold.GetPersistentEventCount() > 0)
        {
            if (Time.unscaledTime - pressStartTime >= holdTime)
            {
                holdFired = true;
                onHold.Invoke();
                targetLocalPos = idleLocalPos;
            }
        }
    }

    void OnMouseEnter()
    {
        if (!interactable) return;
        mouseOver = true;
        if (!pressing)
            targetLocalPos = idleLocalPos + Vector3.up * pressDepth * 0.4f;
    }

    void OnMouseDown()
    {
        Debug.Log($"[PhysicalButton3D] OnMouseDown: {gameObject.name}");
        if (!interactable) return;
        pressing = true;
        holdFired = false;
        pressStartTime = Time.unscaledTime;
        targetLocalPos = idleLocalPos + Vector3.down * pressDepth;
    }

    void OnMouseUp()
    {
        if (!interactable || !pressing) return;
        pressing = false;

        if (!holdFired)
            onClick.Invoke();

        targetLocalPos = mouseOver ? idleLocalPos + Vector3.up * pressDepth * 0.4f : idleLocalPos;
    }

    void OnMouseExit()
    {
        mouseOver = false;
        if (!pressing)
            targetLocalPos = idleLocalPos;
    }

    public void SetInteractable(bool value)
    {
        interactable = value;
        if (!value) targetLocalPos = idleLocalPos;
    }
}
