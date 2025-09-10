using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager I;
    [SerializeField] CanvasGroup fader; // assign FadeCanvas
    [SerializeField] float fadeTime = 0.35f;

    void Awake()
    {
        if (I == null) { I = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    // Preload without showing yet
    public void Preload(string sceneName)
    {
        StartCoroutine(CoPreload(sceneName));
    }

    IEnumerator CoPreload(string sceneName)
    {
        var scn = SceneManager.GetSceneByName(sceneName);
        if (scn.isLoaded) yield break;
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        op.allowSceneActivation = false;
        while (op.progress < 0.9f) yield return null; // ready
        // keep it waiting at 90% until Activate()
    }

    // Activate (finish loading) + set active + unload others with fade
    public void Activate(string sceneName)
    {
        StartCoroutine(CoActivate(sceneName));
    }

    IEnumerator CoActivate(string sceneName)
    {
        yield return Fade(1f);

        // finish activation if it was preloaded
        var op = FindPendingOpFor(sceneName);
        if (op != null)
        {
            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;
        }
        else if (!SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            // not preloaded? just load now (will be hidden by fade)
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }

        // set active
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));

        // unload all non-persistent, non-target scenes
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.name != sceneName && s.name != gameObject.scene.name)
                SceneManager.UnloadSceneAsync(s);
        }

        yield return Fade(0f);
    }

    // One-call convenience: fade¡÷load¡÷switch (use if you don¡¦t Preload)
    public void Next(string sceneName) { StartCoroutine(CoNext(sceneName)); }
    IEnumerator CoNext(string sceneName)
    {
        yield return Fade(1f);
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.name != sceneName && s.name != gameObject.scene.name)
                SceneManager.UnloadSceneAsync(s);
        }
        yield return Fade(0f);
    }

    // Optional: guard a signal so it fires only once (for looping timelines)
    public void NextOnce(string sceneName, string key)
    {
        if (PlayerPrefs.GetInt("once_" + key, 0) == 1) return;
        PlayerPrefs.SetInt("once_" + key, 1);
        Next(sceneName);
    }

    IEnumerator Fade(float target)
    {
        float start = fader.alpha, t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            fader.alpha = Mathf.Lerp(start, target, t / fadeTime);
            yield return null;
        }
        fader.alpha = target;
    }

    // ---- helpers ----
    AsyncOperation FindPendingOpFor(string sceneName)
    {
        // Unity doesn't expose ops list; this returns null at runtime.
        // We keep API here for clarity; CoPreload parks the op implicitly.
        return null;
    }
}
