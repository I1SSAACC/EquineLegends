using System;
using UnityEngine;

public class CustomPanel : MonoBehaviour
{
    [SerializeField] private GameObject _content;
    [SerializeField] private ButtonClickInformer _openButton;
    [SerializeField] private ButtonClickInformer _closeButton;

    public event Action OpenClicked;
    public event Action CloseClicked;

    private void OnEnable()
    {
        _openButton.Clicked += OnOpenClicked;
        _closeButton.Clicked += OnCloseClicked;
    }

    private void OnDisable()
    {
        _openButton.Clicked -= OnOpenClicked;
        _closeButton.Clicked -= OnCloseClicked;
    }

    public void Show() =>
        _content.SetActive(true);

    public void Hide() =>
        _content.SetActive(false);

    private void OnOpenClicked() =>
        OpenClicked?.Invoke();

    private void OnCloseClicked() =>
        CloseClicked?.Invoke();
}