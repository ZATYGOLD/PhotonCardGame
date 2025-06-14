using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Linq;
using System;
using Photon.Realtime;


[RequireComponent(typeof(PhotonView))]
public class NetworkManager : MonoBehaviourPun
{
    public static NetworkManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    [PunRPC]
    public void RPC_SyncLineUp(int cardId)
    {
        GameManager gm = GameManager.Instance;
        int removeIndex = gm.mainDeck.FindIndex(card => card.GetCardID() == cardId);
        if (removeIndex >= 0) gm.mainDeck.RemoveAt(removeIndex);
        gm.lineUpCards.Add(CardManager.Instance.GetCardById(cardId));
    }

    [PunRPC]
    public void RPC_SyncSuperVillain(int cardId)
    {
        GameManager gm = GameManager.Instance;
        int removeIndex = gm.superVillainDeck.FindIndex(card => card.GetCardID() == cardId);
        if (removeIndex >= 0) gm.superVillainDeck.RemoveAt(removeIndex);
        gm.superVillainCards.Add(CardManager.Instance.GetCardById(cardId));
    }

    #region Helpers
    public void Shuffle<T>(List<T> list)
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