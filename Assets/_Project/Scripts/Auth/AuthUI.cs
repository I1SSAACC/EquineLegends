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

    private void OnEnable()
    {
        _registerButton.onClick.AddListener(OnRegisterButtonClicked);
        _loginButton.onClick.AddListener(OnLoginButtonClicked);
    }

    private void OnDisable()
    {
        _registerButton.onClick.RemoveListener(OnRegisterButtonClicked);
        _loginButton.onClick.RemoveListener(OnLoginButtonClicked);
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
        if (!msg.success)
        {
            _loginFeedbackText.text = $"Ошибка авторизации: {msg.message}";
            return;
        }

        AuthManager.Instance.CurrentPlayerData = new PlayerData
        {
            id = msg.accountId,
            nickname = msg.nickname,
            gameCurrency = msg.gameCurrency,
            donationCurrency = msg.donationCurrency,
            level = msg.level
        };

        SceneTransitionManager.Instance.StartGameLoad();
    }

}