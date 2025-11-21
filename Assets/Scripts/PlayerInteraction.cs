using Fusion;
using UnityEngine;

public class PlayerInteraction : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float interactDistance = 2f;
    [SerializeField] private LayerMask interactLayer;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    // Armazena se o jogador apertou R no Update
    private bool pressedR;

    void Update()
    {
        // Só captura input do jogador local
        if (Object.HasInputAuthority)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                pressedR = true; // salva o estado

                if (debugMode)
                    Debug.Log("[PlayerInteraction] R apertado no Update()");
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority)
            return;

        // Se o jogador apertou R no Update, execute a interação no tick de rede
        if (pressedR)
        {
            TryInteract();
            pressedR = false; // reseta o input
        }
    }

    private void TryInteract()
    {
        // Raycast agora sai do player
        Vector3 origin = transform.position + Vector3.up * 1f;
        Vector3 direction = transform.forward;

        if (debugMode)
            Debug.Log("[PlayerInteraction] Tentando interagir via FixedUpdateNetwork...");

        if (Physics.Raycast(origin, direction, out RaycastHit hit, interactDistance, interactLayer))
        {
            if (debugMode)
                Debug.Log($"[PlayerInteraction] Raycast acertou: {hit.collider.name}");

            var obj = hit.collider.GetComponent<ChangeTextureObject>();

            if (obj != null)
            {
                if (debugMode)
                    Debug.Log("[PlayerInteraction] Objeto interagível encontrado! Chamando RPC...");

                obj.RpcChangeTexture();
            }
            else
            {
                if (debugMode)
                    Debug.Log("[PlayerInteraction] O objeto acertado não contém ChangeTextureObject.");
            }
        }
        else
        {
            if (debugMode)
                Debug.Log("[PlayerInteraction] Raycast não acertou nada.");
        }
    }

    void OnDrawGizmos()
    {
        if (!debugMode) return;

        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position + Vector3.up * 1f;
        Gizmos.DrawLine(origin, origin + transform.forward * interactDistance);
    }
}
