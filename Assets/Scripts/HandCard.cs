using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;

public class HandCard : Card
{
    PhotonView ownerPhotonView;
    private PlayerManager player;
    private float halfH;
    [Header("Canvas")]
    [SerializeField] protected Canvas visualCanvas;

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
            halfH = cardTransform.rect.height * 0.52f;
            Vector3 a = visualContainer.anchoredPosition;
            visualContainer.anchoredPosition = new Vector3(a.x, a.y - halfH, a.z);
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

        Vector3 a = visualContainer.localPosition;
        visualContainer.localPosition = new Vector3(a.x, a.y + halfH, a.z);
        MoveToPlayArea();
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (player == null || isHovering) return;
        isHovering = true;
        if (cardTransform.parent == player.handTransform || cardTransform.parent == player.locationTransform)
        {
            //visualCanvas.sortingOrder = 2;
            Vector3 a = visualContainer.localPosition;
            visualContainer.localPosition = new Vector3(a.x, a.y + halfH, a.z);
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (player == null || !isHovering) return;
        isHovering = false;

        //visualCanvas.sortingOrder = 1;
        Vector3 a = visualContainer.localPosition;
        visualContainer.localPosition = new Vector3(a.x, a.y - halfH, a.z);
    }
}
