
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerEntry : MonoBehaviourPunCallbacks
{
    [SerializeField] private TMP_Text playerName;
    [SerializeField] private Button kickButton;

    private Player player;

    public void Setup(Player player)
    {
        if (playerName == null || kickButton == null)
        {
            Debug.LogError("Some elements in the PlayerListEntry are not assigned in the Inspector!");
            return;
        }

        this.player = player;

        playerName.text = player.NickName;

        bool canKick = PhotonNetwork.IsMasterClient && player != PhotonNetwork.MasterClient;
        kickButton.gameObject.SetActive(canKick);

        if (canKick)
        {
            kickButton.onClick.RemoveAllListeners();
            kickButton.onClick.AddListener(() => OnClickKick());
        }
    }

    private void OnClickKick()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            KickPlayer(player.ActorNumber); // Use the ActorNumber of the player to kick
        }
    }

    private void KickPlayer(int playerID)
    {
        // Send an RPC to that player to kick them out
        photonView.RPC("KickPlayerRPC", RpcTarget.Others, playerID);

    }

    [PunRPC]
    private void KickPlayerRPC(int playerID)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == playerID)
        {
            PhotonNetwork.LeaveRoom();
        }
    }
}
