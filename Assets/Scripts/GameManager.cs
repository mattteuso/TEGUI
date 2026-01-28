using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Cenas")]
    [SerializeField] private string menuSceneName = "MainMenu";

    [Header("Musica")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private float fadeOutDuration = 1.5f;

    [Header("Derrota e vitoria")]
    [SerializeField] private VictoryScreenHandler victoryHandler;

    //[Header("Defeat System")]
    //[SerializeField] private GameObject defeatPanel; 

    public bool IsGameActive { get; private set; } = true;
    private bool isBusy;

    public static GameManager ActiveHandler => Instance;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null)
            musicSource = FindObjectOfType<AudioSource>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            ReturnToMenu();
    }

    // =================================================================
    // SISTEMA DE DERROTA
    // =================================================================

    public void EndGameLocal()
    {
        if (!IsGameActive) return;

        Debug.Log("Derrota confirmada!");
        IsGameActive = false;

        //// Ativa o painel de derrota (UI)
        //if (defeatPanel != null)
        //{
        //    defeatPanel.SetActive(true);
        //}

        
        //Time.timeScale = 0f; acho que tem jeito melhor pra fazer isso

        // 
        StartCoroutine(FadeOutMusicRoutine());
    }

    // =================================================================
    // SISTEMA DE VITORIA
    // =================================================================

    public void WinGameLocal()
    {
        if (!IsGameActive) return;

        Debug.Log("Vitória confirmada!");
        IsGameActive = false;

        if (victoryHandler != null)
        {
            victoryHandler.StartVictorySequence();
        }
        

        
        StartCoroutine(FadeOutMusicRoutine());
    }


    // =================================================================
    // GAME FLOW
    // =================================================================

    public void RestartLevel()
    {
        if (isBusy) return;
        Time.timeScale = 1f; // tempo volte ao normal
        StartCoroutine(RestartRoutine());
    }

    public void ReturnToMenu()
    {
        if (isBusy) return;
        Time.timeScale = 1f;
        StartCoroutine(ReturnToMenuRoutine());
    }

    // =================================================================
    // ROUTINES
    // =================================================================

    private IEnumerator RestartRoutine()
    {
        isBusy = true;
        IsGameActive = true;

        if (musicSource != null)
            yield return FadeOutMusicRoutine();

        yield return new WaitForSecondsRealtime(0.3f);

        string sceneName = SceneManager.GetActiveScene().name;
        yield return SceneManager.LoadSceneAsync(sceneName);

        isBusy = false;
    }

    private IEnumerator ReturnToMenuRoutine()
    {
        isBusy = true;
        IsGameActive = true;

        if (musicSource != null)
            yield return FadeOutMusicRoutine();

        yield return new WaitForSecondsRealtime(0.3f);
        yield return SceneManager.LoadSceneAsync(menuSceneName);

        isBusy = false;
    }

    private IEnumerator FadeOutMusicRoutine()
    {
        if (musicSource == null || !musicSource.isPlaying)
            yield break;

        float startVolume = musicSource.volume;
        float t = 0f;

        while (t < fadeOutDuration)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeOutDuration);
            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = startVolume;
    }
}