using UnityEngine;

public class PanelShower : MonoBehaviour
{
    [SerializeField] private SfxPlayer _sfxPlayer;
    [SerializeField] private CustomPanel _logInPanel;
    [SerializeField] private CustomPanel _signUpPanel;
    [SerializeField] private CustomPanel _settingsPanel;

    private void Awake()
    {
        if (_logInPanel != null)
            _logInPanel.Hide();

        if (_signUpPanel != null)
            _signUpPanel.Hide();

        if (_settingsPanel != null)
            _settingsPanel.Hide();
    }

    private void OnEnable()
    {
        if(_logInPanel != null)
        {
            _logInPanel.OpenClicked += OnLogInButtonOpenClicked;
            _logInPanel.CloseClicked += OnLogInButtonCloseClicked;
        }

        if (_signUpPanel != null)
        {
            _signUpPanel.OpenClicked += OnSignUpButtonOpenClicked;
            _signUpPanel.CloseClicked += OnSignUpButtonCloseClicked;
        }
        
        if(_settingsPanel != null)
        {
            _settingsPanel.OpenClicked += OnSettingsButtonOpenClicked;
            _settingsPanel.CloseClicked += OnSettingsButtonCloseClicked;
        }
    }

    private void OnDisable()
    {
        if(_logInPanel != null)
        {
            _logInPanel.OpenClicked -= OnLogInButtonOpenClicked;
            _logInPanel.CloseClicked -= OnLogInButtonCloseClicked;
        }

        if (_signUpPanel != null)
        {
            _signUpPanel.OpenClicked -= OnSignUpButtonOpenClicked;
            _signUpPanel.CloseClicked -= OnSignUpButtonCloseClicked;
        }

        if (_settingsPanel != null)
        {
            _settingsPanel.OpenClicked -= OnSettingsButtonOpenClicked;
            _settingsPanel.CloseClicked -= OnSettingsButtonCloseClicked;
        }
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

    private void OnSettingsButtonOpenClicked()
    {
        _settingsPanel.Show();
        _sfxPlayer.PlayButtonClick();
    }

    private void OnSettingsButtonCloseClicked()
    {
        _settingsPanel.Hide();
        _sfxPlayer.PlayButtonClick();
    }
}