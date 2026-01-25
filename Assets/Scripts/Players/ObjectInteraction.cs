using UnityEngine;

public class ObjectInteraction : MonoBehaviour
{
    [Header("Interact Settings")]
    [SerializeField] private float interactDistance = 2f;
    [SerializeField] private LayerMask interactLayer;
    [SerializeField] private Transform carryPoint;
    [SerializeField] private float carrySpeedMultiplier = 0.7f;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private PlayerMovementDefi playerMovement;

    private Rigidbody carriedRb;
    private Transform carriedTransform;
    private Transform originalParent;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovementDefi>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (carriedTransform == null)
                TryPickUp();
            else
                DropObject();
        }
    }

    void TryPickUp()
    {
        Vector3 origin = transform.position + Vector3.up;
        Vector3 dir = transform.forward;

        if (!Physics.Raycast(origin, dir, out RaycastHit hit, interactDistance, interactLayer))
            return;

        if (!hit.collider.CompareTag("Carryable"))
            return;

        Rigidbody rb = hit.collider.attachedRigidbody;
        if (rb == null) return;

        carriedRb = rb;
        carriedTransform = rb.transform;
        originalParent = carriedTransform.parent;

        // Desliga física
        carriedRb.isKinematic = true;
        carriedRb.detectCollisions = false;

        // Parent
        carriedTransform.SetParent(carryPoint);
        carriedTransform.localPosition = Vector3.zero;
        carriedTransform.localRotation = Quaternion.identity;

        // Ajustes no player (SEM método inventado)
        if (playerMovement != null)
        {
            playerMovement.IsInteracting = true;
            playerMovement.CanRotate = false;
            playerMovement.PlayerSpeed *= carrySpeedMultiplier;
        }

        if (debugMode)
            Debug.Log("[ObjectInteraction] Objeto pego: " + carriedTransform.name);
    }

    void DropObject()
    {
        if (carriedTransform == null)
            return;

        carriedTransform.SetParent(originalParent);

        carriedRb.isKinematic = false;
        carriedRb.detectCollisions = true;

        // Restaura player
        if (playerMovement != null)
        {
            playerMovement.IsInteracting = false;
            playerMovement.CanRotate = true;
            playerMovement.PlayerSpeed /= carrySpeedMultiplier;
        }

        carriedRb = null;
        carriedTransform = null;

        if (debugMode)
            Debug.Log("[ObjectInteraction] Objeto solto");
    }
}
