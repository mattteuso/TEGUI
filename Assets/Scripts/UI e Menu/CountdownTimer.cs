using TMPro;
using UnityEngine;

public class CountdownTimer : MonoBehaviour
{
    [Header("Config")]
    public float startTime = 10f;

    public float CurrentTime { get; private set; }

    [Header("UI")]
    public TextMeshProUGUI timerText;

    private bool timerIsRunning = false;

    private void Start()
    {
        CurrentTime = startTime;
        timerIsRunning = true;
        UpdateUI();
    }

    private void Update()
    {
        if (!timerIsRunning) return;

        if (CurrentTime > 0)
        {
            CurrentTime -= Time.deltaTime;

            if (CurrentTime <= 0)
            {
                CurrentTime = 0;
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.EndGameLocal();
                }
            }
        }

        UpdateUI();
    }


    void UpdateUI()
    {
        if (timerText == null) return;

        int m = Mathf.FloorToInt(CurrentTime / 60f);
        int s = Mathf.FloorToInt(CurrentTime % 60f);

        timerText.text = string.Format("{0:0}:{1:00}", m, s);
    }
}