using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager I;
    public static Action<string> OnSceneChanged;

    [Header("Fade")]
    [SerializeField] CanvasGroup canvasFader;        // optional UI fade
    [SerializeField] GameObject meshFaderObject;     // optional blackout mesh (wallsBlack)
    [SerializeField] float fadeTime = 0.35f;
    [SerializeField] AnimationCurve fadeCurve = AnimationCurve.Linear(0, 0, 1, 1);

    List<Material> meshMaterials;

    [Header("Auto Boot (optional)")]
    [SerializeField] bool autoLoadOnStart = true;
    [SerializeField] string firstScene = "Scene00";
    [SerializeField] float bootDelay = 0f;

    string bootstrapSceneName;

    void Awake()
    {
        if (I == null)
        {
            I = this;
            DontDestroyOnLoad(gameObject);

            bootstrapSceneName = SceneManager.GetActiveScene().name;
            CacheMeshMaterials();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void CacheMeshMaterials()
    {
        meshMaterials = new List<Material>();

        if (!meshFaderObject)
            return;

        var renderers = meshFaderObject.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            foreach (var m in r.materials)
                meshMaterials.Add(m);
        }
    }

    void Start()
    {
        OnSceneChanged?.Invoke(SceneManager.GetActiveScene().name);

        if (autoLoadOnStart && !string.IsNullOrEmpty(firstScene))
            StartCoroutine(CoAutoBoot());
    }

    IEnumerator CoAutoBoot()
    {
        if (bootDelay > 0f)
            yield return new WaitForSeconds(bootDelay);

        if (!SceneManager.GetSceneByName(firstScene).isLoaded)
            yield return CoNext(firstScene);
    }

    public void Next(string sceneName)
    {
        StartCoroutine(CoNext(sceneName));
    }

    public void Boot(string sceneName)
    {
        StartCoroutine(CoNext(sceneName));
    }

    IEnumerator CoNext(string sceneName)
    {
        yield return Fade(1f);

        var scn = SceneManager.GetSceneByName(sceneName);
        if (!scn.isLoaded)
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
        OnSceneChanged?.Invoke(sceneName);

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.name != sceneName && s.name != bootstrapSceneName && s.name != "DontDestroyOnLoad")
                SceneManager.UnloadSceneAsync(s);
        }

        yield return Fade(0f);
    }

    IEnumerator Fade(float target)
    {
        float startCanvas = canvasFader ? canvasFader.alpha : 0f;

        float startMeshAlpha = 0f;
        if (meshMaterials != null && meshMaterials.Count > 0)
            startMeshAlpha = meshMaterials[0].color.a;

        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(t / fadeTime);

            float curved = fadeCurve.Evaluate(normalized);

            float finalAlphaCanvas = Mathf.Lerp(startCanvas, target, curved);
            float finalAlphaMesh = Mathf.Lerp(startMeshAlpha, target, curved);

            if (canvasFader)
                canvasFader.alpha = finalAlphaCanvas;

            if (meshMaterials != null)
            {
                foreach (var m in meshMaterials)
                {
                    var c = m.color;
                    c.a = finalAlphaMesh;
                    m.color = c;
                }
            }

            yield return null;
        }

        if (canvasFader)
            canvasFader.alpha = target;

        if (meshMaterials != null)
        {
            foreach (var m in meshMaterials)
            {
                var c = m.color;
                c.a = target;
                m.color = c;
            }
        }
    }
}
