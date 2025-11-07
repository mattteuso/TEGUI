using Fusion;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PushableNetworkController : NetworkBehaviour
{
    [Header("Ajustes")]
    public float followSpeed = 10f;        // quão rápido segue o holdPoint
    public float positionLerp = 12f;
    public float rotationLerp = 12f;

    // Network state (não precisa ser replicado manualmente se só o StateAuthority manipula)
    private NetworkId carrierPlayerId = NetworkId.None;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Chamado pelo servidor (StateAuthority) via RPC no ObjectInteraction
    public void StartCarrying(NetworkId playerId)
    {
        // Só executamos isso na máquina que tem autoridade de estado sobre o objeto
        if (!Object.HasStateAuthority)
            return;

        carrierPlayerId = playerId;
        rb.isKinematic = false; // mantemos física, mas vamos ajustar posição manualmente
    }

    public void StopCarrying()
    {
        if (!Object.HasStateAuthority)
            return;

        carrierPlayerId = NetworkId.None;
        // volta a ser afetado pela física normalmente
        rb.isKinematic = false;
    }

    // FixedUpdateNetwork roda no contexto de rede (NetworkBehaviour)
    public override void FixedUpdateNetwork()
    {
        // Apenas a máquina com StateAuthority deve mover o objeto
        if (!Object.HasStateAuthority)
            return;

        if (carrierPlayerId == NetworkId.None)
            return;

        var playerObj = Runner.FindObject(carrierPlayerId);
        if (playerObj == null) return;

        var objInteraction = playerObj.GetComponent<ObjectInteraction>();
        if (objInteraction == null) return;

        Transform holdPoint = objInteraction.GetHoldPoint();
        if (holdPoint == null) return;

        // posição alvo
        Vector3 targetPos = holdPoint.position;
        Quaternion targetRot = holdPoint.rotation;

        // mover o rigidbody de forma suave
        Vector3 newPos = Vector3.Lerp(rb.position, targetPos, positionLerp * Runner.DeltaTime);
        rb.MovePosition(newPos);

        Quaternion newRot = Quaternion.Slerp(rb.rotation, targetRot, rotationLerp * Runner.DeltaTime);
        rb.MoveRotation(newRot);
    }
}
