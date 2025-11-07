using Fusion;
using UnityEngine;

public class ObjectInteraction : NetworkBehaviour
{
    [Header("Referências")]
    [SerializeField] private Transform holdPoint;
    [SerializeField] private float interactDist = 3f;
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float carrySpeedMultiplier = 0.6f;
    [SerializeField] private Animator animator;

    private PlayerMovementDefi playerMovement;
    private CharacterController cc;

    private NetworkObject heldNetObject;
    private bool isInteracting;
    private bool axisLocked;
    private Vector3 lockedAxis;
    private Quaternion lockedRotation;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovementDefi>();
        cc = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (!Object.HasInputAuthority)
            return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!isInteracting)
                TryInteract();
            else
                StopInteraction();
        }

        if (isInteracting && heldNetObject != null)
            HandleMovement();
    }

    void TryInteract()
    {
        Debug.DrawRay(transform.position + Vector3.up * 0.5f, transform.forward * interactDist, Color.yellow, 2f);

        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, out RaycastHit hit, interactDist))
        {
            if (hit.collider.CompareTag("Interact"))
            {
                var netObj = hit.collider.GetComponent<NetworkObject>();
                if (netObj == null)
                {
                    Debug.LogWarning("Objeto Interact precisa ter NetworkObject e PushableNetworkController.");
                    return;
                }

                // Guardamos a referência localmente (NetworkObject)
                heldNetObject = netObj;

                // Pedimos ao dono do estado que comece a carregar esse objeto por este player
                // O RPC será executado na StateAuthority do objeto (onde a posição realmente é aplicada)
                RPC_RequestStartCarry(heldNetObject.Id, Object.Id);

                isInteracting = true;

                // bloqueia rotação e ativa modo de interação no player
                lockedRotation = transform.rotation;
                playerMovement.IsInteracting = true;
                playerMovement.CanRotate = false;
                playerMovement.PlayerSpeed *= carrySpeedMultiplier;

                // animações
                if (animator != null)
                {
                    animator.SetBool("isPushing", true);
                    animator.SetBool("PushingIdle", true);
                }

                Debug.Log("Pedido de interação enviado ao servidor para: " + heldNetObject.name);
            }
        }
        else
        {
            Debug.Log("Nenhum objeto atingido.");
        }
    }

    void StopInteraction()
    {
        if (heldNetObject != null)
        {
            // Pedimos ao dono do estado para parar de carregar esse objeto
            RPC_RequestStopCarry(heldNetObject.Id);
            heldNetObject = null;
        }

        isInteracting = false;
        axisLocked = false;

        // restaura controle do player
        playerMovement.IsInteracting = false;
        playerMovement.CanRotate = true;
        playerMovement.PlayerSpeed /= carrySpeedMultiplier;

        // animações reset
        if (animator != null)
        {
            animator.SetBool("isPushing", false);
            animator.SetBool("PushForward", false);
            animator.SetBool("PushBackward", false);
            animator.SetBool("PushRight", false);
            animator.SetBool("PushLeft", false);
            animator.SetBool("PushingIdle", false);
        }

        Debug.Log("Pedido de stop enviado ao servidor.");
    }

    void HandleMovement()
    {
        transform.rotation = lockedRotation;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Detecta qual eixo será usado
        if (!axisLocked && (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f))
        {
            lockedAxis = Mathf.Abs(h) > Mathf.Abs(v) ? Vector3.right : Vector3.forward;
            axisLocked = true;
        }

        Vector3 moveDir = Vector3.zero;
        if (axisLocked)
        {
            moveDir = (lockedAxis == Vector3.right) ? new Vector3(h, 0, 0)
                                                    : new Vector3(0, 0, v);
        }

        if (Mathf.Abs(h) < 0.1f && Mathf.Abs(v) < 0.1f)
        {
            axisLocked = false;
            ResetPushAnimations();
            if (animator != null) animator.SetBool("PushingIdle", true);
        }

        if (moveDir.sqrMagnitude > 0.01f)
        {
            cc.Move(moveDir.normalized * moveSpeed * Time.deltaTime);
            UpdatePushAnimations(moveDir);
        }
        else
        {
            ResetPushAnimations();
            if (animator != null) animator.SetBool("PushingIdle", true);
        }
    }

    void UpdatePushAnimations(Vector3 moveDir)
    {
        if (animator == null) return;

        bool forward = Vector3.Dot(moveDir, transform.forward) > 0.5f;
        bool backward = Vector3.Dot(moveDir, -transform.forward) > 0.5f;
        bool right = Vector3.Dot(moveDir, transform.right) > 0.5f;
        bool left = Vector3.Dot(moveDir, -transform.right) > 0.5f;

        animator.SetBool("PushingIdle", false);
        animator.SetBool("PushForward", forward);
        animator.SetBool("PushBackward", backward);
        animator.SetBool("PushRight", right);
        animator.SetBool("PushLeft", left);
    }

    void ResetPushAnimations()
    {
        if (animator == null) return;

        animator.SetBool("PushForward", false);
        animator.SetBool("PushBackward", false);
        animator.SetBool("PushRight", false);
        animator.SetBool("PushLeft", false);
    }

    // ---------------- RPCs que pedem para a StateAuthority do objeto agir ---------------- //

    // pedido enviado ao StateAuthority do objeto: comece a seguir o player (playerId)
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_RequestStartCarry(NetworkId objectId, NetworkId playerId)
    {
        var obj = Runner.FindObject(objectId);
        if (obj == null) return;

        // o PushableNetworkController existe no objeto e só age na StateAuthority
        var ctrl = obj.GetComponent<PushableNetworkController>();
        if (ctrl != null)
        {
            ctrl.StartCarrying(playerId);
            Debug.Log("[RPC] Server: start carrying object " + objectId + " by player " + playerId);
        }
    }

    // pedido enviado ao StateAuthority do objeto: pare de seguir
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_RequestStopCarry(NetworkId objectId)
    {
        var obj = Runner.FindObject(objectId);
        if (obj == null) return;

        var ctrl = obj.GetComponent<PushableNetworkController>();
        if (ctrl != null)
        {
            ctrl.StopCarrying();
            Debug.Log("[RPC] Server: stop carrying object " + objectId);
        }
    }
}
