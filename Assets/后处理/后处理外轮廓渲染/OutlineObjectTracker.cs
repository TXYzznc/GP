using UnityEngine;

public class OutlineObjectTracker : MonoBehaviour
{
    public string profileName = "Enemy";
    private OutlineManager outlineManager;
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private Vector3 lastScale;

    private void Start()
    {
        outlineManager = OutlineManager.Instance;

        lastPosition = transform.position;
        lastRotation = transform.rotation;
        lastScale = transform.localScale;
    }

    private void LateUpdate()
    {
        if (HasTransformChanged())
        {
            InvalidateCache();
            UpdateLastTransform();
        }
    }

    private bool HasTransformChanged()
    {
        return lastPosition != transform.position ||
               lastRotation != transform.rotation ||
               lastScale != transform.localScale;
    }

    private void UpdateLastTransform()
    {
        lastPosition = transform.position;
        lastRotation = transform.rotation;
        lastScale = transform.localScale;
    }

    private void InvalidateCache()
    {
        if (outlineManager != null)
        {
            outlineManager.InvalidateCache(profileName);
        }
    }

    private void OnEnable()
    {
        InvalidateCache();
    }

    private void OnDisable()
    {
        InvalidateCache();
    }
}