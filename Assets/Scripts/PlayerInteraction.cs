using Fusion;
using UnityEngine;

public class PlayerInteraction : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float interactDistance = 2f;
    [SerializeField] private LayerMask interactLayer;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private bool pressedR;

    void Update()
    {
        if (Object.HasInputAuthority)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                pressedR = true;

                if (debugMode)
                    Debug.Log("[PlayerInteraction] R apertado");
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority)
            return;

        if (pressedR)
        {
            TryInteract();
            pressedR = false;
        }
    }

    private void TryInteract()
    {
        Vector3 origin = transform.position + Vector3.up;
        Vector3 direction = transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, interactDistance, interactLayer))
        {
            if (debugMode)
                Debug.Log($"[PlayerInteraction] Raycast acertou: {hit.collider.name}");

            var obj = hit.collider.GetComponent<ChangeTextureObject>();

            if (obj != null)
            {
                if (debugMode)
                    Debug.Log("[PlayerInteraction] Enviando pedido ao Host para trocar textura...");

                // Agora funciona em Shared Mode
                obj.RpcRequestChangeTexture();
            }
        }
        else
        {
            if (debugMode)
                Debug.Log("[PlayerInteraction] Nada acertado.");
        }
    }
}
