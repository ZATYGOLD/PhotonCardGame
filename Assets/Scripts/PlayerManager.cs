using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PhotonView))]
public class PlayerManager : MonoBehaviourPun
{
    public static PlayerManager Local { get; private set; }
    public Player Player { get; private set; }
    public int ActorNumber => photonView.OwnerActorNr;
    public bool IsLocal => photonView.IsMine;
    private PhotonView NetworkManager;

    [Header("Card Prefabs")]
    [SerializeField] private GameObject characterCardPrefab;

    [Header("Player Information")]
    public CardData character;
    public List<CardData> deck = new();
    public List<CardData> hand = new();
    public List<CardData> discardPile = new();
    public List<CardData> locationCards = new();

    [Header("UI Text")]
    [SerializeField] private TMP_Text playerName;
    [SerializeField] private TMP_Text deckCount;
    [SerializeField] private TMP_Text discardPileCount;
    [SerializeField] private TMP_Text timerCount;

    [Header("Buttons")]
    [SerializeField] private Button endTurnButton;

    [Header("UI Locations")]
    public RectTransform characterTransform;
    public RectTransform handTransform;
    public RectTransform locationTransform;

    [Header("Turn Timer")]
    private readonly float turnDuration = 50f;
    private float currentTimer;
    private bool isMyTurn;

    public event Action<CardData> OnCardDrawn; // TODO: Implement
    public event Action<CardData> OnCardPlayed; // TODO: Implement
    public event Action<CardData, CardZone> OnCardDiscarded; // TODO: Implement
    public event Action<CardData, PlayerManager> OnPlayerCardDiscarded; // TODO: Implement

    private static readonly Dictionary<int, PlayerManager> PLAYERS = new();
    private static readonly Dictionary<CardZone, (RectTransform container, List<CardData> list)> _zones = new();

    void Awake()
    {
        PLAYERS[photonView.ViewID] = this;
        if (photonView.IsMine)
        {
            if (Local != null) { Destroy(gameObject); return; }
            Local = this;
            SetZoneEnums();
            NetworkManager = global::NetworkManager.Instance.photonView;
        }
    }

    void Start()
    {
        if (!ValidateElements()) return;
    }

    void Update()
    {
        if (!IsLocal) return;
        if (isMyTurn)
        {
            currentTimer -= Time.deltaTime;
            UpdateTimerUI();

            if (currentTimer <= 0f) EndTurn();
        }
    }

    private bool ValidateElements()
    {
        if (playerName == null || deckCount == null || discardPileCount == null || timerCount == null ||
            endTurnButton == null || handTransform == null || locationTransform == null)
        {
            Debug.LogError("Some elements of the PlayerManager are not assigned in the Inspector!", this);
            return false;
        }
        if (photonView == null)
        {
            Debug.LogError("PhotonView is missing in the PlayerPrefab!");
            Debug.LogError("PlayerManager - Awake |  photonView ID: " + photonView.ViewID, this);
            return false;
        }
        return true;
    }

    public void Setup(Player player)
    {
        if (!IsLocal) return;
        Player = player;
        playerName.text = Player.NickName;
        deck = GameManager.Instance.playerDeck;

        InstantiateCharacter();
        ShuffleDeck(this);
        DrawCard(this, 5);

        endTurnButton.onClick.AddListener(EndTurn);
        endTurnButton.gameObject.SetActive(false);
        GameManager.Instance.OnTurnEnded += HandleTurnEnded;
    }

    #region Character
    public void InstantiateCharacter()
    {
        if (!IsLocal) return;
        int cardId = character.GetCardID();

        PhotonNetwork.Instantiate(characterCardPrefab.name, Vector3.zero, Quaternion.identity, 0,
            new object[] { cardId, photonView.ViewID });

        NetworkManager.RPC(nameof(global::NetworkManager.Instance.RPC_SyncCharacters), RpcTarget.OthersBuffered,
            photonView.ViewID, cardId);
    }
    #endregion

    public void DrawCard(PlayerManager player, int count = 1)
    {
        var sourceList = player.deck;
        var destinationList = player.hand;

        for (int i = 0; i < count; i++)
        {
            if (sourceList.Count <= 0)
            {
                AddDiscardPileToDeck(player);
                if (sourceList.Count == 0) return;
            }

            CardData card = sourceList[0];
            sourceList.RemoveAt(0);
            destinationList.Add(card);

            PhotonNetwork.Instantiate(CardManager.Instance.handCardPrefab.name, Vector3.zero, Quaternion.identity, 0,
                new object[] { card.GetCardID(), player.GetViewID() });

            NetworkManager.RPC(nameof(global::NetworkManager.Instance.RPC_SyncPlayerDraw), RpcTarget.OthersBuffered, player.GetViewID(), card.GetCardID());
        }
    }

    public void AddDiscardPileToDeck(PlayerManager player)
    {
        if (player.discardPile.Count == 0) return;

        player.deck.AddRange(player.discardPile);
        player.discardPile.Clear();
        global::NetworkManager.Instance.Shuffle(player.deck);

        NetworkManager.RPC(nameof(global::NetworkManager.Instance.RPC_SyncDiscardPileToDeck), RpcTarget.OthersBuffered,
            player.GetViewID(), CardManager.Instance.ConvertCardDataToIds(player.deck));
    }

    public void ShuffleDeck(PlayerManager player)
    {
        GameManager.Instance.Shuffle(player.deck);
        NetworkManager.RPC(nameof(global::NetworkManager.Instance.RPC_SyncPlayerDeck), RpcTarget.OthersBuffered,
            player.GetViewID(), CardManager.Instance.ConvertCardDataToIds(player.deck));
    }

    #region Card Interactions
    public void DiscardAllHand()
    {
        if (!IsLocal) return;
        int[] cardIds = hand.Select(card => card.GetCardID()).ToArray();

        NetworkManager.RPC(nameof(global::NetworkManager.Instance.RPC_SyncPlayerHand), RpcTarget.OthersBuffered,
            photonView.ViewID, cardIds);

        hand.Clear();
        DestroyZoneVisual(CardZone.Hand);
        discardPile.AddRange(CardManager.Instance.ConvertCardIdsToCardData(cardIds));
        RefreshCounters();
    }

    public void SendPlayedCardsToDiscardPile()
    {
        if (!photonView.IsMine) return;
        List<CardData> playedCards = GameManager.Instance.playedCards;
        if (playedCards == null || playedCards.Count <= 0) return;

        foreach (Transform cardObject in GameManager.Instance.playedCardsTransform)
        { Destroy(cardObject.gameObject); }
        discardPile.AddRange(playedCards);

        NetworkManager.RPC(nameof(global::NetworkManager.Instance.RPC_SyncPlayedCardsToDiscardPile), RpcTarget.OthersBuffered,
            GetViewID(), CardManager.Instance.ConvertCardDataToIds(playedCards));

        playedCards.Clear();
    }


    #endregion

    #region Player Lookup
    public static bool TryGetRemotePlayer(int viewID, out PlayerManager playerManager)
    {
        playerManager = null;
        Ownership who = GetOwnership(viewID);
        if (who == Ownership.Remote)
        {
            playerManager = PLAYERS[viewID];
            return true;
        }
        return false;
    }

    public static bool TryGetLocalPlayer(int viewID, out PlayerManager playerManager)
    {
        playerManager = null;
        Ownership who = GetOwnership(viewID);
        if (who == Ownership.Local)
        {
            playerManager = PLAYERS[viewID];
            return true;
        }
        return false;
    }

    public static Ownership GetOwnership(int viewID)
    {
        if (viewID < 0) return Ownership.Shared;
        if (PLAYERS.TryGetValue(viewID, out var playerManager))
        {
            return playerManager.photonView.IsMine ? Ownership.Local : Ownership.Remote;
        }

        return Ownership.Unknown;
    }
    #endregion

    #region Player Turns
    // This method will be called to start the player's turn
    public void StartTurn()
    {
        if (!IsLocal) return;
        isMyTurn = true;
        currentTimer = turnDuration;
        endTurnButton.gameObject.SetActive(true);
    }

    // This is your End Turn method
    public void EndTurn()
    {
        if (!IsLocal || !isMyTurn) return;
        isMyTurn = false;
        endTurnButton.gameObject.SetActive(false);
        GameManager.Instance.RequestEndTurn(Player.ActorNumber);
    }

    public void SetTurnActive(bool isActive)
    {
        isMyTurn = isActive;
        endTurnButton.gameObject.SetActive(isActive);

        if (isActive)
        {
            currentTimer = turnDuration;
            // Additional logic to start the turn can go here
        }
        else
        {
            // Logic to handle when the turn ends can go here
            // For example, resetting UI elements or variables
        }
    }

    // Event handler for when the turn ends
    private void HandleTurnEnded(int actor)
    {
        if (actor != ActorNumber) return;

        DiscardAllHand();
        SendPlayedCardsToDiscardPile();
        DrawCard(this, 5);
    }
    #endregion

    void OnDestroy()
    {
        // Unsubscribe from the event to prevent memory leaks
        if (GameManager.Instance != null)
            GameManager.Instance.OnTurnEnded -= HandleTurnEnded;
        // Remove this instance from lookup
        PLAYERS.Remove(photonView.ViewID);
    }

    public int GetViewID() => photonView.ViewID;
    public bool GetIsMyTurn() => isMyTurn;

    #region Helpers
    private void SetZoneEnums()
    {
        _zones[CardZone.Hand] = (handTransform, hand);
        _zones[CardZone.Lineup] = (GameManager.Instance.lineUpCardsTransform, GameManager.Instance.lineUpCards);
        _zones[CardZone.SuperVillain] = (GameManager.Instance.superVillainCardsTransform, GameManager.Instance.superVillainCards);
        _zones[CardZone.Played] = (GameManager.Instance.playedCardsTransform, GameManager.Instance.playedCards);
    }

    private void RemoveFromZoneLocally(CardData card, CardZone zone)
    {
        if (_zones.TryGetValue(zone, out var info))
            info.list.Remove(card);
    }

    private CardZone GetZoneFromTransform(Transform parent)
    {
        foreach (var zone in _zones)
        {
            if (zone.Value.container == parent) return zone.Key;
        }
        return CardZone.Unknown;
    }

    private void DestroyZoneVisual(CardZone zone)
    {
        var (container, _) = _zones[zone];
        foreach (Transform child in container) Destroy(child.gameObject);
    }
    #endregion

    #region UI
    private void RefreshCounters()
    {
        deckCount.text = deck.Count.ToString();
        discardPileCount.text = discardPile.Count.ToString();
    }

    private void UpdateTimerUI()
    {
        timerCount.text = TimeSpan.FromSeconds(currentTimer).ToString(@"mm\:ss");
    }
    #endregion

    public static PlayerManager Get(int viewID) => PLAYERS.TryGetValue(viewID, out var pm) ? pm : null;
}
