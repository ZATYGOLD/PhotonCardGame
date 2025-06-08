using System.Collections;
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
    private PhotonView NetworkManagerView;

    public CardData cardData;
    public Image image;

    [Header("Power Text")]
    [SerializeField] protected TMP_Text inner;
    [SerializeField] protected TMP_Text middle;
    [SerializeField] protected TMP_Text outer;

    [Header("Value Text")]
    [SerializeField] private TMP_Text value;

    [Header("RectTransforms")]
    [SerializeField] public RectTransform visualContainer;
    [SerializeField] protected RectTransform cardTransform;

    // [Header("Canvas")]
    // [SerializeField] protected Canvas visualCanvas;

    protected int playerViewID;
    protected bool isHovering = false;
    protected int originalIndex;


    protected virtual void Awake()
    {
        cardManager = CardManager.Instance;
        NetworkManagerView = NetworkManager.Instance.photonView;
        //visualCanvas.sortingOrder = 1;
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

        transform.SetParent(GameManager.Instance.playedCardsTransform, false);
        GameManager.Instance.playedCards.Add(cardData);

        StartCoroutine(DelayedResetPosition());

        NetworkManagerView.RPC(nameof(NetworkManager.Instance.RPC_SyncMoveToPlayArea), RpcTarget.OthersBuffered,
            playerViewID, photonView.ViewID, cardData.GetCardID());
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

        NetworkManagerView.RPC(nameof(NetworkManager.Instance.RPC_SyncMoveToLocationArea), RpcTarget.OthersBuffered,
            playerViewID, photonView.ViewID, cardData.GetCardID());
    }

    protected void MoveToDiscardPile()
    {
        if (playerViewID < 0)
        {
            playerViewID = PlayerManager.Local.GetComponent<PhotonView>().ViewID;
            Debug.Log("Set the ownerID: " + playerViewID);
        }

        var targetView = PhotonView.Find(playerViewID);
        if (!targetView.TryGetComponent<PlayerManager>(out var player)) return;
        if (!targetView.IsMine) return;

        // Determine which list this card belongs to based on its parent container
        var zone = GetZoneFromTransform(cardTransform.parent);
        List<CardData> sourceList;
        switch (zone)
        {
            case CardZone.Hand:
                sourceList = player.hand;
                break;
            case CardZone.Lineup:
                sourceList = GameManager.Instance.lineUpCards;
                break;
            case CardZone.SuperVillain:
                sourceList = GameManager.Instance.superVillainCards;
                break;
            default:
                return;
        }

        sourceList.Remove(cardData);
        player.discardPile.Add(cardData);

        NetworkManagerView.RPC(nameof(NetworkManager.Instance.RPC_SyncMoveToDiscardPile), RpcTarget.OthersBuffered,
            playerViewID, photonView.ViewID, cardData.GetCardID(), (int)zone);

        Destroy(gameObject);
    }

    private CardZone GetZoneFromTransform(Transform parent)
    {
        foreach (var zone in PlayerManager._zones)
        {
            if (zone.Value.container == parent) return zone.Key;
        }
        return CardZone.Unknown;
    }

    private IEnumerator DelayedResetPosition()
    {
        yield return null;
        transform.localPosition = Vector3.zero;
        visualContainer.localPosition = Vector3.zero;
    }

    public virtual void OnPointerClick(PointerEventData eventData) { }
    public virtual void OnDrag(PointerEventData eventData) { }
    public virtual void OnBeginDrag() { }
    public virtual void OnEndDrag(PointerEventData eventData) { }
    public virtual void OnPointerEnter(PointerEventData eventData) { }
    public virtual void OnPointerExit(PointerEventData eventData) { }
}
