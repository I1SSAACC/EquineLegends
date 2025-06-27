using System;
using UnityEngine;
using UnityEngine.UI;

public class CustomSlider : MonoBehaviour
{
    [SerializeField] private Slider _slider;

    public float Value => _slider.value;

    public event Action<float> Changed;

    private void OnEnable() =>
        _slider.onValueChanged.AddListener(OnChanged);

    private void OnDisable() =>
        _slider.onValueChanged.RemoveListener(OnChanged);

    public void SetValue(float value) =>
        _slider.value = value;

    private void OnChanged(float value) =>
        Changed?.Invoke(value);
}