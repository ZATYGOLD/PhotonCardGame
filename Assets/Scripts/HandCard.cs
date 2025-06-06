using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;

public class HandCard : Card
{
    PhotonView ownerPhotonView;
    private static HandCard currentlyHovered;
    private bool isHovering = false;

    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);
        ownerPhotonView = PhotonView.Find(playerViewID);
        if (ownerPhotonView == null) return;
        AssignToOwnerArea();
    }

    private void AssignToOwnerArea()
    {
        if (ownerPhotonView.TryGetComponent(out PlayerManager player))
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

        if (cardData.GetCardType() == CardType.Location)
        {
            MoveToLocationArea();
            return;
        }

        MoveToPlayArea();
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (currentlyHovered != null && currentlyHovered != this)
        {
            currentlyHovered.CancelHover();
        }

        if (isHovering) return;
        isHovering = true;
        currentlyHovered = this;

        if (ownerPhotonView.TryGetComponent(out PlayerManager player))
        {
            var pos = cardTransform.position;
            transform.SetParent(player.hoverTransform, true);
            cardTransform.position = pos;
            float halfH = cardTransform.rect.height * 0.52f;
            cardTransform.localPosition += new Vector3(0f, halfH, 0f);
        }

    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (!isHovering) return;
        isHovering = false;

        if (ownerPhotonView.TryGetComponent(out PlayerManager player))
        {
            var pos = cardTransform.position;
            transform.SetParent(player.handTransform, true);
            cardTransform.position = pos;
            float halfH = cardTransform.rect.height * 0.52f;
            cardTransform.localPosition -= new Vector3(0f, halfH, 0f);
        }
    }

    private void CancelHover()
    {
        if (!isHovering) return;
        isHovering = false;
        if (ownerPhotonView.TryGetComponent(out PlayerManager player))
        {
            transform.SetParent(player.handTransform, worldPositionStays: false);
        }
    }
}
