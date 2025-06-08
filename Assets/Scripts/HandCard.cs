using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;

public class HandCard : Card
{
    PhotonView ownerPhotonView;
    private PlayerManager player;
    private float halfH;

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
            halfH = cardTransform.rect.height * 0.5f;
            Vector3 a = visualContainer.localPosition;
            visualContainer.localPosition = new Vector3(a.x, a.y - halfH, a.z);
        }
        else
        {
            Debug.LogError($"PlayerManager with PhotonView ID {playerViewID} not found.");
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

        MoveToPlayArea(); //TODO: Set default position of card back to (0,0,0) when moving the card and syncing with other players
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (player == null || cardTransform.parent != player.handTransform) return;

        if (isHovering) return;
        isHovering = true;

        Vector3 a = visualContainer.localPosition;
        visualContainer.localPosition = new Vector3(a.x, a.y + halfH, a.z);

    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (player == null || !isHovering) return;
        isHovering = false;

        Vector3 a = visualContainer.localPosition;
        visualContainer.localPosition = new Vector3(a.x, a.y - halfH, a.z);
    }
}
