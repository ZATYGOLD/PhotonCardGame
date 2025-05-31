using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ReadyButtonController : MonoBehaviour
{
    public static ReadyButtonController Instance { get; private set; }

    private TMP_Text buttonText;
    private Image buttonImage;
    private Button button;
    private ExitGames.Client.Photon.Hashtable playerProperties = new();

    private void Awake()
    {
        // Get components
        buttonText = GetComponentInChildren<TMP_Text>();
        buttonImage = GetComponent<Image>();
        button = GetComponent<Button>();

        // Ensure the button is properly set up
        if (button == null || buttonText == null || buttonImage == null)
        {
            Debug.LogError("Button components are not assigned correctly.");
        }

        Instance = this;
    }

    private void Update()
    {
        UpdateButtonState();
    }

    public void ResetReadyButton()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            return;
        }

        playerProperties["isReady"] = false;
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
        UpdateButtonState();
    }

    // Updates the button text and color, and adds the appropriate listener
    private void UpdateButtonState()
    {
        if (PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            if (!PhotonNetwork.IsConnectedAndReady)
            {
                return;
            }

            playerProperties["isReady"] = true;
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);

            buttonText.text = "START GAME";
            buttonImage.color = Color.green;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClickStartGame);

            button.interactable = AllPlayersReady();
        }
        else
        {
            bool isReady = playerProperties.ContainsKey("isReady") && (bool)playerProperties["isReady"];

            buttonText.text = "READY";
            buttonImage.color = isReady ? Color.green : Color.red;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ToggleReadyState());

            button.interactable = true;
        }
    }


    private bool AllPlayersReady()
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount < PhotonNetwork.CurrentRoom.MaxPlayers)
        {
            return false;
        }

        foreach (KeyValuePair<int, Player> player in PhotonNetwork.CurrentRoom.Players)
        {
            if (player.Value.CustomProperties.ContainsKey("isReady"))
            {
                if (!(bool)player.Value.CustomProperties["isReady"])
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        return true;
    }

    // Toggles the ready state and updates the button color
    private void ToggleReadyState()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            return;
        }

        bool isReady = playerProperties.ContainsKey("isReady") && (bool)playerProperties["isReady"];
        isReady = !isReady;
        playerProperties["isReady"] = isReady;
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);

        buttonImage.color = isReady ? Color.green : Color.red;
    }

    // Action for the MasterClient to start the game
    private void OnClickStartGame()
    {
        PhotonNetwork.LoadLevel("GameScene");
    }
}