using UnityEngine;
using Fusion;

[RequireComponent(typeof(CharacterController))]
public class LedgeGrab : NetworkBehaviour
{
    [Header("Referências")]
    private CharacterController controller;
    private PlayerMovement playerMovement;
    private Animator animator;

    [Header("Configurações")]
    public float climbUpHeight = 1.5f;
    public float climbUpSpeed = 3f;
    public float moveSpeed = 2f;
    public LayerMask ledgeLayer;
    public float rayLength = 0.8f;
    public float rayHeight = 1.5f;
    public bool autoGrab = true;
    public float grabHeightOffset = 0.1f;
    public float grabForwardOffset = 0.25f;
    public int rayAmount = 5;
    public float rayOffset = 0.15f;
    public float lateralRayLength = 0.5f;
    public float lateralRayOffset = 0.1f;

    [Header("Ajustes adicionais")]
    [Tooltip("Offset lateral (direita/esquerda) pro ray de continuidade (amarelo)")]
    public float ledgeContinuityLateralOffset = 0.25f;
    [Tooltip("Tempo mínimo antes de poder agarrar novamente")]
    public float grabCooldown = 0.5f;
    [Tooltip("Delay entre o input do jump e o movimento físico acontecer (para sincronizar com a animação)")]
    public float jumpDelay = 0.2f; // Delay para o movimento acompanhar a animação

    [Header("Controles")]
    public KeyCode releaseKey = KeyCode.LeftShift;

    // Estados
    private bool isGrabbing;
    private bool canGrab;
    private bool grabBlocked;
    private Vector3 ledgePosition;
    private Quaternion grabRotation;
    private RaycastHit lastLedgeHit;
    private float grabMoveBlend;

    // Input cache
    private float _horizontalInput;
    private bool _jumpPressed;
    private bool _releasePressed;

    // Debug
    private Vector3 debugRayOrigin;
    private Vector3 debugRayEnd;
    private bool debugCanGrab;

    public override void Spawned()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (!HasInputAuthority)
            return;

        _horizontalInput = Input.GetAxisRaw("Horizontal");
        if (Input.GetButtonDown("Jump"))
            _jumpPressed = true;
        if (Input.GetKeyDown(releaseKey))
            _releasePressed = true;

        // Debug visual
        if (controller.isGrounded)
        {
            debugCanGrab = false;
            return;
        }

        debugCanGrab = false;
        for (int i = 0; i < rayAmount; i++)
        {
            Vector3 rayOrigin = transform.position + Vector3.up * rayHeight + Vector3.up * rayOffset * i;
            RaycastHit hit;
            bool rayHit = Physics.Raycast(rayOrigin, transform.forward, out hit, rayLength, ledgeLayer);

            if (rayHit && hit.collider.CompareTag("Ledge"))
            {
                debugCanGrab = true;
                debugRayOrigin = rayOrigin;
                debugRayEnd = hit.point;
                Debug.DrawLine(rayOrigin, hit.point, Color.green, 0.1f);
                break;
            }
            else
            {
                Debug.DrawLine(rayOrigin, rayOrigin + transform.forward * rayLength, Color.red, 0.1f);
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority)
            return;

        // verifica possibilidade de agarrar
        if (!isGrabbing && !controller.isGrounded && !grabBlocked)
        {
            for (int i = 0; i < rayAmount; i++)
            {
                Vector3 rayOrigin = transform.position + Vector3.up * rayHeight + Vector3.up * rayOffset * i;
                RaycastHit hit;

                if (Physics.Raycast(rayOrigin, transform.forward, out hit, rayLength, ledgeLayer))
                {
                    if (hit.collider.CompareTag("Ledge"))
                    {
                        lastLedgeHit = hit;
                        ledgePosition = hit.point;
                        canGrab = true;

                        if (autoGrab)
                            StartGrab();
                        break;
                    }
                }
            }

            if (!canGrab)
                canGrab = false;
        }

        if (isGrabbing)
        {
            HandleLedgeMovement();

            // Ajustado: Inicia animação imediatamente e delay para o movimento
            if (_jumpPressed)
            {
                if (animator)
                {
                    animator.SetTrigger("GrabJump");
                    animator.SetBool("IsGrabIdle", false);
                }
                Runner.StartCoroutine(JumpDelayRoutine());
                _jumpPressed = false; // Reseta para evitar múltiplos
            }

            if (_releasePressed)
                ReleaseLedge();
        }

        _jumpPressed = false;
        _releasePressed = false;
    }

    // Ajustado: Coroutine para delay do movimento (após animação iniciar)
    private System.Collections.IEnumerator JumpDelayRoutine()
    {
        Debug.Log("Animação de jump iniciada - Aguardando delay para movimento...");
        yield return new WaitForSeconds(jumpDelay); // Aguarda o delay para sincronizar com a animação
        if (isGrabbing) // Verifica se ainda está agarrando
        {
            Runner.StartCoroutine(ClimbUp());
            Debug.Log("Movimento de jump executado após delay!");
        }
    }

    private void StartGrab()
    {
        isGrabbing = true;
        canGrab = false;
        grabBlocked = false;

        controller.enabled = false;
        playerMovement.enabled = false;

        // alinha o player em relação a normal do objeto
        grabRotation = Quaternion.LookRotation(-lastLedgeHit.normal, Vector3.up);
        transform.rotation = grabRotation;

        // ajusta a posicao baseado na normal
        Vector3 grabPos = lastLedgeHit.point
            + Vector3.up * grabHeightOffset
            - grabRotation * Vector3.forward * grabForwardOffset;
        transform.position = grabPos;

        Debug.Log("Agarrou em: " + grabPos);

        //animacao

        if (animator)
        {
            animator.SetTrigger("Grab");
            animator.SetBool("IsGrabbing", true);
            animator.SetBool("IsGrabIdle", true);
        }
    }

    private void ReleaseLedge()
    {
        isGrabbing = false;
        controller.enabled = true;
        playerMovement.enabled = true;

        if (animator)
        {
            animator.SetBool("IsGrabbing", false);
            animator.SetBool("IsGrabIdle", false);
            animator.SetBool("IsGrabWalkingLeft", false);
            animator.SetBool("IsGrabWalkingRight", false);
        }

        transform.position -= transform.forward * 0.1f;

        Debug.Log("Soltou da borda!");
        Runner.StartCoroutine(GrabCooldownRoutine()); // delay
    }

    private void HandleLedgeMovement()
    {
        transform.rotation = grabRotation;

        if (Mathf.Abs(_horizontalInput) > 0.1f)
        {
            Vector3 moveDir = transform.right * _horizontalInput * moveSpeed * Runner.DeltaTime;
            Vector3 lateralDirection = _horizontalInput > 0 ? transform.right : -transform.right;

            Vector3 collisionRayOrigin = transform.position + Vector3.up * lateralRayOffset;
            RaycastHit collisionHit;

            Debug.DrawRay(collisionRayOrigin, lateralDirection * lateralRayLength, Color.blue, 0.1f);

            if (Physics.Raycast(collisionRayOrigin, lateralDirection, out collisionHit, lateralRayLength))
            {
                if (!collisionHit.collider.CompareTag("Ledge"))
                    return;
            }

            // RAY AMARELO
            Vector3 lateralOffsetDir = _horizontalInput > 0 ? transform.right : -transform.right;
            Vector3 ledgeCheckRayOrigin =
                transform.position + moveDir +
                Vector3.up * lateralRayOffset +
                lateralOffsetDir * ledgeContinuityLateralOffset; // offset lateral

            RaycastHit ledgeCheckHit;
            float continuityRayLength = rayLength * 0.8f;

            Debug.DrawRay(ledgeCheckRayOrigin, transform.forward * continuityRayLength, Color.yellow, 0.1f);

            if (!Physics.Raycast(ledgeCheckRayOrigin, transform.forward, out ledgeCheckHit, continuityRayLength, ledgeLayer)
                || !ledgeCheckHit.collider.CompareTag("Ledge"))
                return;

            transform.position += moveDir;

            if (animator)
            {
                animator.SetBool("IsGrabIdle", false);
                animator.SetBool("IsGrabWalkingLeft", _horizontalInput < 0);
                animator.SetBool("IsGrabWalkingRight", _horizontalInput > 0);
            }
        }

        else
        {
            if (animator)
            {
                animator.SetBool("IsGrabIdle", true);
                animator.SetBool("IsGrabWalkingLeft", false);
                animator.SetBool("IsGrabWalkingRight", false);
            }
        }
    }

    private System.Collections.IEnumerator ClimbUp()
    {
        // Removido: O trigger "GrabJump" agora é setado no FixedUpdateNetwork para sincronizar com a animação
        // if (animator) { animator.SetTrigger("GrabJump"); animator.SetBool("IsGrabIdle", false); }

        float t = 0f;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.up * climbUpHeight;

        while (t < 1f)
        {
            t += Time.deltaTime * climbUpSpeed;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        controller.enabled = true;
        playerMovement.enabled = true;
        isGrabbing = false;

        if (animator)
        {
            animator.SetBool("IsGrabbing", false);
        }

        Debug.Log("Subiu a escalada!");
        playerMovement._velocity.y = 0f;

        Runner.StartCoroutine(GrabCooldownRoutine()); // delay
    }

    private System.Collections.IEnumerator GrabCooldownRoutine()
    {
        grabBlocked = true;
        yield return new WaitForSeconds(grabCooldown);
        grabBlocked = false;
        Debug.Log("Pode agarrar novamente!");
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = debugCanGrab ? Color.green : Color.red;
            Gizmos.DrawLine(debugRayOrigin, debugRayEnd);

            if (debugCanGrab)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(debugRayEnd, 0.05f);
            }
        }
        else
        {
            for (int i = 0; i < rayAmount; i++)
            {
                Vector3 rayOrigin = transform.position + Vector3.up * rayHeight + Vector3.up * rayOffset * i;
                Vector3 rayEnd = rayOrigin + transform.forward * rayLength;
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(rayOrigin, rayEnd);
            }
        }
    }
}
