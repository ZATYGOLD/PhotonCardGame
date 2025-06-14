using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PhotonView))]
public class PlayerManager : MonoBehaviourPun
{
    public static PlayerManager Local { get; private set; }
    public bool IsLocal => photonView.IsMine;

    [Header("Player Data")]
    public int ActorNumber;
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
    public RectTransform hoverTransform;

    [Header("Card Prefabs")]
    [SerializeField] private GameObject characterCardPrefab;

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
        ActorNumber = photonView.OwnerActorNr;
        if (photonView.IsMine)
        {
            if (Local != null) { Destroy(gameObject); return; }
            Local = this;
            SetZoneEnums();
        }
    }

    void OnEnable()
    {
        if (IsLocal && TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPhaseChanged += HandlePhaseChanged;
        }
    }

    void Start()
    {
        if (!ValidateElements() || !IsLocal) return;
        Setup();
    }

    void OnDisable()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
        }
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

    private void Setup()
    {
        playerName.text = PhotonNetwork.LocalPlayer.NickName;
        deck = GameManager.Instance.playerDeck;
        DeckManager.Instance.ShuffleDeck(photonView.ViewID);
        DeckManager.Instance.DrawCards(photonView.ViewID, 5);

        endTurnButton.onClick.AddListener(EndTurn);
        endTurnButton.gameObject.SetActive(false);

        StartCoroutine(GetCharacter());
    }


    #region Character
    private IEnumerator GetCharacter()
    {
        yield return new WaitUntil(() => IsLocal && GameManager.Instance.IsCharacterDeckSynced);
        var deck = GameManager.Instance.characterDeck;
        int index = ActorNumber - 1;
        if (index > deck.Count) index = 0;
        character = deck[index];
        deck.RemoveAt(index);
        InstantiateCharacter();
    }

    [PunRPC]
    private void RPC_SyncCharacters(int viewID, int cardID)
    {
        var player = PLAYERS[viewID];
        if (IsLocal || player == null) return;
        player.character = CardManager.Instance.GetCardById(cardID);
        int index = GameManager.Instance.characterDeck.FindIndex(c => c.GetCardID() == cardID);
        if (index >= 0) GameManager.Instance.characterDeck.RemoveAt(index);
    }

    private void InstantiateCharacter()
    {
        if (!IsLocal) return;
        int cardId = character.GetCardID();

        PhotonNetwork.Instantiate(characterCardPrefab.name, Vector3.zero, Quaternion.identity, 0,
            new object[] { cardId, photonView.ViewID });

        photonView.RPC(nameof(RPC_SyncCharacters), RpcTarget.OthersBuffered, photonView.ViewID, cardId);
    }
    #endregion

    #region Player Turns
    public void StartTurn()
    {
        if (!IsLocal) return;
        isMyTurn = true;
        currentTimer = turnDuration;
        endTurnButton.gameObject.SetActive(true);
    }

    public void EndTurn()
    {
        if (!IsLocal || !isMyTurn) return;
        TurnManager.Instance.EndMainPhase();
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

    private void HandlePhaseChanged(int actorNumber, TurnPhase phase)
    {
        if (actorNumber != ActorNumber) return;
        switch (phase)
        {
            case TurnPhase.StartPhase:
                SetTurnActive(true);
                break;
            case TurnPhase.MainPhase:
                break;
            case TurnPhase.EndPhase:
                DeckManager.Instance.DiscardHand(photonView.ViewID);
                DeckManager.Instance.DiscardPlayedCards(photonView.ViewID);
                DeckManager.Instance.DrawCards(photonView.ViewID, 5);
                SetTurnActive(false);
                break;
        }
    }
    #endregion

    void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
        }
        PLAYERS.Remove(photonView.ViewID);
    }

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

    public void DestroyZoneVisual(CardZone zone)
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
