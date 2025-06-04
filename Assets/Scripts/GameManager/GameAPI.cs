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

    [PunRPC]
    public void RPC_SyncMainDeck(int[] cardIds)
    {
        GameManager.Instance.mainDeck = CardManager.Instance.ConvertCardIdsToCardData(cardIds);
    }

    [PunRPC]
    public void RPC_SyncLineUp(int cardId)
    {
        GameManager gm = GameManager.Instance;
        int removeIndex = gm.mainDeck.FindIndex(card => card.GetCardID() == cardId);
        if (removeIndex >= 0) gm.mainDeck.RemoveAt(removeIndex);
        gm.lineUpCards.Add(CardManager.Instance.FindCardDataById(cardId));
    }

    [PunRPC]
    public void RPC_SyncSuperVillainDeck(int[] cardIds)
    {
        GameManager.Instance.superVillainDeck = CardManager.Instance.ConvertCardIdsToCardData(cardIds);
    }

    [PunRPC]
    public void RPC_SyncSuperVillain(int cardId)
    {
        GameManager gm = GameManager.Instance;
        int removeIndex = gm.superVillainDeck.FindIndex(card => card.GetCardID() == cardId);
        if (removeIndex >= 0) gm.superVillainDeck.RemoveAt(removeIndex);
        gm.superVillainCards.Add(CardManager.Instance.FindCardDataById(cardId));
    }

    [PunRPC]
    public void RPC_ReceiveCharacterIndex(int charIndex)
    {
        PlayerManager local = PlayerManager.Local;
        local.character = GameManager.Instance.characterDeck[charIndex];
        local.Setup(PhotonNetwork.LocalPlayer);
    }

    [PunRPC]
    public void RPC_SyncCharacters(int viewID, int cardId)
    {
        if (!PlayerManager.TryGetRemotePlayer(viewID, out var player)) return;
        player.character = CardManager.Instance.FindCardDataById(cardId);
    }

    [PunRPC]
    public void RPC_SyncPlayerDeck(int viewID, int[] cardIds)
    {
        if (!PlayerManager.TryGetRemotePlayer(viewID, out var player)) return;
        List<CardData> newDeck = cardIds.Select(cardId => CardManager.Instance.FindCardDataById(cardId)).ToList();
        player.deck = newDeck;
    }

    [PunRPC]
    public void RPC_PlayerDraw(int viewID, int cardId)
    {
        if (!PlayerManager.TryGetRemotePlayer(viewID, out var player)) return;
        int index = player.deck.FindIndex(card => card.GetCardID() == cardId);
        if (index >= 0) player.deck.RemoveAt(index);

        CardData card = CardManager.Instance.FindCardDataById(cardId);
        player.hand.Add(card);
    }

    [PunRPC]
    public void RPC_AddDiscardPileToDeck(int viewID, int[] cardIds)
    {
        if (!PlayerManager.TryGetRemotePlayer(viewID, out var player)) return;
        player.deck = CardManager.Instance.ConvertCardIdsToCardData(cardIds);
        player.discardPile.Clear();
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