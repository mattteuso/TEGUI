using Fusion;
using UnityEngine;

public class ChangeTextureObject : NetworkBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    [Header("References")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Texture textureA;
    [SerializeField] private Texture textureB;
    [SerializeField] private Texture textureC;

    [Networked] public bool AlreadyUsed { get; set; }

    public override void Spawned()
    {
        if (targetRenderer != null)
            targetRenderer.material.mainTexture = textureA;
    }

    // CLIENT/ HOST → HOST (sempre)
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcRequestTextureChange(bool isHost)
    {
        if (AlreadyUsed)
            return;

        AlreadyUsed = true;

        int textureIndex = isHost ? 1 : 2; // 1=B, 2=C

        RpcApplyTexture(textureIndex);
    }

    // HOST → TODOS
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcApplyTexture(int index)
    {
        if (!targetRenderer) return;

        switch (index)
        {
            case 1:
                targetRenderer.material.mainTexture = textureB;
                break;
            case 2:
                targetRenderer.material.mainTexture = textureC;
                break;
        }

        if (debugMode)
            Debug.Log("[ChangeTextureObject] Aplicada textura " + index);
    }
}
