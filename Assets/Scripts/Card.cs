using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class Card : MonoBehaviourPun, IPunInstantiateMagicCallback, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    //public CardData CardData { get; private set; }
    public CardData cardData;
    public Image image;

    [Header("Power Text")]
    [SerializeField] protected TMP_Text inner;
    [SerializeField] protected TMP_Text middle;
    [SerializeField] protected TMP_Text outer;

    [Header("Value Text")]
    [SerializeField] private TMP_Text value;

    protected RectTransform cardTransform;
    protected int ownerViewID;
    private CardManager cardManager;


    protected virtual void Awake()
    {
        cardManager = CardManager.Instance;
        cardTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        if (!ValidateElements()) return;
    }

    protected virtual bool ValidateElements()
    {
        if (image == null || inner == null || middle == null || outer == null || value == null)
        {
            Debug.LogError("UI elements are not fully assigned on Card: " + name, this);
            return false;
        }
        return true;
    }

    public virtual void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        if (TryParseInstantiationData(info, out int cardID, out int ownerID))
        {
            ownerViewID = ownerID;
            CardData data = cardManager.FindCardDataById(cardID);
            if (data != null)
            {
                SetCardData(data);
            }
            else
            {
                Debug.LogError($"Invalid CardData ID {cardID} on instantiate for {name}");
            }
        }
        else
        {
            Debug.LogError("Failed to parse instantiation data for card: " + name, this);
        }
    }

    private bool TryParseInstantiationData(PhotonMessageInfo info, out int cardID, out int ownerID)
    {
        cardID = -1;
        ownerID = -1;

        if (info.photonView.InstantiationData is object[] data && data.Length >= 2)
        {
            if (data[0] is int id && data[1] is int viewId)
            {
                cardID = id;
                ownerID = viewId;
                return true;
            }
        }
        return false;
    }

    private void SetCardData(CardData data)
    {
        image.sprite = data.GetCardImage();
        value.text = data.GetCardValue().ToString();

        if (data.GetCardType() != CardType.Character)
        {
            SetPowerText(data.GetCardCost());
        }
        cardData = data;
    }

    protected void SetPowerText(int power)
    {
        inner.text = power.ToString();
        middle.text = power.ToString();
        outer.text = power.ToString();
    }


    protected void MoveToPlayArea()
    {
        if (!PlayerManager.TryGetLocalPlayer(ownerViewID, out var playerManager)) return;
        if (transform.parent == GameManager.Instance.playedCardsTransform) return;

        //List<CardData> cardDataList = new();

        if (cardTransform == playerManager.handTransform)
        {
            playerManager.hand.Remove(cardData);
        }

        // if (cardTransform == GameManager.Instance.lineUpCardsTransform)
        // {
        //     GameManager.Instance.lineUpCards.Remove(cardData);
        //     cardDataList = GameManager.Instance.lineUpCards;
        // }

        gameObject.transform.SetParent(GameManager.Instance.playedCardsTransform, false);
        GameManager.Instance.playedCards.Add(cardData);

        photonView.RPC(nameof(RPC_MoveToPlayArea), RpcTarget.OthersBuffered, cardManager.ConvertCardDataToIds(playerManager.hand),
            cardManager.ConvertCardDataToIds(GameManager.Instance.playedCards));
    }

    [PunRPC]
    public void RPC_MoveToPlayArea(int[] cardIds, int[] playedCards)
    {
        if (!PlayerManager.TryGetRemotePlayer(ownerViewID, out var playerManager)) return;

        playerManager.hand = cardManager.ConvertCardIdsToCardData(cardIds);

        //GameManager.Instance.lineUpCards = cardManager.ConvertCardIdsToCardData(cardIds);

        GameManager.Instance.playedCards = cardManager.ConvertCardIdsToCardData(playedCards);
        transform.SetParent(GameManager.Instance.playedCardsTransform, false);
    }

    protected void MoveToLocationArea()
    {
        if (!PlayerManager.TryGetLocalPlayer(ownerViewID, out var playerManager)) return;

        if (transform.parent == playerManager.locationTransform) return;

        if (cardTransform == playerManager.handTransform)
        {
            playerManager.hand.Remove(cardData);
        }

        playerManager.locationCards.Add(cardData);
        transform.SetParent(playerManager.locationTransform, false);

        photonView.RPC(nameof(RPC_MoveToLocationArea), RpcTarget.OthersBuffered,
            ownerViewID, cardManager.ConvertCardDataToIds(playerManager.hand),
            cardManager.ConvertCardDataToIds(playerManager.locationCards));
    }

    [PunRPC]
    public void RPC_MoveToLocationArea(int playerViewID, int[] handIds, int[] locationCardsIds)
    {
        if (!PlayerManager.TryGetRemotePlayer(playerViewID, out var playerManager)) return;

        playerManager.hand = cardManager.ConvertCardIdsToCardData(handIds);
        playerManager.locationCards = cardManager.ConvertCardIdsToCardData(locationCardsIds);
    }

    protected void MoveToDiscardPile()
    {
        if (ownerViewID < 0)
        {
            ownerViewID = PlayerManager.Local.GetComponent<PhotonView>().ViewID;
            Debug.Log("Set the ownerID: " + ownerViewID);
        }

        // Find the owner player's PhotonView and get their PlayerManager
        PhotonView targetView = PhotonView.Find(ownerViewID);
        if (!targetView.TryGetComponent<PlayerManager>(out var playerManager)) return;
        if (!targetView.IsMine) return;

        List<CardData> cardDataList = new();

        if (cardTransform == playerManager.handTransform)
        {
            cardDataList = playerManager.hand;
        }
        else if (cardTransform == GameManager.Instance.lineUpCardsTransform)
        {
            cardDataList = GameManager.Instance.lineUpCards;
        }
        else if (cardTransform == GameManager.Instance.superVillainCardsTransform)
        {
            cardDataList = GameManager.Instance.superVillainCards;
        }
        else
        {
            return;
        }

        cardDataList.Remove(cardData);
        playerManager.discardPile.Add(cardData);

        photonView.RPC(nameof(RPC_MoveToDiscardPile), RpcTarget.OthersBuffered,
            ownerViewID, cardData.GetCardID(), cardManager.ConvertCardDataToIds(cardDataList),
            cardManager.ConvertCardDataToIds(playerManager.discardPile));

        Destroy(gameObject);
    }

    [PunRPC]
    public void RPC_MoveToDiscardPile(int playerViewID, int cardId, int[] cardIds, int[] discardPileIds)
    {
        if (!PlayerManager.TryGetRemotePlayer(playerViewID, out var playerManager)) return;

        int removeIndex;

        // Sync the hand and discard pile for the other players
        playerManager.hand = cardManager.ConvertCardIdsToCardData(cardIds);
        playerManager.discardPile = cardManager.ConvertCardIdsToCardData(discardPileIds);

        removeIndex = GameManager.Instance.lineUpCards.FindIndex(card => card.GetCardID() == cardId);
        if (removeIndex >= 0) GameManager.Instance.lineUpCards.RemoveAt(removeIndex);

        removeIndex = GameManager.Instance.superVillainCards.FindIndex(card => card.GetCardID() == cardId);
        if (removeIndex >= 0) GameManager.Instance.superVillainCards.RemoveAt(removeIndex);

        Destroy(gameObject);
    }

    public virtual void OnPointerClick(PointerEventData eventData) { }
    public virtual void OnDrag(PointerEventData eventData) { }
    public virtual void OnBeginDrag() { }
    public virtual void OnEndDrag(PointerEventData eventData) { }
    public virtual void OnPointerEnter(PointerEventData eventData) { }
    public virtual void OnPointerExit(PointerEventData eventData) { }
}
