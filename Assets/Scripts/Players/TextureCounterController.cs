using UnityEngine;

public class TextureCounterController : MonoBehaviour
{
    // O contador agora é uma propriedade simples
    public int TextureChangeCount { get; private set; }

    private static TextureCounterController instance;
    public static TextureCounterController Instance => instance;

    private void Awake()
    {
        // Singleton simples para Single Player
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        TextureChangeCount = 0;
    }

    // Wrapper estático igual ao anterior para não quebrar seus outros scripts
    public static void Incrementar()
    {
        if (instance == null)
        {
            Debug.LogWarning("[TextureCounterController] Nenhuma instância encontrada na cena.");
            return;
        }

        instance.AddCount();
    }

    // Método que substitui o antigo RPC
    public void AddCount()
    {
        TextureChangeCount++;
        Debug.Log($"[TextureCounterController] Contador atualizado -> {TextureChangeCount}");
    }
}