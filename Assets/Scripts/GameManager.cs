using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Fusion;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Menu Scene Name")]
    public string menuSceneName = "MainMenu";

    [Header("Music")]
    public AudioSource musicSource;
    [Header("Music Fade Out")]
    public float fadeOutDuration = 1.5f;    // tempo do fade


    [Header("Victory")]
    public int requiredTextureCount = 4;     // Número necessário para vencer
    public GameObject victoryScreen;          // Tela de vitória
    public bool isVictoryTriggered = false;

    private bool isBusy = false;
    [HideInInspector] public bool IsGameActive = true;
    public GameObject defeatScreen;
    public bool isGameOverTriggered = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Garante que existe um AudioSource para a música
        if (musicSource == null)
        {
            // Tenta encontrar um AudioSource na cena se a referência estiver vazia.
            musicSource = FindObjectOfType<AudioSource>();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnToMenuESC();
        }
    }

    // [Restante dos métodos de RestartLevel, RPC_InitiateRestart, ShutdownAndRestartRoutine, ReturnToMenuButton, ReturnToMenuRoutine - Sem alterações]

    public void RestartLevel()
    {
        if (isBusy) return;
        isBusy = true;

        var runner = FindObjectOfType<NetworkRunner>();

        if (runner != null)
        {
            var sessionHandler = FindObjectOfType<GameSessionHandler>();

            if (sessionHandler != null)
            {
                // Chama o RPC no objeto de rede para notificar todos os clientes
                // A classe GameSessionHandler PRECISA existir na sua cena e ser um NetworkBehaviour
                // sessionHandler.RPC_InitiateRestart(); // Linha Original - Reativar se GameSessionHandler existe

                // Fallback para compilação se GameSessionHandler não for definido:
                StartCoroutine(ShutdownAndRestartRoutine(runner));
            }
            else
            {
                Debug.LogWarning("[GameManager] GameSessionHandler não encontrado. Fazendo shutdown direto.");
                StartCoroutine(ShutdownAndRestartRoutine(runner));
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] NetworkRunner não encontrado. Reiniciando sem shutdown.");
            StartCoroutine(ShutdownAndRestartRoutine(null));
        }
    }

    // RPC para notificar todos os clientes a fazerem shutdown (executado em todos os clientes)
    // [Este RPC precisa ser movido para um NetworkBehaviour (ex: GameSessionHandler) para funcionar]
    // [Rpc(RpcSources.All, RpcTargets.All)] 
    private void RPC_InitiateRestart()
    {
        var runner = FindObjectOfType<NetworkRunner>();
        if (runner != null)
        {
            StartCoroutine(ShutdownAndRestartRoutine(runner));
        }
    }

    public IEnumerator ShutdownAndRestartRoutine(NetworkRunner runner)
    {
        if (runner != null)
        {
            Debug.Log("[GameManager] Iniciando shutdown da sessão para todos os jogadores.");
            yield return runner.Shutdown();
            Debug.Log("[GameManager] Sessão encerrada. Preparando para abrir uma nova sessão.");
        }

        IsGameActive = true;
        isGameOverTriggered = false;

        yield return new WaitForSeconds(0.4f);

        string currentSceneName = SceneManager.GetActiveScene().name;
        yield return SceneManager.LoadSceneAsync(currentSceneName);

        isBusy = false;
        Debug.Log("[GameManager] Nível reiniciado com nova sessão.");
    }

    public void ReturnToMenuButton()
    {
        if (isBusy) return;
        isBusy = true;
        StartCoroutine(ReturnToMenuRoutine());
    }

    private IEnumerator ReturnToMenuRoutine()
    {
        IsGameActive = true;
        isGameOverTriggered = false;
        var runner = FindObjectOfType<NetworkRunner>();

        // PARA A MÚSICA
        if (musicSource != null)
            StartCoroutine(FadeOutMusic());

        if (runner != null)
        {
            yield return runner.Shutdown();
        }
        yield return new WaitForSeconds(0.4f);
        yield return SceneManager.LoadSceneAsync(menuSceneName);
        isBusy = false;
    }

    public void HandleGameOver(GameObject defeatScreenObject)
    {
        // NOVO AJUSTE: Se a vitória já foi acionada, ignore o Game Over.
        if (isGameOverTriggered || isVictoryTriggered) return;

        isGameOverTriggered = true;

        IsGameActive = false;
        Debug.Log("Game Over: Ativando tela de derrota.");

        // PARA A MÚSICA
        if (musicSource != null)
            StartCoroutine(FadeOutMusic());

        if (defeatScreenObject != null)
        {
            defeatScreenObject.SetActive(true);
        }
    }


    public void HandleVictory(GameObject victoryScreenObject)
    {
        // Condição original correta: Se a vitória OU Game Over já foram acionados, ignore.
        if (isVictoryTriggered || isGameOverTriggered) return;

        isVictoryTriggered = true;
        IsGameActive = false;

        // PARA A MÚSICA
        if (musicSource != null)
            StartCoroutine(FadeOutMusic());

        Debug.Log("Vitória! Preparando tela de vitória...");

        StartCoroutine(DelayedVictory(victoryScreenObject));
    }

    private IEnumerator DelayedVictory(GameObject victoryScreenObject)
    {
        float victoryDelay = 2.5f;
        yield return new WaitForSecondsRealtime(victoryDelay);

        if (victoryScreenObject != null)
        {
            victoryScreenObject.SetActive(true);
            Debug.Log("Tela de vitória ativada.");
        }
    }

    private void ReturnToMenuESC()
    {
        if (isBusy) return;
        isBusy = true;

        StartCoroutine(ReturnToMenuRoutine());
    }

    private IEnumerator FadeOutMusic()
    {
        if (musicSource == null || musicSource.clip == null)
            yield break;

        float startVolume = musicSource.volume;

        float t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.unscaledDeltaTime; // ignora TimeScale
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeOutDuration);
            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = startVolume; // volta ao normal para próxima vez
    }

}