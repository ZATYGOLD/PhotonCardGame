using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;

public class HandCard : Card, IPunInstantiateMagicCallback
{
    //private int ownerViewID;

    /// <summary>
    /// Called by Photon when the object is instantiated.
    /// </summary>
    /// <param name="info">Instantiation data.</param>
    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);
        AssignToOwnerArea();
    }

    /// <summary>
    /// Assigns the card to the owner's hand area based on ownerViewID.
    /// </summary>
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
}
