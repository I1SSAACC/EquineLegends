using UnityEngine;
using TMPro;

public class GameUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _playerInfoText;

    public void SetupPlayerInfo(PlayerData data)
    {
        _playerInfoText.text = data.nickname;
    }
}