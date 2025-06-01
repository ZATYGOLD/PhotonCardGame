using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Linq;
using System;
using Photon.Realtime;


[RequireComponent(typeof(PhotonView))]
public class GameAPI : MonoBehaviourPun
{
    public static GameAPI Instance { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void DrawCard(int viewID, int count = 1)
    {
        if (!PlayerManager.TryGetLocalPlayer(viewID, out var pm)) return;

        var sourceList = pm.deck;
        var destinationList = pm.hand;

        for (int i = 0; i < count; i++)
        {
            if (sourceList.Count <= 0)
            {
                Shuffle(sourceList);
                if (sourceList.Count == 0) return;
            }

            CardData card = sourceList[0];
            sourceList.RemoveAt(0);
            destinationList.Add(card);

            PhotonNetwork.Instantiate(CardManager.Instance.handCardPrefab.name, Vector3.zero, Quaternion.identity, 0,
                new object[] { card.GetCardID(), viewID });

            photonView.RPC(nameof(RPC_OnPlayerDraw), RpcTarget.OthersBuffered, viewID, card.GetCardID());
        }
    }

    [PunRPC]
    public void RPC_OnPlayerDraw(int viewID, int cardId)
    {
        if (!PlayerManager.TryGetRemotePlayer(viewID, out var pm)) return;
        int index = pm.deck.FindIndex(card => card.GetCardID() == cardId);
        if (index >= 0) pm.deck.RemoveAt(index);

        CardData card = CardManager.Instance.FindCardDataById(cardId);
        pm.hand.Add(card);
    }

    #region Helpers
    private void Shuffle<T>(List<T> list)
    {
        var rand = new System.Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rand.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void InstantiateCard(int ownerId, CardData data, int zoneIndex)
    {
        PhotonNetwork.Instantiate(CardManager.Instance.handCardPrefab.name, Vector3.zero, Quaternion.identity, 0,
            new object[] { data.GetCardID(), ownerId, zoneIndex });
    }
    #endregion
}

public enum CardZone
{
    Unknown = -1,
    Hand = 0,
    Lineup = 1,
    SuperVillain = 2,
    Played = 3
}