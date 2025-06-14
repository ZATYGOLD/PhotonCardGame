using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HandCard : Card
{
    PhotonView ownerPhotonView;
    private PlayerManager player;

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
        if (!PlayerManager.Local.GetIsMyTurn() || !isHovering) return;
        PlayCard();
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (player == null || cardTransform.parent != player.handTransform) return;
        if (currentlyHovered != null && currentlyHovered != this)
        {
            currentlyHovered.EndHover();
        }

        if (isHovering) return;
        isHovering = true;
        currentlyHovered = this;

        CreatePlaceholder();
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (player == null || !isHovering) return;
        if (cardTransform.parent != player.hoverTransform) return;
        isHovering = false;

        RestoreCardPosition();
    }

    protected override void EndHover()
    {
        base.EndHover();
        if (ownerPhotonView.TryGetComponent(out PlayerManager player))
        {
            // Only cancel if placeholder still exists under hand
            if (placeholder != null && placeholder.transform.parent == player.handTransform)
            {
                // Reparent back to hand and destroy placeholder
                Vector3 worldPos = cardTransform.position;
                transform.SetParent(player.handTransform, false);
                cardTransform.position = worldPos;
                cardTransform.SetSiblingIndex(placeholderIndex);
                Destroy(placeholder);
            }
        }
    }

    protected override void CreatePlaceholder()
    {
        base.CreatePlaceholder();
        placeholder.transform.SetParent(player.handTransform, false);
        placeholder.transform.SetSiblingIndex(placeholderIndex);

        transform.SetParent(player.hoverTransform, false);
        cardTransform.position = worldPos;

        float halfH = cardTransform.rect.height * 0.52f;
        cardTransform.localPosition += new Vector3(0f, halfH, 0f);
    }

    private void RestoreCardPosition()
    {
        transform.SetParent(player.handTransform, false);
        cardTransform.position = worldPos;
        float halfH = cardTransform.rect.height * 0.52f;
        cardTransform.localPosition -= new Vector3(0f, halfH, 0f);
        cardTransform.SetSiblingIndex(placeholderIndex);
        Destroy(placeholder);
    }

    private void PlayCard()
    {
        if (placeholder != null)
        {
            Destroy(placeholder);
            placeholder = null;
        }

        isHovering = false;
        if (currentlyHovered == this) currentlyHovered = null;

        if (cardData.GetCardType() == CardType.Location)
        {
            MoveToLocationArea();
        }
        else
        {
            MoveToPlayArea();
        }
    }
}
