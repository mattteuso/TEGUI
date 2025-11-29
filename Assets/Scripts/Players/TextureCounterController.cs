using Fusion;
using UnityEngine;

public class TextureCounterController : NetworkBehaviour
{
    [Networked]
    public int TextureChangeCount { get; set; }

    private static TextureCounterController instance;
    public static TextureCounterController Instance => instance;

    public override void Spawned()
    {
        instance = this;

        if (Object.HasStateAuthority)
        {
            TextureChangeCount = 0;
        }
    }

    // Wrapper estático para facilitar chamadas externas.
    // Chamem TextureCounterController.Incrementar() de qualquer lugar.
    public static void Incrementar()
    {
        if (instance == null)
        {
            Debug.LogWarning("[TextureCounterController] Nenhuma instância encontrada na cena.");
            return;
        }

        // Chamamos o RPC na instância — o RPC será executado apenas no StateAuthority.
        instance.RpcIncrementar();
    }

    // RPC executado no StateAuthority (host). Marca conforme o padrão do Fusion.
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcIncrementar()
    {
        // Segurança extra: só conta se estivermos no StateAuthority.
        if (!Object.HasStateAuthority)
            return;

        TextureChangeCount++;
        Debug.Log($"[TextureCounterController] Contador atualizado -> {TextureChangeCount}");
    }
}
