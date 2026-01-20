using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementDefi : MonoBehaviour, IPlayerMovement
{
    // =================================================================
    // REFERÊNCIAS
    // =================================================================
    private CharacterController _controller;
    private Animator _animator;

    public bool IsGrounded => _controller.isGrounded;

    [HideInInspector] public Vector3 _velocity;
    [HideInInspector] public bool IsMovementBlocked = false;

    // =================================================================
    // CONFIGURAÇÕES
    // =================================================================

    [Header("Movimento")]
    public float PlayerSpeed = 5f;
    public float GravityValue = -9.81f;

    [Header("Rotação")]
    public float RotationSpeed = 10f;

    [Header("Interação")]
    public float InteractSpeedMultiplier = 0.5f;

    [Header("Pulo")]
    public float JumpAnimationTimeout = 2f;

    public bool CanRotate = true;
    public bool IsInteracting = false;

    // =================================================================
    // ESTADOS
    // =================================================================

    private bool _jumpPressed;
    private bool isJumpingAnimation;
    private float jumpStartTime;

    private Vector2 _rawInput;
    private Vector3 _moveInput;
    private Vector3 _desiredDirection;

    // =================================================================
    // LIFECYCLE
    // =================================================================

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        UpdateJumpAnimationState();
        HandleMovement();
    }

    // =================================================================
    // INPUT SYSTEM
    // =================================================================

    public void OnMove(InputValue value)
    {
        _rawInput = value.Get<Vector2>();
        _moveInput = new Vector3(_rawInput.x, 0, _rawInput.y);
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed && IsGrounded && !isJumpingAnimation)
        {
            _jumpPressed = true;
        }
    }

    // =================================================================
    // MOVIMENTO
    // =================================================================

    private void HandleMovement()
    {
        if (IsMovementBlocked)
        {
            _velocity = Vector3.zero;
            return;
        }

        if (IsGrounded && _velocity.y < 0)
            _velocity.y = -2f;

        HandleJump();
        ApplyGravity();

        Vector3 horizontalMove = HandleHorizontalMovement();

        _controller.Move(
            (horizontalMove + _velocity) * Time.deltaTime
        );

        HandleRotation();
        HandleAnimator(horizontalMove);
    }

    private void HandleJump()
    {
        if (!_jumpPressed) return;

        isJumpingAnimation = true;
        jumpStartTime = Time.time;

        if (_animator != null)
            _animator.SetBool("isJumping", true);

        _jumpPressed = false;
    }

    private void ApplyGravity()
    {
        _velocity.y += GravityValue * Time.deltaTime;
    }

    private Vector3 HandleHorizontalMovement()
    {
        if (isJumpingAnimation) return Vector3.zero;

        Vector3 move = _moveInput;
        float speed = PlayerSpeed;

        if (IsInteracting)
        {
            if (Mathf.Abs(move.x) > Mathf.Abs(move.z))
                move = new Vector3(move.x, 0, 0);
            else
                move = new Vector3(0, 0, move.z);

            speed *= InteractSpeedMultiplier;
        }

        _desiredDirection = move.normalized;
        return move * speed;
    }

    // =================================================================
    // ROTAÇÃO SUAVE
    // =================================================================

    private void HandleRotation()
    {
        if (!CanRotate) return;
        if (_desiredDirection.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(_desiredDirection);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            RotationSpeed * Time.deltaTime
        );
    }

    // =================================================================
    // ANIMAÇÃO
    // =================================================================

    private void UpdateJumpAnimationState()
    {
        if (!isJumpingAnimation || _animator == null) return;

        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);

        if (state.IsName("Jump") && state.normalizedTime >= 1f)
            ResetJumpAnimation();
        else if (Time.time - jumpStartTime > JumpAnimationTimeout)
            ResetJumpAnimation();
    }

    private void ResetJumpAnimation()
    {
        isJumpingAnimation = false;
        _animator.SetBool("isJumping", false);
    }

    private void HandleAnimator(Vector3 move)
    {
        if (_animator == null || isJumpingAnimation) return;

        float speedPercent = move.magnitude / PlayerSpeed;
        _animator.SetBool("isWalking", speedPercent > 0.05f);
        _animator.SetFloat("Speed", speedPercent);
    }

    // =================================================================
    // INTERFACE
    // =================================================================

    public void SetMovementBlocked(bool isBlocked)
    {
        IsMovementBlocked = isBlocked;

        if (_animator != null)
        {
            _animator.SetBool("isWalking", false);
            _animator.SetBool("isJumping", false);
        }
    }
}
