using UnityEngine;
using TMPro;

public class GameUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _playerNickname;
    [SerializeField] private TMP_Text _playerLevel;

    public void SetupPlayerInfo(PlayerData data)
    {
        _playerNickname.text = data.nickname;
        _playerLevel.text = data.level.ToString();
    }
}