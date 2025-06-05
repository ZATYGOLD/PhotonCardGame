using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;

public class HandCard : Card
{
    private bool isHovering = false;

    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);
        AssignToOwnerArea();
    }

    private void AssignToOwnerArea()
    {
        PhotonView ownerPhotonView = PhotonView.Find(ownerViewID);
        if (ownerPhotonView != null && ownerPhotonView.TryGetComponent(out PlayerManager ownerPlayer))
        {
            cardTransform = ownerPlayer.handTransform;
            transform.SetParent(cardTransform, false);
        }
        else
        {
            Debug.LogError($"PlayerManager with PhotonView ID {ownerViewID} not found.");
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
        if (!isHovering)
        {
            isHovering = true;
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (isHovering)
        {
            isHovering = false;
        }
    }
}
