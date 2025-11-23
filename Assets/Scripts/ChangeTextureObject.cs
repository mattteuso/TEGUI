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

    [Networked] public bool AlreadyUsed { get; set; }

    public override void Spawned()
    {
        if (targetRenderer != null)
        {
            targetRenderer.material.mainTexture = textureA;

            if (debugMode)
                Debug.Log("[ChangeTextureObject] Começando com textura A.");
        }
    }

    // Qualquer peer pode enviar o RPC, mas apenas o Host executa
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcRequestChangeTexture()
    {
        ApplyTextureChange(); // executa apenas no Host (StateAuthority)
    }

    // Host replica para todos a troca visual
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcApplyTexture()
    {
        if (targetRenderer != null)
            targetRenderer.material.mainTexture = textureB;

        if (debugMode)
            Debug.Log("[ChangeTextureObject] Textura trocada para B!");
    }

    private void ApplyTextureChange()
    {
        if (AlreadyUsed)
        {
            if (debugMode)
                Debug.Log("[ChangeTextureObject] Já foi usado, ignorando.");
            return;
        }

        AlreadyUsed = true;

        // Envia para TODOS aplicarem a textura
        RpcApplyTexture();
    }
}
