using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HandCard : Card
{
    PhotonView ownerPhotonView;
    private PlayerManager player;
    private static HandCard currentlyHovered;
    private bool isHovering = false;
    private GameObject placeholder;
    private int placeholderIndex;

    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);
        ownerPhotonView = PhotonView.Find(playerViewID);
        if (ownerPhotonView == null) return;
        if (!ownerPhotonView.TryGetComponent(out player))
        {
            Debug.LogError($"HandCard: no PlayerManager on viewID={playerViewID}");
            Destroy(gameObject);
            return;
        }
        AssignToOwnerArea();
    }

    private void AssignToOwnerArea()
    {
        if (player != null)
        {
            transform.SetParent(player.handTransform, false);
        }
        else
        {
            Debug.LogError($"PlayerManager with PhotonView ID {playerViewID} not found.");
            // Optionally, handle default positioning or destroy the card to prevent orphaned objects
            Destroy(gameObject);
        }
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (PlayerManager.Local.GetIsMyTurn() == false) return;
        // If currently hovered (under hoverTransform), cancel hover so we reparent under handTransform
        if (isHovering)
        {
            CancelHover();
        }


        if (cardData.GetCardType() == CardType.Location)
        {
            MoveToLocationArea();
            return;
        }

        MoveToPlayArea();
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (player == null) return;
        if (cardTransform.parent != player.handTransform) return;
        if (currentlyHovered != null && currentlyHovered != this)
        {
            currentlyHovered.CancelHover();
        }

        if (isHovering) return;
        isHovering = true;
        currentlyHovered = this;

        // 1) Determine this card's index in the hand
        placeholderIndex = cardTransform.GetSiblingIndex();
        // 2) Create placeholder GameObject to hold the slot
        placeholder = new GameObject("CardPlaceholder", typeof(RectTransform));
        // Ensure placeholder’s RectTransform matches the card’s size
        RectTransform phRect = placeholder.GetComponent<RectTransform>();
        phRect.sizeDelta = cardTransform.sizeDelta;
        // Copy LayoutElement from this card so layout size matches
        var cardLE = GetComponent<LayoutElement>();
        var phLE = placeholder.AddComponent<LayoutElement>();
        phLE.minWidth = cardLE.minWidth;
        phLE.minHeight = cardLE.minHeight;
        phLE.preferredWidth = cardLE.preferredWidth;
        phLE.preferredHeight = cardLE.preferredHeight;
        phLE.flexibleWidth = cardLE.flexibleWidth;
        phLE.flexibleHeight = cardLE.flexibleHeight;
        phLE.layoutPriority = cardLE.layoutPriority;
        // Parent placeholder into hand at the same index
        placeholder.transform.SetParent(player.handTransform, false);
        placeholder.transform.SetSiblingIndex(placeholderIndex);
        phRect.localPosition = Vector3.zero;

        // 3) Reparent card into hoverTransform
        Vector3 worldPos = cardTransform.position;
        transform.SetParent(player.hoverTransform, worldPositionStays: false);
        cardTransform.position = worldPos;
        // 4) Nudge up by half height
        float halfH = cardTransform.rect.height * 0.52f;
        cardTransform.localPosition += new Vector3(0f, halfH, 0f);

    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (player == null) return;
        if (!isHovering) return;
        isHovering = false;
        // Only exit hover if currently under hoverTransform
        if (cardTransform.parent != player.hoverTransform)
            return;

        // 1) Capture world position
        Vector3 worldPos = cardTransform.position;
        // 2) Reparent card back under handTransform
        transform.SetParent(player.handTransform, worldPositionStays: false);
        cardTransform.position = worldPos;
        // 3) Lower by half height
        float halfH = cardTransform.rect.height * 0.52f;
        cardTransform.localPosition -= new Vector3(0f, halfH, 0f);
        // 4) Insert card at placeholder index
        cardTransform.SetSiblingIndex(placeholderIndex);
        // 5) Destroy the placeholder
        Destroy(placeholder);
    }

    private void CancelHover()
    {
        if (!isHovering) return;
        isHovering = false;
        currentlyHovered = null;
        if (ownerPhotonView.TryGetComponent(out PlayerManager player))
        {
            // Only cancel if placeholder still exists under hand
            if (placeholder != null && placeholder.transform.parent == player.handTransform)
            {
                // Reparent back to hand and destroy placeholder
                Vector3 worldPos = cardTransform.position;
                transform.SetParent(player.handTransform, worldPositionStays: false);
                cardTransform.position = worldPos;
                cardTransform.SetSiblingIndex(placeholderIndex);
                Destroy(placeholder);
            }
        }
    }
}
