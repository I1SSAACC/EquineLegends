using UnityEngine;
using TMPro;

public class GameUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _playerInfoText;

    public void SetupPlayerInfo(PlayerData data)
    {
        _playerInfoText.text =
            $"ID: {data.id}\n" +
            $"Ник: {data.nickname}\n" +
            $"Уровень: {data.level}\n" +
            $"Валюта: {data.gameCurrency}\n" +
            $"Донат: {data.donationCurrency}";
    }
}