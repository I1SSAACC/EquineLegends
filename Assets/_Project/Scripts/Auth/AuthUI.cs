using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class AuthUI : MonoBehaviour
{
    [Header("Регистрация")]
    [SerializeField] private TMP_InputField _regEmailInput;
    [SerializeField] private TMP_InputField _regLoginInput;
    [SerializeField] private TMP_InputField _regPasswordInput;
    [SerializeField] private TMP_Text _regFeedbackText;

    [Header("Авторизация")]
    [SerializeField] private TMP_InputField _loginLoginInput;
    [SerializeField] private TMP_InputField _loginPasswordInput;
    [SerializeField] private TMP_Text _loginFeedbackText;

    [Header("Управление")]
    [SerializeField] private Button _registerButton;
    [SerializeField] private Button _loginButton;

    [Header("UI Canvas")]
    [SerializeField] private GameObject _authCanvas;
    [SerializeField] private TMP_Text _playerInfoText;

    private void Start()
    {
        if (_registerButton != null)
            _registerButton.onClick.AddListener(OnRegisterButtonClicked);

        if (_loginButton != null)
            _loginButton.onClick.AddListener(OnLoginButtonClicked);
    }

    public void OnRegisterButtonClicked()
    {
        if (NetworkClient.isConnected == false)
        {
            _regFeedbackText.text = "Нет соединения с сервером.";
            return;
        }

        RegisterRequestMessage registerMessage = new()
        {
            email = _regEmailInput.text,
            login = _regLoginInput.text,
            password = _regPasswordInput.text
        };

        NetworkClient.Send(registerMessage);
        NetworkClient.RegisterHandler<RegisterResponseMessage>(OnRegisterResponse);
    }

    public void OnLoginButtonClicked()
    {
        if (NetworkClient.isConnected == false)
        {
            _loginFeedbackText.text = "Нет соединения с сервером.";
            return;
        }

        LoginRequestMessage loginMessage = new()
        {
            login = _loginLoginInput.text,
            password = _loginPasswordInput.text
        };

        NetworkClient.Send(loginMessage);
        NetworkClient.RegisterHandler<LoginResponseMessage>(OnLoginResponse);
    }

    private void OnRegisterResponse(RegisterResponseMessage msg) =>
        Debug.Log("Ответ регистрации: " + msg.message);

    private void OnLoginResponse(LoginResponseMessage msg)
    {
        Debug.Log("OnLoginResponse в AuthUI запущен!");
        if (msg.success == false)
        {
            _loginFeedbackText.text = "Ошибка авторизации: " + msg.message;
            return;
        }

        //_loginFeedbackText.text = msg.message;

        Debug.Log("Полученный JSON игрока: " + msg.playerDataJson);

        if (string.IsNullOrEmpty(msg.playerDataJson) == true)
        {
            Debug.LogWarning("Не найден JSON с данными игрока.");
            return;
        }

        PlayerData playerData = JsonUtility.FromJson<PlayerData>(msg.playerDataJson);
        if (playerData == null)
        {
            Debug.LogError("Ошибка десериализации JSON игрока: " + msg.playerDataJson);
            return;
        }

        Debug.Log($"ID: {playerData.id} | Ник: {playerData.nickname} | Уровень: {playerData.level} | Валюта: {playerData.gameCurrency} | Донат: {playerData.donationCurrency}");

        if (_playerInfoText != null)
        {
            _playerInfoText.text =
                $"ID: {playerData.id}\n" +
                $"Ник: {playerData.nickname}\n" +
                $"Уровень: {playerData.level}\n" +
                $"Валюта: {playerData.gameCurrency}\n" +
                $"Донат: {playerData.donationCurrency}";
        }
        else
        {
            Debug.LogWarning("PlayerInfoText не назначен в инспекторе!");
        }

        //if (_authCanvas != null)
        //    _authCanvas.SetActive(false);
    }
}