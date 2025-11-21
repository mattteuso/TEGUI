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
        // Começa na textura A
        if (targetRenderer != null)
        {
            targetRenderer.material.mainTexture = textureA;

            if (debugMode)
                Debug.Log("[ChangeTextureObject] Começando com textura A.");
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcChangeTexture()
    {
        if (AlreadyUsed)
        {
            if (debugMode)
                Debug.Log("[ChangeTextureObject] Já foi usado! Ignorando...");
            return;
        }

        // Marca como usado e troca para textura B
        AlreadyUsed = true;
        targetRenderer.material.mainTexture = textureB;

        if (debugMode)
            Debug.Log("[ChangeTextureObject] Textura trocada para B!");
    }
}

