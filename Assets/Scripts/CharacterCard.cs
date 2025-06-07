using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.EventSystems;

[RequireComponent(typeof(PhotonView))]
public class CharacterCard : Card, IPunInstantiateMagicCallback
{
    /// <summary>
    /// Called by Photon when the object is instantiated.
    /// Assigns the BoardCard to the shared lineUpCardsArea.
    /// </summary>
    /// <param name="info">Instantiation data.</param>
    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);
        AssignToArea();
    }

    /// <summary>
    /// Parents the card under the given transform (lineup or super‐villain area).
    /// </summary>
    /// <param name="parentTransform">The RectTransform of the target area.</param>
    private void AssignToArea()
    {
        PhotonView ownerPhotonView = PhotonView.Find(playerViewID);
        if (ownerPhotonView != null && ownerPhotonView.TryGetComponent(out PlayerManager ownerPlayer))
        {
            cardTransform = ownerPlayer.characterTransform;
            transform.SetParent(cardTransform, false);
        }
        else
        {
            Debug.LogError($"PlayerManager with PhotonView ID {playerViewID} not found.");
            Destroy(gameObject); // Optionally, handle default positioning or destroy the card to prevent orphaned objects
        }
    }

    public override void OnPointerClick(PointerEventData eventData)
    {

    }
}
