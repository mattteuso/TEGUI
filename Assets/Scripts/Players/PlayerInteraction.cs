using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactDistance = 2f;
    [SerializeField] private LayerMask interactLayer;
    [SerializeField] private Transform carryPoint;

    [Header("Movement Modifiers")]
    [SerializeField] private float carrySpeedMultiplier = 0.7f;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private PlayerMovementDefi playerMovement;

    private Rigidbody carriedRb;
    private Transform carriedTransform;
    private Transform originalParent;

    private float originalSpeed;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovementDefi>();

        if (playerMovement != null)
            originalSpeed = playerMovement.PlayerSpeed;
        else
            Debug.LogWarning("[PlayerInteraction] PlayerMovementDefi não encontrado.");
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

    // --------------------------------------------------
    void TryPickUp()
    {
        if (carryPoint == null)
        {
            Debug.LogWarning("[PlayerInteraction] CarryPoint não atribuído.");
            return;
        }

        Vector3 origin = transform.position + Vector3.up;
        Vector3 dir = transform.forward;

        if (!Physics.Raycast(origin, dir, out RaycastHit hit, interactDistance, interactLayer))
            return;

        Rigidbody rb = hit.collider.attachedRigidbody;
        if (rb == null)
            return;

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

        // Ajusta player
        if (playerMovement != null)
        {
            playerMovement.IsInteracting = true;
            playerMovement.CanRotate = false;
            playerMovement.PlayerSpeed = originalSpeed * carrySpeedMultiplier;
        }

        if (debugMode)
            Debug.Log("[PlayerInteraction] Objeto carregado: " + carriedTransform.name);
    }

    // --------------------------------------------------
    void DropObject()
    {
        if (carriedTransform == null)
            return;

        carriedTransform.SetParent(originalParent);

        carriedRb.isKinematic = false;
        carriedRb.detectCollisions = true;

        if (playerMovement != null)
        {
            playerMovement.IsInteracting = false;
            playerMovement.CanRotate = true;
            playerMovement.PlayerSpeed = originalSpeed;
        }

        carriedRb = null;
        carriedTransform = null;
        originalParent = null;

        if (debugMode)
            Debug.Log("[PlayerInteraction] Objeto solto");
    }
}
