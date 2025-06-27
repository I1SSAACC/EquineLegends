using UnityEngine;

public class SfxPlayer : MonoBehaviour
{
    [SerializeField] private AudioSource _source;
    [SerializeField] private AudioClip _clickButton;
    [SerializeField] private AudioClip _footstepsClip;

    public void PlayButtonClick() =>
        _source.PlayOneShot(_clickButton);

    public void PlayFootsteps() =>
        _source.PlayOneShot(_footstepsClip);
}