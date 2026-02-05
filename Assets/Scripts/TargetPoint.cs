using UnityEngine;

public class TargetPoint : MonoBehaviour
{
    public int index;

    public void SetCompleted(bool completed)
    {
        // Hide the whole target when completed.
        gameObject.SetActive(!completed);
    }
}
