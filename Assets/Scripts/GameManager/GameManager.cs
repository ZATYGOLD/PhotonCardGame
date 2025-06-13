using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System;
using System.Linq;
using System.Collections;

[RequireComponent(typeof(PhotonView))]
public class GameManager : MonoBehaviourPun
{
    public static GameManager Instance { get; private set; }
    private PhotonView NetworkManagerView;
    //private readonly CardManager CardManager = CardManager.Instance;


    [Header("Prefabs")]
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

    [Header("Players")]
    public List<CardData> characterDeck = new();
    public int currentPlayerIndex;
    public List<int> playerActorNumbers = new();

    public event Action<int> OnTurnEnded; // Event for ending a turn
    public event Action<int> OnPowerChange;

    private bool IsCharacterDeckShuffled = false;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        NetworkManagerView = NetworkManager.Instance.photonView;
    }

    private void Start()
    {
        if (!ValidateElements()) return;

        if (PhotonNetwork.IsMasterClient)
        {
            InitializeDecks();
            SetLineUp();
            TurnManager.Instance.SetupTurnOrder();
        }

        StartCoroutine(SpawnPlayers());
    }

    private bool ValidateElements()
    {
        if (playedCardsTransform == null || lineUpCardsTransform == null)
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

    private IEnumerator SpawnPlayers()
    {
        yield return new WaitUntil(() => IsCharacterDeckShuffled);
        PlayerSpawner.Instance.SpawnLocalPlayer();
    }

    private void InitializeDecks()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        Shuffle(mainDeck);
        NetworkManagerView.RPC(nameof(NetworkManager.Instance.RPC_SyncMainDeck), RpcTarget.OthersBuffered, CardManager.Instance.ConvertCardDataToIds(mainDeck));
        Shuffle(superVillainDeck);
        NetworkManagerView.RPC(nameof(NetworkManager.Instance.RPC_SyncSuperVillainDeck), RpcTarget.OthersBuffered, CardManager.Instance.ConvertCardDataToIds(superVillainDeck));
        ShuffleCharacters();
    }

    private void SetLineUp()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        DrawMainDeckCard(5);
        DrawSuperVillaincard();
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
                new object[] { card.GetCardID(), -1, 0 } //0 is for LineUpArea
            );

            NetworkManagerView.RPC(nameof(NetworkManager.Instance.RPC_SyncLineUp), RpcTarget.OthersBuffered, card.GetCardID());
        }
    }

    public void DrawSuperVillaincard(int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            if (superVillainDeck.Count <= 0)
            {
                Shuffle(superVillainDeck);
                if (superVillainDeck.Count == 0) return;
            }

            CardData card = superVillainDeck[0];
            superVillainDeck.RemoveAt(0);
            superVillainCards.Add(card);

            PhotonNetwork.Instantiate(boardCardPrefab.name, Vector3.zero, Quaternion.identity, 0,
                new object[] { card.GetCardID(), -1, 1 } //1 is for SuperVillainArea
            );

            NetworkManagerView.RPC(nameof(NetworkManager.Instance.RPC_SyncSuperVillain), RpcTarget.OthersBuffered, card.GetCardID());
        }
    }


    private void ShuffleCharacters()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        Shuffle(characterDeck);
        IsCharacterDeckShuffled = true;
        //var ids = CardManager.Instance.ConvertCardDataToIds(characterDeck);
        photonView.RPC(nameof(RPC_SyncCharacterDeck), RpcTarget.AllBuffered,
            characterDeck.Select(c => c.GetCardID()).ToArray());
    }

    [PunRPC]
    private void RPC_SyncCharacterDeck(int[] ids)
    {
        characterDeck = ids.Select(id => CardManager.Instance.GetCardById(id)).ToList();
        IsCharacterDeckShuffled = true;
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

    #region Helper
    public void Shuffle<T>(List<T> list)
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