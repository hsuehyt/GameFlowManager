using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SceneInfoTimerUI : MonoBehaviour
{
    public TextMeshProUGUI sceneNameText;
    public TextMeshProUGUI timeText;

    float startTime;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        startTime = Time.time;

        // Listen to GameFlowManager scene change event
        GameFlowManager.OnSceneChanged += UpdateSceneNameCorrectly;

        // Also update initial
        UpdateSceneNameCorrectly(SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        GameFlowManager.OnSceneChanged -= UpdateSceneNameCorrectly;
    }

    void UpdateSceneNameCorrectly(string newSceneName)
    {
        if (sceneNameText)
            sceneNameText.text = newSceneName;
    }

    void Update()
    {
        float elapsed = Time.time - startTime;
        int h = (int)(elapsed / 3600);
        int m = (int)(elapsed / 60) % 60;
        int s = (int)elapsed % 60;
        if (timeText) timeText.text = $"{h:00}:{m:00}:{s:00}";
    }
}
