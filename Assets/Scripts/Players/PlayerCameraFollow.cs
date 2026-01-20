using UnityEngine;

public class PlayerCameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Offset")]
    [SerializeField] private Vector3 offset = new Vector3(0, 5, -8);

    [Header("Follow Settings")]
    [SerializeField] private float smoothSpeed = 10f;

    private Vector3 velocity = Vector3.zero;

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            1f / smoothSpeed
        );
    }

    // Opcional: definir target via código
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}