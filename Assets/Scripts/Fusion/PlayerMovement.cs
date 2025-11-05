using Fusion;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    private CharacterController _controller;
    private Animator _animator;

    public Vector3 _velocity;
    private bool _jumpPressed;
    private bool wasGroundedLastFrame = false;
    private bool isJumping;
    private bool isFalling;
    private bool isLanding;
    private float landTimer; // Timer para controlar o bloqueio do land

    private Vector3 _moveInput;

    [Header("Configurações")]
    public float PlayerSpeed = 5f;
    public float JumpForce = 10f;
    public float GravityValue = -9.81f;
    public float landLockTime = 0.4f; // Tempo mínimo que o land bloqueia movimento

    public override void Spawned()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (!HasInputAuthority)
            return;

        // Captura input
        _moveInput = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        if (Input.GetButtonDown("Jump"))
            _jumpPressed = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority)
            return;

        bool isGrounded = _controller.isGrounded;

        // Detecta início da queda (apenas se não estava no chão e velocidade Y negativa)
        if (!isGrounded && !isFalling && _velocity.y < -1f)
        {
            isFalling = true;
            if (_animator) _animator.SetBool("isFalling", true);
        }

        // Detecta aterrissagem (apenas se estava caindo e acabou de tocar o chão)
        if (isGrounded && !wasGroundedLastFrame && isFalling)
        {
            if (_animator)
            {
                _animator.SetTrigger("Land");
                _animator.SetBool("isFalling", false);
                _animator.SetBool("isJumping", false);
            }

            isLanding = true;  // Ativa o lock
            isFalling = false;
            isJumping = false;
            landTimer = 0f;  // Reinicia o timer
            Debug.Log("Land triggered - Movement locked");  // Log para debug
        }

        // Gerencia o fim do land (timer baseado em tempo de rede)
        if (isLanding)
        {
            landTimer += Runner.DeltaTime;
            if (landTimer >= landLockTime)
            {
                isLanding = false;  // Desativa o lock
                if (_animator)
                {
                    _animator.ResetTrigger("Land");
                    _animator.SetBool("isIdle", true);
                }
                Debug.Log("Land ended - Movement unlocked");  // Log para debug
            }
        }

        // Pulo (bloqueado durante land)
        if (_jumpPressed && isGrounded && !isLanding)
        {
            _velocity.y = JumpForce;
            if (_animator)
            {
                _animator.SetBool("isJumping", true);
                _animator.SetBool("isIdle", false);
                _animator.SetBool("isWalking", false);
            }
            isJumping = true;
        }

        // Gravidade
        if (isGrounded && _velocity.y < 0)
            _velocity.y = -1f;
        else
            _velocity.y += GravityValue * Runner.DeltaTime;

        // Movimento (bloqueado apenas durante land)
        Vector3 move = Vector3.zero;
        if (!isLanding)  // Ajuste aqui: Certifique-se de que o lock só é aplicado quando isLanding é true
        {
            Vector3 normalizedInput = _moveInput.normalized;
            move = normalizedInput * PlayerSpeed * Runner.DeltaTime;
        }

        _controller.Move(move + _velocity * Runner.DeltaTime);

        // Atualiza rotação e animações (bloqueado durante land)
        if (move.sqrMagnitude > 0.001f && !isLanding)
        {
            transform.forward = move.normalized;
            if (_animator)
            {
                _animator.SetBool("isWalking", true);
                _animator.SetBool("isIdle", false);
            }
        }
        else if (!isLanding && !isJumping && isGrounded)
        {
            if (_animator)
            {
                _animator.SetBool("isWalking", false);
                _animator.SetBool("isIdle", true);
            }
        }

        _jumpPressed = false;
        wasGroundedLastFrame = isGrounded;
    }
}
