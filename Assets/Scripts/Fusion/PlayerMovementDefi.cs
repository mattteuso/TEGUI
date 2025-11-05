using Fusion;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementDefi : NetworkBehaviour
{
    // =================================================================
    // REFERÊNCIAS
    // =================================================================
    private CharacterController _controller;
    private Animator _animator; // Referência ao Animator

    // Variável de velocidade vertical (compartilhada)
    [HideInInspector] public Vector3 _velocity;

    // =================================================================
    // CONFIGURAÇÕES
    // =================================================================

    [Header("Configurações Base")]
    public float PlayerSpeed = 5f;
    public float JumpForce = 10f; // Mantida, mas não será usada para a força
    public float GravityValue = -9.81f;

    [Header("Configurações de Interação/Rotação")]
    public float InteractSpeedMultiplier = 0.5f; // 50% da velocidade ao interagir

    [Header("Configurações de Pulo")]
    public float JumpAnimationTimeout = 2f; // Tempo máximo para a animação de pulo (fallback)

    // Estados de Controle
    [HideInInspector] public bool CanRotate = true;
    [HideInInspector] public bool IsInteracting = false;

    // Estados de Pulo (Animação)
    private bool _jumpPressed;
    private bool isJumpingAnimation; // Flag para controlar o estado da animação de pulo
    private float jumpStartTime; // Timer para fallback

    // Input cache
    private Vector3 _moveInput;

    // =================================================================
    // LIFECYCLE E INPUT
    // =================================================================

    public override void Spawned()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (!HasInputAuthority)
            return;

        _moveInput = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

        // Captura input de pulo: Se pressionou e não está em animação de pulo
        if (Input.GetButtonDown("Jump") && !isJumpingAnimation)
        {
            _jumpPressed = true;
        }

        // Verifica se a animação de jump terminou ou timeout
        if (isJumpingAnimation && _animator != null)
        {
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            // Verifica se está na animação "Jump" e terminou
            if (stateInfo.IsName("Jump") && stateInfo.normalizedTime >= 1.0f)
            {
                isJumpingAnimation = false;
                _animator.SetBool("isJumping", false);
                Debug.Log("[PlayerMovement] Animação de pulo terminou normalmente.");
            }
            // Fallback: Se passou o tempo limite, força o reset
            else if (Time.time - jumpStartTime > JumpAnimationTimeout)
            {
                isJumpingAnimation = false;
                _animator.SetBool("isJumping", false);
                Debug.LogWarning("[PlayerMovement] Animação de pulo resetada por timeout.");
            }
        }
    }

    // =================================================================
    // LÓGICA DE MOVIMENTO DE REDE
    // =================================================================

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority)
            return;

        // 1. Aplica e zera a velocidade de queda no chão
        if (_controller.isGrounded && _velocity.y < 0)
            _velocity.y = -1f;

        // 2. Processamento do Pulo e Gravidade

        // A. Se pulo foi solicitado E estiver no chão E não estiver em outra animação de pulo, INICIA A ANIMAÇÃO.
        if (_jumpPressed && _controller.isGrounded && !isJumpingAnimation)
        {
            isJumpingAnimation = true;
            jumpStartTime = Time.time; // Inicia o timer
            if (_animator != null)
            {
                _animator.SetBool("isJumping", true);
            }
            // Não aplica força vertical (Pulo Físico IGNORADO)
            _jumpPressed = false;
            Debug.Log("[PlayerMovement] Pulo iniciado.");
        }

        // B. Aplica a Gravidade
        // Isso é feito SEMPRE para que o player caia, mesmo durante a animação de pulo
        _velocity.y += GravityValue * Runner.DeltaTime;

        // C. Prepara Movimento Horizontal
        Vector3 finalMove = Vector3.zero;

        // 3. Trava o movimento horizontal se estiver na animação de pulo
        if (!isJumpingAnimation)
        {
            // --- Lógica de Movimento Horizontal (XZ) ---

            float speed = PlayerSpeed;
            finalMove = _moveInput;

            if (IsInteracting)
            {
                // Lógica de travamento de eixo para Interação
                if (Mathf.Abs(finalMove.x) > Mathf.Abs(finalMove.z))
                    finalMove = new Vector3(finalMove.x, 0, 0);
                else
                    finalMove = new Vector3(0, 0, finalMove.z);

                speed *= InteractSpeedMultiplier;
            }

            finalMove *= speed * Runner.DeltaTime; // Aplica velocidade e DeltaTime
        }
        else
        {
            // Se estiver pulando, o movimento horizontal é ZERO.
            // A animação (se for Root Motion) ou um evento no Animator que deve mover o player.
        }

        // 4. Aplica Movimento Final (Horizontal + Vertical)
        _controller.Move(finalMove + _velocity * Runner.DeltaTime);

        // 5. Rotação: Só se houver movimento horizontal (não durante pulo)
        if (finalMove.sqrMagnitude > 0.001f && CanRotate)
            transform.forward = finalMove.normalized;

        // 6. Integração com Animator (Idle/Walk) - Somente se não estiver pulando
        if (!isJumpingAnimation)
        {
            HandleAnimator(finalMove);
        }

        // 7. Reseta Inputs (o _jumpPressed já foi resetado na seção 2.A)
    }

    // =================================================================
    // MÉTODOS AUXILIARES
    // =================================================================

    private void HandleAnimator(Vector3 currentMove)
    {
        if (_animator != null)
        {
            float horizontalSpeed = currentMove.magnitude / (PlayerSpeed * Runner.DeltaTime);

            if (horizontalSpeed > 0.1f)
            {
                _animator.SetBool("isWalking", true);
            }
            else
            {
                _animator.SetBool("isWalking", false);
            }
            _animator.SetFloat("Speed", horizontalSpeed);
        }
    }
}
