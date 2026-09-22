using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLobbyUI : MonoBehaviour
{
    public Image playerImage;
    public TextMeshProUGUI playerNameText;
    public GameObject unReadyStatus;
    public GameObject readyStatus;
    private bool ready = false;

    public void Setup(Sprite _playerImage, string _playerName, bool _readyStatus)
    {
        playerImage.sprite = _playerImage;
        playerNameText.text = _playerName;
        unReadyStatus.SetActive(!_readyStatus);
        readyStatus.SetActive(_readyStatus);
    }

    public void SetReadyStatus(bool _readyStatus)
    {
        ready = _readyStatus;
        readyStatus.SetActive(ready);
        unReadyStatus.SetActive(!ready);
    }
}