using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCarrySystem : MonoBehaviour
{
    [Header("Configurações de Interação")]
    [SerializeField] private float interactDistance = 2.5f;
    [SerializeField] private LayerMask interactLayer;
    [SerializeField] private Transform carryPoint;
    [SerializeField] private float carrySpeedMultiplier = 0.6f;

    [Header("Configurações de Colisão")]
    [SerializeField] private LayerMask wallLayer; // Layer das paredes (ex.: "Wall" ou "Obstacle")
    [SerializeField] private float collisionCheckRadius = 0.5f; // Raio para verificar colisão

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private PlayerMovementDefi playerMovement;
    private Rigidbody carriedRb;
    private Transform carriedTransform;
    private Transform originalParent;
    private float originalSpeed;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovementDefi>();
    }

    private void Start()
    {
        if (playerMovement != null)
            originalSpeed = playerMovement.PlayerSpeed;
    }

    private void Update()
    {
        // Verifica colisão enquanto carregando (para evitar passagem por paredes)
        if (carriedTransform != null)
        {
            CheckForWallCollision();
        }
    }

    // =================================================================
    // INPUT SYSTEM CALLBACK
    // =================================================================
    public void OnGrab(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        if (carriedTransform == null)
            TryPickUp();
        else
            DropObject();
    }

    private void TryPickUp()
    {
        Vector3 origin = transform.position + Vector3.up;
        Vector3 dir = transform.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, interactDistance, interactLayer))
        {
            // Verifica se o objeto tem Rigidbody e a Tag correta
            Rigidbody rb = hit.collider.attachedRigidbody;
            if (rb != null && hit.collider.CompareTag("Carryable"))
            {
                PickUp(rb);
            }
        }
    }

    private void PickUp(Rigidbody rb)
    {
        carriedRb = rb;
        carriedTransform = rb.transform;
        originalParent = carriedTransform.parent;

        // --- AJUSTE DE COLISÃO ---
        // Mantemos detectCollisions = true para ele bater nas paredes enquanto você carrega
        carriedRb.isKinematic = true;
        carriedRb.interpolation = RigidbodyInterpolation.None;

        // Parentesco
        carriedTransform.SetParent(carryPoint);
        carriedTransform.localPosition = Vector3.zero;
        carriedTransform.localRotation = Quaternion.identity;

        // Ajusta estado do Player
        if (playerMovement != null)
        {
            playerMovement.IsInteracting = true;
            playerMovement.CanRotate = false; // Player olha apenas para frente
            playerMovement.PlayerSpeed = originalSpeed * carrySpeedMultiplier;
        }

        if (debugMode) Debug.Log($"[CarrySystem] Carregando: {carriedTransform.name}");
    }

    private void DropObject()
    {
        if (carriedTransform == null) return;

        // Solta o objeto
        carriedTransform.SetParent(originalParent);
        carriedRb.isKinematic = false;

        // Aplica uma pequena força para frente para não dropar "dentro" do player
        carriedRb.AddForce(transform.forward * 2f, ForceMode.Impulse);

        // Restaura Player
        if (playerMovement != null)
        {
            playerMovement.IsInteracting = false;
            playerMovement.CanRotate = true;
            playerMovement.PlayerSpeed = originalSpeed;
        }

        if (debugMode) Debug.Log("[CarrySystem] Objeto solto");

        carriedRb = null;
        carriedTransform = null;
    }

    // Novo método: Verifica se o objeto carregado está colidindo com paredes e solta se necessário
    private void CheckForWallCollision()
    {
        if (carriedTransform == null) return;

        // Usa OverlapSphere para detectar colisões com paredes
        Collider[] hits = Physics.OverlapSphere(carriedTransform.position, collisionCheckRadius, wallLayer);
        if (hits.Length > 0)
        {
            if (debugMode) Debug.Log("[CarrySystem] Objeto colidiu com parede — soltando automaticamente.");
            DropObject();
        }
    }
}