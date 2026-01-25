using UnityEngine;
using UnityEngine.InputSystem; // Adicionado para suportar CallbackContext
using System.Collections;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float interactDistance = 2f;
    [SerializeField] private LayerMask interactLayer;

    [Header("Animation")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private string paintTrigger = "Pixo";
    [SerializeField] private float paintDelay = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private IPlayerMovement movement;

    private void Awake()
    {
        movement = GetComponent<IPlayerMovement>();
        if (movement == null)
        {
            Debug.LogWarning("[PlayerInteraction] Nenhum script de movimento compatível encontrado.");
        }
    }

    // =================================================================
    // INPUT SYSTEM CALLBACK (Unity Events)
    // =================================================================
    public void OnInteract(InputAction.CallbackContext context)
    {
        // 'started' garante que a interação só rode UMA vez por clique
        if (context.started)
        {
            if (debugMode) Debug.Log("[PlayerInteraction] Botão de Interação pressionado");
            TryInteract();
        }
    }

    private void TryInteract()
    {
        // Verifica se o player está no chão via interface
        if (movement == null || !movement.IsGrounded)
        {
            if (debugMode) Debug.Log("[PlayerInteraction] Cancelado: Player no ar.");
            return;
        }

        Vector3 origin = transform.position + Vector3.up;
        Vector3 dir = transform.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, interactDistance, interactLayer))
        {
            var obj = hit.collider.GetComponent<ChangeTextureObject>();
            if (obj == null) return;

            // Executa a animação
            if (playerAnimator != null)
            {
                playerAnimator.SetTrigger(paintTrigger);
            }

            // Inicia a lógica com delay
            StartCoroutine(ApplyPaintDelayed(obj));
        }
    }

    private IEnumerator ApplyPaintDelayed(ChangeTextureObject obj)
    {
        // Bloqueia movimento
        movement?.SetMovementBlocked(true);

        yield return new WaitForSeconds(paintDelay);

        // Lógica de seleção por Tag (Player 1 ou Player 2)
        string playerTag = gameObject.tag;
        int textureIndex = 0;

        if (playerTag == "Player") textureIndex = Random.Range(1, 3);
        else if (playerTag == "Player2") textureIndex = Random.Range(3, 5);
        else
        {
            if (debugMode) Debug.LogWarning("[PlayerInteraction] Tag inválida: " + playerTag);
            movement?.SetMovementBlocked(false);
            yield break;
        }

        obj.ApplyTextureIndex(textureIndex);

        // Libera movimento
        movement?.SetMovementBlocked(false);
    }
}