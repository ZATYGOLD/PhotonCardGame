using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using System;
using System.Linq;

[RequireComponent(typeof(PhotonView))]
public class CardManager : MonoBehaviourPun
{
    #region Singleton
    public static CardManager Instance { get; private set; }
    #endregion

    #region Prefabs
    [Header("Card Prefabs")]
    public GameObject handCardPrefab;
    public GameObject boardCardPrefab;
    #endregion

    #region Events
    public event Action<List<CardData>> OnDeckShuffled;
    public event Action<CardData> OnLineUpCardDrawn;
    public event Action<CardData> OnSuperVillainCardDrawn;
    #endregion

    #region Fields
    // Dictionaries to store game-specific decks and their destinations
    public static readonly Dictionary<int, CardData> CARD_LIST = new();
    private static readonly System.Random _rng = new();
    #endregion

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        DontDestroyOnLoad(gameObject);
        LoadAllCardData();
    }

    #region Deck Operations

    public void ShufflePlayerDeck(PlayerManager playerManager)
    {
        if (playerManager == null) return;

        Shuffle(playerManager.deck);
        OnDeckShuffled?.Invoke(playerManager.deck);
        int[] cardIds = playerManager.deck.Select(card => card.GetCardID()).ToArray();

        photonView.RPC(nameof(RPC_ShufflePlayerDeck), RpcTarget.OthersBuffered,
            playerManager.GetViewID(), cardIds);
    }

    [PunRPC]
    public void RPC_ShufflePlayerDeck(int playerViewID, int[] cardIds)
    {
        if (!PlayerManager.TryGetRemotePlayer(playerViewID, out var playerManager)) return;
        List<CardData> newDeck = cardIds.Select(cardId => FindCardDataById(cardId)).ToList();
        playerManager.deck = newDeck;
        OnDeckShuffled?.Invoke(newDeck);
    }

    #endregion

    #region Game-Specific Deck Management

    // private void DrawOntoBoard(List<CardData> source, List<CardData> destination, int count,
    //     int zone, Action<CardData> onDrawCard, string rpcName)
    // {
    //     if (!PhotonNetwork.IsMasterClient) return;
    //     if (source == null || source.Count <= 0) return;
    //     for (int i = 0; i < count; i++)
    //     {
    //         CardData card = PopTopCard(source);
    //         destination.Add(card);
    //         InstantiateBoardCard(card, zone);
    //         onDrawCard?.Invoke(card);
    //         photonView.RPC(rpcName, RpcTarget.OthersBuffered, card.GetCardID());
    //     }
    // }

    // public void DrawFromMainDeckToLineUp(int count)
    // {
    //     DrawOntoBoard(GameManager.Instance.mainDeck, GameManager.Instance.lineUpCards,
    //         count, 0, OnLineUpCardDrawn, nameof(RPC_DrawFromMainDeck));
    // }

    // [PunRPC]
    // public void RPC_DrawFromMainDeck(int cardId)
    // {
    //     GameManager gm = GameManager.Instance;
    //     int removeIndex = gm.mainDeck.FindIndex(card => card.GetCardID() == cardId);
    //     if (removeIndex >= 0) gm.mainDeck.RemoveAt(removeIndex);
    //     gm.lineUpCards.Add(FindCardDataById(cardId));
    // }

    // public void DrawFromSuperVillainDeckToLineUp(int count)
    // {
    //     DrawOntoBoard(GameManager.Instance.superVillainDeck, GameManager.Instance.superVillainCards,
    //         count, 1, OnSuperVillainCardDrawn, nameof(RPC_DrawFromVillainDeck));
    // }

    // [PunRPC]
    // public void RPC_DrawFromVillainDeck(int cardId)
    // {
    //     GameManager gm = GameManager.Instance;
    //     int removeIndex = gm.superVillainDeck.FindIndex(card => card.GetCardID() == cardId);
    //     if (removeIndex >= 0) gm.superVillainDeck.RemoveAt(removeIndex);
    //     gm.superVillainCards.Add(FindCardDataById(cardId));
    // }

    public void InitializeDecks()
    {
        ShuffleDeck(GameManager.Instance.mainDeck, OnDeckShuffled, nameof(RPC_ShuffleGameDeck));
        ShuffleDeck(GameManager.Instance.characterDeck, OnDeckShuffled, nameof(RPC_ShuffleCharacterDeck));
        ShuffleDeck(GameManager.Instance.superVillainDeck, OnDeckShuffled, nameof(RPC_ShuffleSuperVillainDeck));
    }

    private void ShuffleDeck(List<CardData> deck, Action<List<CardData>> action, string rpcName)
    {
        if (!PhotonNetwork.IsMasterClient || deck == null || deck.Count == 0) return;
        Shuffle(deck);

        action?.Invoke(deck);

        int[] cardIds = ConvertCardDataToIds(deck);
        photonView.RPC(rpcName, RpcTarget.OthersBuffered, cardIds);
    }

    [PunRPC]
    public void RPC_ShuffleGameDeck(int[] cardIds)
    {
        GameManager.Instance.mainDeck = ConvertCardIdsToCardData(cardIds);
    }

    [PunRPC]
    public void RPC_ShuffleCharacterDeck(int[] cardIds)
    {
        GameManager.Instance.characterDeck = ConvertCardIdsToCardData(cardIds);
    }

    [PunRPC]
    public void RPC_ShuffleSuperVillainDeck(int[] cardIds)
    {
        GameManager.Instance.superVillainDeck = ConvertCardIdsToCardData(cardIds);
    }

    #endregion

    #region Helper Functions

    public CardData PopTopCard(List<CardData> deck)
    {
        CardData card = deck[0]; deck.RemoveAt(0); return card;
    }

    public void InstantiateHandCard(CardData data, int ownerViewID)
    {
        PhotonNetwork.Instantiate(handCardPrefab.name, Vector3.zero, Quaternion.identity, 0,
            new object[] { data.GetCardID(), ownerViewID });
    }

    public void InstantiateBoardCard(CardData data, int zoneIndex)
    {
        PhotonNetwork.Instantiate(boardCardPrefab.name, Vector3.zero, Quaternion.identity, 0,
            new object[] { data.GetCardID(), -1, zoneIndex });
    }

    // public void ReshufflePlayerDiscardPileToDeck(PlayerManager playerManager)
    // {
    //     if (playerManager.discardPile.Count == 0) return;

    //     playerManager.deck.AddRange(playerManager.discardPile);
    //     playerManager.discardPile.Clear();
    //     Shuffle(playerManager.deck);

    //     photonView.RPC(nameof(RPC_ReshufflePlayerDiscardPileToDeck), RpcTarget.OthersBuffered,
    //         playerManager.GetViewID(), ConvertCardDataToIds(playerManager.deck));
    // }

    // [PunRPC]
    // public void RPC_ReshufflePlayerDiscardPileToDeck(int playerViewID, int[] cardIds)
    // {
    //     if (!PlayerManager.TryGetRemotePlayer(playerViewID, out var playerManager)) return;

    //     playerManager.deck = ConvertCardIdsToCardData(cardIds);
    //     playerManager.discardPile.Clear();
    // }

    private void Shuffle(List<CardData> deck)
    {
        int n = deck.Count;
        while (n > 1)
        {
            int k = _rng.Next(n--);
            (deck[k], deck[n]) = (deck[n], deck[k]);
        }
    }

    public int[] ConvertCardDataToIds(List<CardData> cardDataList)
    {
        return cardDataList.Select(card => card.GetCardID()).ToArray();
    }


    public CardData FindCardDataById(int cardId)
    {
        if (CARD_LIST.TryGetValue(cardId, out var data)) return data;
        Debug.LogError($"CardData ID {cardId} not found in CARD_LIST.");
        return null;
    }


    public List<CardData> ConvertCardIdsToCardData(int[] cardIds)
    {
        List<CardData> cardDataList = new();

        foreach (int cardId in cardIds)
        {
            CardData card = FindCardDataById(cardId);
            if (card != null)
            {
                cardDataList.Add(card);
            }
        }
        return cardDataList;
    }

    #endregion

    #region Data Loading
    private void LoadAllCardData()
    {
        CARD_LIST.Clear();

        foreach (CardType type in Enum.GetValues(typeof(CardType)))
        {
            if (type == CardType.None)
                continue;

            string category = type.ToString();
            CardData[] cards = Resources.LoadAll<CardData>($"Cards/{category}");
            if (cards.Length == 0)
                Debug.LogWarning($"No cards found in Resources/Cards/{category} (CardType {type}).");

            RegisterCards(cards, category);
        }
    }

    private void RegisterCards(CardData[] cardArray, string category)
    {
        foreach (var card in cardArray)
        {
            int id = card.GetCardID();
            if (CARD_LIST.ContainsKey(id))
            {
                Debug.LogWarning($"Duplicate CardData ID detected in {category}: {id} for {card.name}");
                return;
            }

            CARD_LIST.Add(id, card);
            Debug.Log($"Registered {category} Card: {card.name} (ID: {id})");
        }
    }
    #endregion
}