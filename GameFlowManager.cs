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
    [SerializeField] CanvasGroup canvasFader;        // optional (UI)
    [SerializeField] GameObject meshFaderObject;     // optional (custom blackout mesh)
    [SerializeField] float fadeTime = 0.35f;

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
        // Fade OUT
        yield return Fade(1f);

        // Load destination scene if not yet loaded
        var scn = SceneManager.GetSceneByName(sceneName);
        if (!scn.isLoaded)
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        // Set active
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
        OnSceneChanged?.Invoke(sceneName);

        // Unload everything except bootstrap + dontdestroyonload + target
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.name != sceneName && s.name != bootstrapSceneName && s.name != "DontDestroyOnLoad")
                SceneManager.UnloadSceneAsync(s);
        }

        // Fade IN
        yield return Fade(0f);
    }

    IEnumerator Fade(float target)
    {
        float startCanvas = canvasFader ? canvasFader.alpha : 0f;

        // If mesh exists ¡÷ read its first material's alpha
        float startMeshAlpha = 0f;
        if (meshMaterials != null && meshMaterials.Count > 0)
            startMeshAlpha = meshMaterials[0].color.a;

        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            float lerp = t / fadeTime;

            // UI CanvasGroup fading
            if (canvasFader)
                canvasFader.alpha = Mathf.Lerp(startCanvas, target, lerp);

            // Mesh-based fading (wallsBlack)
            if (meshMaterials != null)
            {
                float newAlpha = Mathf.Lerp(startMeshAlpha, target, lerp);
                foreach (var m in meshMaterials)
                {
                    Color c = m.color;
                    c.a = newAlpha;
                    m.color = c;
                }
            }

            yield return null;
        }

        // Snap at end
        if (canvasFader)
            canvasFader.alpha = target;

        if (meshMaterials != null)
        {
            foreach (var m in meshMaterials)
            {
                Color c = m.color;
                c.a = target;
                m.color = c;
            }
        }
    }
}
