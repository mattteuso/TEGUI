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
            if (Input.GetKeyDown(KeyCode.F))
                pressedR = true;
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
        Vector3 dir = transform.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, interactDistance, interactLayer))
        {
            var obj = hit.collider.GetComponent<ChangeTextureObject>();
            if (!obj) return;

            // A forma 100% confiável de identificar o HOST
            bool isHost = obj.Object.HasStateAuthority;

            if (debugMode)
                Debug.Log($"[PlayerInteraction] Enviando pedido, Host = {isHost}");

            obj.RpcRequestTextureChange(isHost);
        }
        else
        {
            if (debugMode)
                Debug.Log("[PlayerInteraction] Raycast não acertou nada.");
        }
    }
}
