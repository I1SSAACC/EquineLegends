using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : NetworkBehaviour
{
    public float ActivationThreshold = 0.9f;

    [SerializeField] private CanvasGroup _loadingCanvas;
    [SerializeField] private Slider _loadingSlider;
    [SerializeField] private string _gameSceneName = "Game";
    [SerializeField] private float _progressSmoothSpeed = 1.5f;
    [SerializeField] private float _fadeDuration = 1f;

    public static SceneTransitionManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _loadingCanvas.gameObject.SetActive(false);
    }

    public void StartGameLoad()
    {
        _loadingCanvas.gameObject.SetActive(true);
        StartCoroutine(LoadGameRoutine());
    }

    private IEnumerator LoadGameRoutine()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(_gameSceneName);
        operation.allowSceneActivation = false;

        yield return FadeCanvas(_loadingCanvas, 0f, 1f);
        _loadingSlider.value = 0f;

        while (operation.progress < ActivationThreshold)
        {
            float normalized = Mathf.Clamp01(operation.progress / ActivationThreshold);
            UpdateSliderProgress(normalized);
            yield return null;
        }

        //while (operation.progress < 0.9f)
        //{
        //    UpdateSliderProgress(Mathf.Clamp01(operation.progress / 0.9f));
        //    yield return null;
        //}

        while (_loadingSlider.value < 1f)
        {
            UpdateSliderProgress(1f);
            yield return null;
        }

        operation.allowSceneActivation = true;

        while (operation.isDone == false)
            yield return null;

        NetworkClient.Send(new MovePlayerMessage());

        yield return new WaitForSeconds(0.2f);
        yield return FadeCanvas(_loadingCanvas, 1f, 0f);
        _loadingCanvas.gameObject.SetActive(false);
        InitializeGameUI();
    }

    private IEnumerator FadeCanvas(CanvasGroup canvasGroup, float from, float to)
    {
        float elapsed = 0f;
        canvasGroup.alpha = from;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / _fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private void UpdateSliderProgress(float targetProgress)
    {
        _loadingSlider.value = Mathf.MoveTowards(
            _loadingSlider.value,
            targetProgress,
            Time.deltaTime * _progressSmoothSpeed
        );
    }

    private void InitializeGameUI()
    {
        GameUI gameUI = FindFirstObjectByType<GameUI>();

        if (gameUI == null)
            throw new System.InvalidOperationException("GameUI: не был найден на загруженной сцене");

        if (AuthManager.Instance.CurrentPlayerData == null)
            throw new System.InvalidOperationException("CurrentPlayerData: отсутствуют данные игрока");

        gameUI.SetupPlayerInfo(AuthManager.Instance.CurrentPlayerData);
    }
}