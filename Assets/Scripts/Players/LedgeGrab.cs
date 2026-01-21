using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class LedgeGrab : MonoBehaviour
{
    #region === REFERENCES ===
    private CharacterController controller;
    private PlayerMovement playerMovement;
    private Animator animator;
    #endregion

    #region === SETTINGS ===
    [Header("Ledge Config")]
    public LayerMask ledgeLayer;
    public float rayLength = 0.8f;
    public float rayHeight = 1.5f;
    public float grabHeightOffset = 0.1f;
    public float grabForwardOffset = 0.25f;
    public float moveSpeed = 2f;
    public int rayAmount = 5;
    public float rayOffset = 0.15f;

    [Header("Lateral Movement")]
    public float lateralRayLength = 0.5f;
    [Tooltip("Distância lateral para checar se a borda continua")]
    public float ledgeContinuityLateralOffset = 0.3f;

    [Header("Climb")]
    public float climbUpHeight = 1.5f;
    public float climbUpSpeed = 3f;

    [Header("Input & Behavior")]
    public bool autoGrab = true;
    public float jumpDelay = 0.25f;
    #endregion

    #region === STATE ===
    public bool isGrabbing;
    public bool isClimbing;
    private bool grabBlocked;
    private bool jumpBlockedAfterGrab;
    private bool jumpOnCooldown;
    private bool isGrabJumping;

    private RaycastHit lastLedgeHit;
    private Quaternion grabRotation;

    // Inputs
    private float horizontalInput;
    private bool jumpPressed;
    private bool releasePressed;
    #endregion

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        animator = GetComponentInChildren<Animator>();
    }

    #region === INPUT SYSTEM CALLBACKS ===
    public void OnMove(InputValue value)
    {
        Vector2 input = value.Get<Vector2>();
        horizontalInput = input.x;
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed && isGrabbing && !jumpOnCooldown && !jumpBlockedAfterGrab && !isGrabJumping)
        {
            jumpPressed = true;
        }
    }

    public void OnRelease(InputValue value)
    {
        if (value.isPressed && isGrabbing)
            releasePressed = true;
    }
    #endregion

    private void Update()
    {
        if (isClimbing) return;

        TryDetectLedge();

        if (isGrabbing)
        {
            HandleHorizontalMovement();

            if (jumpPressed)
            {
                jumpPressed = false;
                StartCoroutine(JumpDelayRoutine());
            }

            if (releasePressed)
            {
                releasePressed = false;
                ReleaseLedge();
            }
        }

        DrawGrabDebugRays();
    }

    private void TryDetectLedge()
    {
        if (isGrabbing || controller.isGrounded || grabBlocked) return;

        for (int i = 0; i < rayAmount; i++)
        {
            Vector3 origin = transform.position + Vector3.up * (rayHeight + rayOffset * i);

            if (Physics.Raycast(origin, transform.forward, out RaycastHit hit, rayLength, ledgeLayer))
            {
                if (hit.collider.CompareTag("Ledge"))
                {
                    lastLedgeHit = hit;
                    StartGrab();
                    return;
                }
            }
        }
    }

    private void StartGrab()
    {
        isGrabbing = true;
        controller.enabled = false;
        playerMovement.enabled = false;

        grabRotation = Quaternion.LookRotation(-lastLedgeHit.normal, Vector3.up);
        transform.rotation = grabRotation;

        Vector3 grabPos = lastLedgeHit.point +
                         Vector3.up * grabHeightOffset -
                         grabRotation * Vector3.forward * grabForwardOffset;

        transform.position = grabPos;

        animator?.SetTrigger("Grab");
        animator?.SetBool("IsGrabbing", true);
        animator?.SetBool("IsGrabIdle", true);

        StartCoroutine(GrabInputDelay());
    }

    private void HandleHorizontalMovement()
    {
        transform.rotation = grabRotation;

        if (Mathf.Abs(horizontalInput) < 0.1f)
        {
            SetGrabAnimations(true, false, false);
            return;
        }

        Vector3 moveDir = horizontalInput > 0 ? transform.right : -transform.right;

        // CORREÇÃO
        bool canMove = !BlockedByWall(moveDir) && HasLedgeAhead(moveDir);

        if (canMove)
        {
            transform.position += moveDir * moveSpeed * Time.deltaTime;
            SetGrabAnimations(false, horizontalInput < 0, horizontalInput > 0);
        }
        else
        {
            SetGrabAnimations(true, false, false);
        }
    }

    private void SetGrabAnimations(bool idle, bool left, bool right)
    {
        animator?.SetBool("IsGrabIdle", idle);
        animator?.SetBool("IsGrabWalkingLeft", left);
        animator?.SetBool("IsGrabWalkingRight", right);
    }

    private bool BlockedByWall(Vector3 dir)
    {
        // Usa rayHeight para garantir que o raio saia da altura das mãos
        Vector3 origin = transform.position + Vector3.up * rayHeight;
        Debug.DrawRay(origin, dir * lateralRayLength, Color.blue);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, lateralRayLength))
        {
            // Bloqueia se atingir algo que NÃO seja a borda
            return !hit.collider.CompareTag("Ledge");
        }
        return false;
    }

    private bool HasLedgeAhead(Vector3 dir)
    {
        // Projeta o raio para frente, mas deslocado para o lado do movimento
        Vector3 lateralOrigin = transform.position + (dir * ledgeContinuityLateralOffset) + (Vector3.up * rayHeight);
        Debug.DrawRay(lateralOrigin, transform.forward * rayLength, Color.yellow);

        return Physics.Raycast(lateralOrigin, transform.forward, out RaycastHit hit, rayLength, ledgeLayer);
    }

    private void ReleaseLedge()
    {
        isGrabbing = false;
        controller.enabled = true;
        playerMovement.enabled = true;

        animator?.SetBool("IsGrabbing", false);
        SetGrabAnimations(false, false, false);

        transform.position -= transform.forward * 0.2f;
        StartCoroutine(GrabCooldownRoutine());
    }

    #region === COROUTINES ===
    private IEnumerator JumpDelayRoutine()
    {
        isGrabJumping = true;
        animator?.SetTrigger("GrabJump");
        animator?.SetBool("IsGrabIdle", false);

        yield return new WaitForSeconds(jumpDelay);

        if (isGrabbing)
        {
            jumpOnCooldown = true;
            StartCoroutine(JumpCooldownRoutine());
            StartCoroutine(ClimbUpRoutine());
        }
        isGrabJumping = false;
    }

    private IEnumerator ClimbUpRoutine()
    {
        isClimbing = true;
        // o playerMovement desativado para não interferir na gravidade/input
        controller.enabled = true;

        float raisedHeight = 0;
        while (raisedHeight < climbUpHeight)
        {
            float step = climbUpSpeed * Time.deltaTime;
            // Usamos Vector3.up * step para subir
            controller.Move(Vector3.up * step);
            raisedHeight += step;
            yield return null;
        }

        float movedForward = 0;
        float forwardDistance = 0.5f; // Distância segura para entrar na plataforma
        while (movedForward < forwardDistance)
        {
            float step = climbUpSpeed * Time.deltaTime;
            // Move na direção que o player está olhando (para a plataforma)
            //respeitar paredes B)
            controller.Move(transform.forward * step);
            movedForward += step;
            yield return null;
        }

        // Finalização
        isClimbing = false;
        isGrabbing = false;
        isGrabJumping = false;

        playerMovement.enabled = true;
        playerMovement._velocity.y = 0;

        animator?.SetBool("IsGrabbing", false);
        animator?.SetBool("IsGrabIdle", false);
    }

    private IEnumerator GrabCooldownRoutine()
    {
        grabBlocked = true;
        yield return new WaitForSeconds(0.5f);
        grabBlocked = false;
    }

    private IEnumerator GrabInputDelay()
    {
        jumpBlockedAfterGrab = true;
        yield return new WaitForSeconds(0.3f);
        jumpBlockedAfterGrab = false;
    }

    private IEnumerator JumpCooldownRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        jumpOnCooldown = false;
    }
    #endregion

    private void DrawGrabDebugRays()
    {
        if (controller.isGrounded) return;
        for (int i = 0; i < rayAmount; i++)
        {
            Vector3 origin = transform.position + Vector3.up * (rayHeight + rayOffset * i);
            Debug.DrawRay(origin, transform.forward * rayLength, Color.red);
        }
    }
}