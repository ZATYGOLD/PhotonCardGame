using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.EventSystems;

[RequireComponent(typeof(PhotonView))]
public class BoardCard : Card, IPunInstantiateMagicCallback
{
    private const int SpawnFlagIndex = 2;
    private bool isSuperVillain;
    /// <summary>
    /// Called by Photon when the object is instantiated.
    /// Assigns the BoardCard to the shared lineUpCardsArea.
    /// </summary>
    /// <param name="info">Instantiation data.</param>
    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);

        object[] data = info.photonView.InstantiationData;
        if (data == null || data.Length <= SpawnFlagIndex)
        {
            Debug.LogError("BoardCard: Missing Spawn location for LineUp or SuperVillains");
            Destroy(gameObject);
            return;
        }

        isSuperVillain = (int)data[SpawnFlagIndex] == 1;

        if (isSuperVillain)
        {
            AssignToArea(GameManager.Instance.superVillainCardsTransform);
        }
        else
        {
            AssignToArea(GameManager.Instance.lineUpCardsTransform);
        }
    }

    /// <summary>
    /// Parents the card under the given transform (lineup or super‐villain area).
    /// </summary>
    /// <param name="parentTransform">The RectTransform of the target area.</param>
    private void AssignToArea(RectTransform parentTransform)
    {
        if (parentTransform == null)
        {
            Debug.LogError("Target area transform is not assigned in GameManager.");
            Destroy(gameObject);
            return;
        }

        cardTransform = parentTransform;
        transform.SetParent(parentTransform, worldPositionStays: false);
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (!PlayerManager.Local.GetIsMyTurn()) return;

        MoveToDiscardPile();

        if (isSuperVillain)
        {
            GameManager.Instance.DrawSuperVillaincard();
        }
        else
        {
            GameManager.Instance.DrawMainDeckCard();
        }
    }
}
