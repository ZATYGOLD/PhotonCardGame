using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;

public class HandCard : Card
{
    PhotonView ownerPhotonView;
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
        if (isHovering) return;
        isHovering = true;
        if (ownerPhotonView.TryGetComponent(out PlayerManager player))
        {
            transform.SetParent(player.hoverTransform, false);
        }

    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (!isHovering) return;
        isHovering = false;
        if (ownerPhotonView.TryGetComponent(out PlayerManager player))
        {
            transform.SetParent(player.handTransform, false);
        }
    }
}
