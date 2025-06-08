using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(LayoutElement))]
[RequireComponent(typeof(PhotonView))]
public abstract class Card : MonoBehaviourPun, IPunInstantiateMagicCallback, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private CardManager cardManager;

    public CardData cardData;
    public Image image;

    [Header("Power Text")]
    [SerializeField] protected TMP_Text inner;
    [SerializeField] protected TMP_Text middle;
    [SerializeField] protected TMP_Text outer;

    [Header("Value Text")]
    [SerializeField] private TMP_Text value;

    [Header("Visual Offset Container")]
    [SerializeField] protected RectTransform visualContainer;

    protected RectTransform cardTransform;
    protected int playerViewID;
    protected bool isHovering = false;


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
            playerViewID = ownerID;
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
        if (!PlayerManager.TryGetLocalPlayer(playerViewID, out var player)) return;
        if (transform.parent == GameManager.Instance.playedCardsTransform) return;

        if (cardTransform.parent == player.handTransform)
        {
            player.hand.Remove(cardData);
        }

        gameObject.transform.SetParent(GameManager.Instance.playedCardsTransform, false);
        GameManager.Instance.playedCards.Add(cardData);

        photonView.RPC(nameof(RPC_MoveToPlayArea), RpcTarget.OthersBuffered, cardManager.ConvertCardDataToIds(player.hand),
            cardManager.ConvertCardDataToIds(GameManager.Instance.playedCards));
    }

    [PunRPC]
    public void RPC_MoveToPlayArea(int[] cardIds, int[] playedCards)
    {
        if (!PlayerManager.TryGetRemotePlayer(playerViewID, out var player)) return;

        player.hand = cardManager.ConvertCardIdsToCardData(cardIds);

        GameManager.Instance.playedCards = cardManager.ConvertCardIdsToCardData(playedCards);
        transform.SetParent(GameManager.Instance.playedCardsTransform, false);
    }

    protected void MoveToLocationArea()
    {
        if (!PlayerManager.TryGetLocalPlayer(playerViewID, out var player)) return;

        if (transform.parent == player.locationTransform) return;

        if (cardTransform == player.handTransform)
        {
            player.hand.Remove(cardData);
        }

        player.locationCards.Add(cardData);
        transform.SetParent(player.locationTransform, false);

        photonView.RPC(nameof(RPC_MoveToLocationArea), RpcTarget.OthersBuffered,
            playerViewID, cardManager.ConvertCardDataToIds(player.hand),
            cardManager.ConvertCardDataToIds(player.locationCards));
    }

    [PunRPC]
    public void RPC_MoveToLocationArea(int playerViewID, int[] handIds, int[] locationCardsIds)
    {
        if (!PlayerManager.TryGetRemotePlayer(playerViewID, out var player)) return;

        player.hand = cardManager.ConvertCardIdsToCardData(handIds);
        player.locationCards = cardManager.ConvertCardIdsToCardData(locationCardsIds);
    }

    protected void MoveToDiscardPile()
    {
        if (playerViewID < 0)
        {
            playerViewID = PlayerManager.Local.GetComponent<PhotonView>().ViewID;
            Debug.Log("Set the ownerID: " + playerViewID);
        }

        // Find the owner player's PhotonView and get their PlayerManager
        PhotonView targetView = PhotonView.Find(playerViewID);
        if (!targetView.TryGetComponent<PlayerManager>(out var player)) return;
        if (!targetView.IsMine) return;

        List<CardData> cardDataList = new();

        if (cardTransform.parent == player.handTransform)
        {
            cardDataList = player.hand;
        }
        else if (cardTransform.parent == GameManager.Instance.lineUpCardsTransform)
        {
            cardDataList = GameManager.Instance.lineUpCards;
        }
        else if (cardTransform.parent == GameManager.Instance.superVillainCardsTransform)
        {
            cardDataList = GameManager.Instance.superVillainCards;
        }
        else
        {
            return;
        }

        cardDataList.Remove(cardData);
        player.discardPile.Add(cardData);

        photonView.RPC(nameof(RPC_MoveToDiscardPile), RpcTarget.OthersBuffered,
            playerViewID, cardData.GetCardID(), cardManager.ConvertCardDataToIds(cardDataList),
            cardManager.ConvertCardDataToIds(player.discardPile));

        Destroy(gameObject);
    }

    [PunRPC]
    public void RPC_MoveToDiscardPile(int playerViewID, int cardId, int[] cardIds, int[] discardPileIds)
    {
        if (!PlayerManager.TryGetRemotePlayer(playerViewID, out var player)) return;

        int removeIndex;

        // Sync the hand and discard pile for the other players
        player.hand = cardManager.ConvertCardIdsToCardData(cardIds);
        player.discardPile = cardManager.ConvertCardIdsToCardData(discardPileIds);

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
