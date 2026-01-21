using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour, IPlayerMovement
{
    private CharacterController _controller;
    private Animator _animator;
    private LedgeGrab ledgeGrab;

    [Header("Configurações de Movimento")]
    public float PlayerSpeed = 5f;
    public float JumpForce = 10f;
    public float GravityValue = -9.81f;
    public float RotationSpeed = 10f;
    [Tooltip("Sensibilidade para ativar animação de andar")]
    public float animationThreshold = 0.1f;

    [Header("Lata de Tinta")]
    public GameObject paintCan;
    public float paintCanDuration = 2f;
    private bool PaintCanActive { get; set; }

    // Propriedades da Interface/Estado
    public bool IsGrounded => _controller != null && _controller.isGrounded;
    public bool IsMovementBlocked = false;

    // Variáveis de Controle Interno
    public Vector3 _velocity;
    private Vector2 _inputVector;
    private bool _jumpPressed;
    private bool wasGroundedLastFrame = false;
    private bool isJumping;
    private bool isFalling;
    public float HorizontalInput => _inputVector.x;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();
        ledgeGrab = GetComponent<LedgeGrab>();
    }

    private void Start()
    {
        if (paintCan != null)
            paintCan.SetActive(false);

        // Busca o HUD na cena
        var hudObject = GameObject.Find("PlayerHUD");
        if (hudObject != null)
        {
            var hudScript = hudObject.GetComponent<PlayerHUD>();
            if (hudScript != null)
                hudScript.SetPlayer(this);
        }
    }

    #region Input System Callbacks
    // send messages

    public void OnMove(InputValue value)
    {
        _inputVector = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed)
            _jumpPressed = true;
    }

    public void OnPaint(InputValue value)
    {
        if (value.isPressed)
            TryUsePaintCan();
    }
    #endregion

    void Update()
    {
        ApplyMovement();
    }

    private void ApplyMovement()
    {
        if (ledgeGrab != null && (ledgeGrab.isGrabbing || ledgeGrab.isClimbing))
        {
            _velocity.y = -1f;
            _jumpPressed = false;
            return;
        }

        //Bloqueio de Movimento
        if (IsMovementBlocked)
        {
            _velocity = Vector3.zero;
            UpdateAnimations(Vector3.zero, _controller.isGrounded);
            return;
        }

        bool isCurrentlyGrounded = _controller.isGrounded;

        //(Falling)
        if (!isCurrentlyGrounded && !isFalling && _velocity.y < -1f)
        {
            isFalling = true;
            _animator.SetBool("isFalling", true);
        }

        //(Landing)
        if (isCurrentlyGrounded && !wasGroundedLastFrame && isFalling)
        {
            _animator.SetTrigger("Land");
            _animator.SetBool("isFalling", false);
            _animator.SetBool("isJumping", false);
            isFalling = false;
            isJumping = false;
        }

        //Lógica de Pulo
        if (_jumpPressed && isCurrentlyGrounded)
        {
            _velocity.y = JumpForce;
            _animator.SetBool("isJumping", true);
            isJumping = true;
        }
        _jumpPressed = false; // Consome o input

        //Gravidade
        if (isCurrentlyGrounded && _velocity.y < 0)
        {
            _velocity.y = -1f;
        }
        else
        {
            _velocity.y += GravityValue * Time.deltaTime;
        }

        //Cálculo de Direção (Horizontal)
        Vector3 moveDirection = new Vector3(_inputVector.x, 0, _inputVector.y);

        // Movimento Horizontal sem o DeltaTime para cálculo de animação estável
        Vector3 horizontalMove = moveDirection.normalized * PlayerSpeed;

        //Aplicação Final no CharacterController
        // (Movimento Horizontal + Velocidade Vertical/Gravidade) * Time.deltaTime
        _controller.Move((horizontalMove + _velocity) * Time.deltaTime);

        //Atualização de Animações e Rotação
        UpdateAnimations(moveDirection, isCurrentlyGrounded);

        wasGroundedLastFrame = isCurrentlyGrounded;
    }

    private void UpdateAnimations(Vector3 moveInput, bool isGrounded)
    {
        if (moveInput.sqrMagnitude > animationThreshold)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveInput.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * RotationSpeed);

            _animator.SetBool("isWalking", true);
            _animator.SetBool("isIdle", false);
        }
        else if (isGrounded)
        {
            _animator.SetBool("isWalking", false);
            _animator.SetBool("isIdle", true);
        }
    }

    // ===========================
    //         PAINT CAN
    // ===========================

    private void TryUsePaintCan()
    {
        if (!_controller.isGrounded) return;
        if (PaintCanActive) return;

        StartCoroutine(PaintCanRoutine());
    }

    private IEnumerator PaintCanRoutine()
    {
        PaintCanActive = true;
        if (paintCan != null)
            paintCan.SetActive(true);

        yield return new WaitForSeconds(paintCanDuration);

        if (paintCan != null)
            paintCan.SetActive(false);

        PaintCanActive = false;
    }

    public void SetMovementBlocked(bool isBlocked)
    {
        IsMovementBlocked = isBlocked;

        if (isBlocked && _animator != null)
        {
            _animator.SetBool("isWalking", false);
            _animator.SetBool("isIdle", true);
            _animator.SetBool("isJumping", false);
            _animator.SetBool("isFalling", false);
        }
    }
}