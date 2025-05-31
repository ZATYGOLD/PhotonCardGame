using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomEntry : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private Button joinButton;

    private string roomName;

    public void Setup(RoomInfo roomInfo)
    {
        if (roomInfo == null || roomNameText == null || playerCountText == null || joinButton == null)
        {
            Debug.LogError("Some elements in the RoomItemPrefab are not assigned in the Inspector!");
            return;
        }

        roomName = roomInfo.Name;
        roomNameText.text = roomName;
        playerCountText.text = $"{roomInfo.PlayerCount}/{roomInfo.MaxPlayers}";
        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(() => LobbyManager.Instance.JoinRoom(roomName));
    }
}
