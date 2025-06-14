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

    public event Action<int> OnTurnEnded;
    public event Action<int> OnPowerChange;

    private bool IsMainDeckSynced = false;
    private bool IsCharacterDeckSynced = false;
    private bool IsSuperVillainsSynced = false;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (!ValidateElements()) return;

        if (PhotonNetwork.IsMasterClient)
        {
            InitializeDecks();
            SetLineUp();
            //TurnManager.Instance.SetupTurnOrder();
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
        return true;
    }

    private IEnumerator SpawnPlayers()
    {
        yield return new WaitUntil(() => IsCharacterDeckSynced && IsMainDeckSynced && IsSuperVillainsSynced);
        PlayerSpawner.Instance.SpawnLocalPlayer();

        if (PhotonNetwork.IsMasterClient)
        {
            TurnManager.Instance.SetupTurnOrder();
        }
    }

    private void InitializeDecks()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        ShuffleMainDeck();
        ShuffleSuperVillainDeck();
        ShuffleCharacters();
    }

    private void SetLineUp()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        DrawMainDeckCard(5);
        DrawSuperVillaincard();
        ManagePower(PowerOperation.Reset);
    }

    private void ShuffleCharacters()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        Shuffle(characterDeck);
        photonView.RPC(nameof(RPC_SyncCharacterDeck), RpcTarget.AllBuffered,
            characterDeck.Select(c => c.GetCardID()).ToArray());
    }

    private void ShuffleMainDeck()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        Shuffle(mainDeck);
        photonView.RPC(nameof(RPC_SyncMainDeck), RpcTarget.AllBuffered,
           mainDeck.Select(c => c.GetCardID()).ToArray());
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

            photonView.RPC(nameof(RPC_SyncMainDeckDraw), RpcTarget.OthersBuffered, card.GetCardID());
        }
    }

    private void ShuffleSuperVillainDeck()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        Shuffle(superVillainDeck);
        photonView.RPC(nameof(RPC_SyncSuperVillainsDeck), RpcTarget.AllBuffered,
           superVillainDeck.Select(c => c.GetCardID()).ToArray());
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

            //NetworkManagerView.RPC(nameof(NetworkManager.Instance.RPC_SyncSuperVillain), RpcTarget.OthersBuffered, card.GetCardID());
            photonView.RPC(nameof(RPC_SyncSuperVillainDraw), RpcTarget.OthersBuffered, card.GetCardID());
        }
    }

    [PunRPC]
    private void RPC_SyncSuperVillainDraw(int id)
    {
        var card = CardManager.Instance.GetCardById(id);
        int index = superVillainDeck.FindIndex(c => c.GetCardID() == id);
        if (index >= 0) superVillainDeck.RemoveAt(index);

        superVillainCards.Add(card);
    }

    [PunRPC]
    private void RPC_SyncCharacterDeck(int[] ids)
    {
        characterDeck = ids.Select(id => CardManager.Instance.GetCardById(id)).ToList();
        IsCharacterDeckSynced = true;
    }

    [PunRPC]
    private void RPC_SyncMainDeck(int[] ids)
    {
        mainDeck = ids.Select(id => CardManager.Instance.GetCardById(id)).ToList();
        IsMainDeckSynced = true;
    }

    [PunRPC]
    private void RPC_SyncMainDeckDraw(int id)
    {
        var card = CardManager.Instance.GetCardById(id);
        int index = mainDeck.FindIndex(c => c.GetCardID() == id);
        if (index >= 0) mainDeck.RemoveAt(index);

        lineUpCards.Add(card);
        //pm.RefreshCounters(); //TODO
    }

    [PunRPC]
    private void RPC_SyncSuperVillainsDeck(int[] ids)
    {
        superVillainDeck = ids.Select(id => CardManager.Instance.GetCardById(id)).ToList();
        IsSuperVillainsSynced = true;
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
public enum CardZone
{
    Unknown = -1,
    Hand = 0,
    Lineup = 1,
    SuperVillain = 2,
    Played = 3
}
#endregion