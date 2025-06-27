using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    public CanvasGroup loadingCanvas;

    public Slider loadingSlider;

    public string gameSceneName = "Game";
    public float fadeDuration = 0.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (loadingCanvas != null) DontDestroyOnLoad(loadingCanvas.gameObject);
        if (loadingSlider != null) DontDestroyOnLoad(loadingSlider.gameObject);
        if (loadingCanvas != null) loadingCanvas.gameObject.SetActive(false);
    }

    public void StartGameLoad()
    {
        if (loadingCanvas != null) loadingCanvas.gameObject.SetActive(true);
        if (loadingSlider != null)
        {
            loadingSlider.minValue = 0f;
            loadingSlider.maxValue = 1f;
            loadingSlider.value = 0f;
        }

        StartCoroutine(LoadGameCoroutine());
    }

    private IEnumerator LoadGameCoroutine()
    {
        yield return FadeCanvas(loadingCanvas, 0f, 1f);

        AsyncOperation op = SceneManager.LoadSceneAsync(gameSceneName);
        op.allowSceneActivation = true;

        while (!op.isDone)
        {
            float prog = Mathf.Clamp01(op.progress / 0.9f);
            if (loadingSlider != null) loadingSlider.value = prog;
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        yield return FadeCanvas(loadingCanvas, 1f, 0f);

        loadingCanvas.interactable = false;
        loadingCanvas.blocksRaycasts = false;

        InitializeGameUI();
    }

    private IEnumerator FadeCanvas(CanvasGroup cg, float from, float to)
    {
        if (cg == null) yield break;
        float elapsed = 0f;
        cg.alpha = from;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        cg.alpha = to;
    }

    private void InitializeGameUI()
    {
        var gameUI = FindObjectOfType<GameUI>();
        if (gameUI != null && AuthManager.Instance.CurrentPlayerData != null)
            gameUI.SetupPlayerInfo(AuthManager.Instance.CurrentPlayerData);
    }
}
