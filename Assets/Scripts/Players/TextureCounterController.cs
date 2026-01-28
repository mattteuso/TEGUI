using UnityEngine;

public class TextureCounterController : MonoBehaviour
{
    [Header("Configurações de Vitória")]
    [SerializeField] private int targetTextureCount = 10; // Quantidade necessária para ganhar

    public int TextureChangeCount { get; private set; }

    private static TextureCounterController instance;
    public static TextureCounterController Instance => instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        TextureChangeCount = 0;
    }

    public static void Incrementar()
    {
        if (instance == null)
        {
            Debug.LogWarning("[TextureCounterController] Nenhuma instância encontrada.");
            return;
        }

        instance.AddCount();
    }

    public void AddCount()
    {
        // Se o jogo já acabou, não conta mais
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive) return;

        TextureChangeCount++;
        Debug.Log($"[TextureCounterController] Contador: {TextureChangeCount} / {targetTextureCount}");

        // VERIFICAÇÃO DA CONDIÇÃO DE VITÓRIA
        if (TextureChangeCount >= targetTextureCount)
        {
            WinGame();
        }
    }

    private void WinGame()
    {
        Debug.Log("Meta atingida! Enviando vitória para o GameManager.");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.WinGameLocal();
        }
        else
        {
            Debug.LogError("GameManager não encontrado para processar a vitória!");
        }
    }
}