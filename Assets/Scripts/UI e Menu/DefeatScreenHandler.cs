using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

public class DefeatScreenHandler : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private CanvasGroup canvasGroup; //fade
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private GameObject menuButton;

    [Header("Configurações")]
    [SerializeField] private float fadeInDuration = 2.0f;
    [SerializeField] private float delayBeforeButton = 3.0f;

    private void Awake()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        menuButton.SetActive(false);
        gameObject.SetActive(false);
    }

    public void StartDefeatSequence()
    {
        gameObject.SetActive(true);
        StartCoroutine(DefeatRoutine());
    }

    private IEnumerator DefeatRoutine()
    {
        videoPlayer.Play();
        audioSource.Play();

        // fade in da tela
        float timer = 0;
        while (timer < fadeInDuration)
        {
            timer += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, timer / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // delay do botao

        yield return new WaitForSeconds(delayBeforeButton);

        menuButton.SetActive(true);
    }

    public void GoToMenu()
    {
        GameManager.Instance.ReturnToMenu();

    }
}
