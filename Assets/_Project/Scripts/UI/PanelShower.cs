using UnityEngine;

public class PanelShower : MonoBehaviour
{
    [SerializeField] private SfxPlayer _sfxPlayer;
    [SerializeField] private CustomPanel _logInPanel;
    [SerializeField] private CustomPanel _signUpPanel;

    private void Awake()
    {
        _logInPanel.Hide();
        _signUpPanel.Hide();
    }

    private void OnEnable()
    {
        _logInPanel.OpenClicked += OnLogInButtonOpenClicked;
        _logInPanel.CloseClicked += OnLogInButtonCloseClicked;
        _signUpPanel.OpenClicked += OnSignUpButtonOpenClicked;
        _signUpPanel.CloseClicked += OnSignUpButtonCloseClicked;
    }

    private void OnDisable()
    {
        _logInPanel.OpenClicked -= OnLogInButtonOpenClicked;
        _logInPanel.CloseClicked -= OnLogInButtonCloseClicked;
        _signUpPanel.OpenClicked -= OnSignUpButtonOpenClicked;
        _signUpPanel.CloseClicked -= OnSignUpButtonCloseClicked;
    }

    private void OnLogInButtonOpenClicked()
    {
        _logInPanel.Show();
        _sfxPlayer.PlayButtonClick();
    }

    private void OnLogInButtonCloseClicked()
    {
        _logInPanel.Hide();
        _sfxPlayer.PlayButtonClick();
    }

    private void OnSignUpButtonOpenClicked()
    {
        _signUpPanel.Show();
        _sfxPlayer.PlayButtonClick();
    }

    private void OnSignUpButtonCloseClicked()
    {
        _signUpPanel.Hide();
        _sfxPlayer.PlayButtonClick();
    }
}