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
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float collisionCheckRadius = 0.5f;

    [Header("Animação")]
    [SerializeField] private Animator animator;

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
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        if (playerMovement != null)
            originalSpeed = playerMovement.PlayerSpeed;
    }

    private void Update()
    {
        if (carriedTransform != null)
        {
            CheckForWallCollision();
            HandleCarryAnimations(); // Gerencia as animações enquanto carrega
        }
    }

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

        carriedRb.isKinematic = true;
        carriedRb.interpolation = RigidbodyInterpolation.None;

        carriedTransform.SetParent(carryPoint);
        carriedTransform.localPosition = Vector3.zero;
        carriedTransform.localRotation = Quaternion.identity;

        if (playerMovement != null)
        {
            playerMovement.IsInteracting = true;
            playerMovement.CanRotate = false;
            playerMovement.PlayerSpeed = originalSpeed * carrySpeedMultiplier;
        }

        // Inicia estados de animação
        if (animator != null)
        {
            animator.SetBool("isPushing", true);
            animator.SetBool("PushingIdle", true);
        }

        if (debugMode) Debug.Log($"[CarrySystem] Carregando: {carriedTransform.name}");
    }

    private void DropObject()
    {
        if (carriedTransform == null) return;

        carriedTransform.SetParent(originalParent);
        carriedRb.isKinematic = false;
        carriedRb.AddForce(transform.forward * 2f, ForceMode.Impulse);

        if (playerMovement != null)
        {
            playerMovement.IsInteracting = false;
            playerMovement.CanRotate = true;
            playerMovement.PlayerSpeed = originalSpeed;
        }

        // Finaliza estados de animação
        if (animator != null)
        {
            animator.SetBool("isPushing", false);
            animator.SetBool("PushingIdle", false);
            ResetPushAnimations();
        }

        carriedRb = null;
        carriedTransform = null;
    }

    private void CheckForWallCollision()
    {
        if (carriedTransform == null) return;
        Collider[] hits = Physics.OverlapSphere(carriedTransform.position, collisionCheckRadius, wallLayer);
        if (hits.Length > 0) DropObject();
    }

    // =================================================================
    // LÓGICA DE ANIMAÇÃO (EXTRAÍDA DO SCRIPT 1)
    // =================================================================

    private void HandleCarryAnimations()
    {
        if (animator == null || playerMovement == null) return;

        CharacterController cc = GetComponent<CharacterController>();

        // Usamos cc.velocity para saber a direção REAL do movimento no mundo
        Vector3 moveDir = cc.velocity;

        // Se a velocidade for muito baixa, ficamos no Idle de empurrar
        if (moveDir.sqrMagnitude < 0.1f)
        {
            ResetPushAnimations();
            animator.SetBool("PushingIdle", true);
            return;
        }

        // A MÁGICA: Converte a velocidade do mundo para a "visão" do player
        // Se localMove.z > 0, ele está indo para a frente dele mesmo.
        // Se localMove.x > 0, ele está indo para a direita dele mesmo.
        Vector3 localMove = transform.InverseTransformDirection(moveDir);
        localMove.Normalize(); // Normalizamos para ter valores entre -1 e 1

        animator.SetBool("PushingIdle", false);

        // Usamos um threshold (0.3f) para permitir que animações laterais 
        // funcionem mesmo se o player estiver levemente inclinado
        bool forward = localMove.z > 0.3f;
        bool backward = localMove.z < -0.3f;
        bool right = localMove.x > 0.3f;
        bool left = localMove.x < -0.3f;

        animator.SetBool("PushForward", forward);
        animator.SetBool("PushBackward", backward);
        animator.SetBool("PushRight", right);
        animator.SetBool("PushLeft", left);
    }

    private void ResetPushAnimations()
    {
        if (animator == null) return;
        animator.SetBool("PushForward", false);
        animator.SetBool("PushBackward", false);
        animator.SetBool("PushRight", false);
        animator.SetBool("PushLeft", false);
    }
}