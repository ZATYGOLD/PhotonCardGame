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
    // public GameObject boardCardPrefab;
    #endregion

    #region Events
    public event Action<List<CardData>> OnDeckShuffled;
    public event Action<CardData> OnLineUpCardDrawn;
    public event Action<CardData> OnSuperVillainCardDrawn;
    #endregion

    #region Fields
    // Dictionaries to store game-specific decks and their destinations
    public static readonly Dictionary<int, CardData> CARD_LIST = new();
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

    #region Card Specific

    public CardData PopTopCard(List<CardData> deck)
    {
        CardData card = deck[0]; deck.RemoveAt(0); return card;
    }

    public void InstantiateHandCard(CardData data, int ownerViewID)
    {
        PhotonNetwork.Instantiate(handCardPrefab.name, Vector3.zero, Quaternion.identity, 0,
            new object[] { data.GetCardID(), ownerViewID });
    }

    // public void InstantiateBoardCard(CardData data, int zoneIndex)
    // {
    //     PhotonNetwork.Instantiate(boardCardPrefab.name, Vector3.zero, Quaternion.identity, 0,
    //         new object[] { data.GetCardID(), -1, zoneIndex });
    // }

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