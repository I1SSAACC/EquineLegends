using UnityEngine;
using TMPro;

public class PlaceHolderDisable : MonoBehaviour
{
    [SerializeField] private TMP_InputField _source;
    [SerializeField] private Behaviour[] _affectedComponents;

    private void Awake()
    {
        if (_source == null)
        {
            enabled = false;
            return;
        }

        _source.onValueChanged.AddListener(OnChange);
    }

    private void OnChange(string text)
    {
        foreach (Behaviour component in _affectedComponents)
            component.enabled = string.IsNullOrEmpty(text);
    }
}