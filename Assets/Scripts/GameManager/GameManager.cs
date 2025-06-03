using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Linq;
using System;

[RequireComponent(typeof(PhotonView))]
public class GameManager : MonoBehaviourPun
{
    public static GameManager Instance { get; private set; }
    //private readonly CardManager CardManager = CardManager.Instance;


    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject boardCardPrefab;

    public int power = 0;

    [Header("Decks")]
    public List<CardData> playerDeck = new(); //Temporary, this deck will be set before the game starts
    public List<CardData> mainDeck = new();
    public List<CardData> superVillainDeck = new();

    [Header("Cards")]
    public List<CardData> playedCards = new();
    public List<CardData> lineUpCards = new();
    public List<CardData> superVillainCards = new();

    [Header("UI Locations")]
    public RectTransform playedCardsTransform;
    public RectTransform lineUpCardsTransform;
    public RectTransform superVillainCardsTransform;
    public RectTransform PlayerSpawnPoint;

    [Header("Players")]
    public List<CardData> characterDeck = new();
    public int currentPlayerIndex;
    public List<int> playerActorNumbers = new();

    public event Action<int> OnTurnEnded; // Event for ending a turn
    public event Action<int> OnPowerChange;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (!ValidateElements()) return;

        SpawnPlayer();

        if (PhotonNetwork.IsMasterClient)
        {
            AssignCharacters();
            CardManager.Instance.InitializeDecks();
            SetLineUp();
            SetupTurnOrder();
        }
    }

    private bool ValidateElements()
    {
        if (PlayerSpawnPoint == null || playerPrefab == null
            || playedCardsTransform == null || lineUpCardsTransform == null)
        {
            Debug.LogError("Some elements in the GameManager are not assigned in the Inspector!", this);
            return false;
        }

        if (mainDeck.Count <= 0 || characterDeck.Count <= 0 || superVillainDeck.Count <= 0)
        {
            Debug.LogError("Some game decks in the GameManager do not have any card data!", this);
            return false;
        }
        return true;
    }

    public void SpawnPlayer()
    {
        GameObject playerObject = PhotonNetwork.Instantiate(playerPrefab.name, PlayerSpawnPoint.localPosition, Quaternion.identity);
        playerObject.transform.SetParent(PlayerSpawnPoint, false);
    }

    private void AssignCharacters()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        List<int> list = Enumerable.Range(0, characterDeck.Count).ToList();

        //Assigns Random character to each player
        foreach (var player in PhotonNetwork.PlayerList)
        {
            int index = UnityEngine.Random.Range(0, list.Count);
            int charIndex = list[index];
            list.RemoveAt(index);

            photonView.RPC(nameof(GameAPI.Instance.RPC_ReceiveCharacterIndex), player, charIndex);
        }
    }

    private void SetLineUp()
    {
        DrawMainDeckCard(5);
        CardManager.Instance.DrawFromSuperVillainDeckToLineUp(1); //TODO: Change number to variable, set in game config
        ManagePower(PowerOperation.Reset);
    }

    public void DrawMainDeckCard(int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            if (mainDeck.Count <= 0)
            {
                Shuffle(mainDeck);
                if (mainDeck.Count == 0) return;
            }

            CardData card = mainDeck[0];
            mainDeck.RemoveAt(0);
            lineUpCards.Add(card);

            PhotonNetwork.Instantiate(boardCardPrefab.name, Vector3.zero, Quaternion.identity, 0,
                new object[] { card.GetCardID(), -1, 0 }
            );

            photonView.RPC(nameof(GameAPI.Instance.RPC_SyncMainDeckAndLineUp), RpcTarget.OthersBuffered, card.GetCardID());
        }
    }

    public void ManagePower(PowerOperation operation, int amount = 0, CardData cardData = null)
    {
        switch (operation)
        {
            case PowerOperation.Add:
                power += amount;
                break;
            case PowerOperation.Subtract:
                if (cardData != null) power -= cardData.GetCardCost();
                else if (cardData == null) power -= amount;
                break;
            case PowerOperation.Reset:
                power = 0;
                break;
        }
    }

    #region Turn Setup
    private void SetupTurnOrder()
    {
        playerActorNumbers.Clear();
        foreach (var player in PhotonNetwork.PlayerList)
        {
            playerActorNumbers.Add(player.ActorNumber);
        }

        Shuffle(playerActorNumbers);
        photonView.RPC(nameof(RPC_SetTurnOrder), RpcTarget.AllBuffered, playerActorNumbers.ToArray());

        currentPlayerIndex = 0;
        photonView.RPC(nameof(RPC_StartTurn), RpcTarget.AllBuffered, playerActorNumbers[currentPlayerIndex]);
    }

    [PunRPC]
    private void RPC_SetTurnOrder(int[] actorNumbers)
    {
        playerActorNumbers = actorNumbers.ToList();
        Debug.Log("Turn order received.");
    }

    [PunRPC]
    public void RPC_StartTurn(int actorNumber)
    {
        currentPlayerIndex = playerActorNumbers.IndexOf(actorNumber);
        Debug.Log($"Starting turn for player with ActorNumber: {actorNumber}");
        bool isCurrentPlayer = PhotonNetwork.LocalPlayer.ActorNumber == actorNumber;
        PlayerManager.Local.SetTurnActive(isCurrentPlayer);
    }

    // Move to the next player
    public void NextTurn()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        currentPlayerIndex = (currentPlayerIndex + 1) % playerActorNumbers.Count;
        Debug.Log($"Next player ActorNumber: {playerActorNumbers[currentPlayerIndex]}");

        photonView.RPC(nameof(RPC_StartTurn), RpcTarget.AllBuffered, playerActorNumbers[currentPlayerIndex]);
    }

    public void RequestEndTurn(int actorNumber)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            ProcessEndTurn(actorNumber);
        }
        else
        {
            Debug.Log("GameManager - RequestEndTurn - NotMasterClient |  playerActorNumber: " + actorNumber);
            photonView.RPC(nameof(RPC_RequestEndTurn), RpcTarget.MasterClient, actorNumber);
        }
    }

    [PunRPC]
    public void RPC_RequestEndTurn(int actorNumber)
    {
        if (PhotonNetwork.IsMasterClient) { ProcessEndTurn(actorNumber); }
    }

    private void ProcessEndTurn(int actorNumber)
    {
        if (actorNumber != playerActorNumbers[currentPlayerIndex]) return;

        photonView.RPC(nameof(RPC_OnTurnEnded), RpcTarget.All, actorNumber);
        NextTurn();
    }

    [PunRPC]
    public void RPC_OnTurnEnded(int actorNumber)
    {
        Debug.Log($"RPC_OnTurnEnded called for playerActorNumber: {actorNumber}");
        OnTurnEnded?.Invoke(actorNumber);
    }
    #endregion

    #region Helper
    private void Shuffle<T>(List<T> list)
    {
        var rand = new System.Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rand.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
    #endregion
}

#region Enums
public enum Ownership { Unknown, Local, Remote, Shared }
public enum PowerOperation { Add, Subtract, Reset }
#endregion