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
        gm.lineUpCards.Add(CardManager.Instance.GetCardById(cardId));
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
        gm.superVillainCards.Add(CardManager.Instance.GetCardById(cardId));
    }

    [PunRPC]
    public void RPC_ReceiveCharacterIndex(int charId)
    {
        PlayerManager local = PlayerManager.Local;
        local.character = CardManager.Instance.GetCardById(charId);
        local.Setup(PhotonNetwork.LocalPlayer);
    }

    [PunRPC]
    public void RPC_SyncCharacters(int viewID, int cardId)
    {
        if (!PlayerManager.TryGetRemotePlayer(viewID, out var player)) return;
        player.character = CardManager.Instance.GetCardById(cardId);
    }

    [PunRPC]
    public void RPC_SyncDiscardPileToDeck(int viewID, int[] cardIds)
    {
        if (!PlayerManager.TryGetRemotePlayer(viewID, out var player)) return;
        player.deck = CardManager.Instance.ConvertCardIdsToCardData(cardIds);
        player.discardPile.Clear();
    }

    [PunRPC]
    public void RPC_SyncPlayerHand(int viewID, int[] ids)
    {
        if (!PlayerManager.TryGetRemotePlayer(viewID, out var player)) return;
        player.discardPile.AddRange(CardManager.Instance.ConvertCardIdsToCardData(ids));
        player.hand.Clear();
    }

    [PunRPC]
    public void RPC_SyncPlayedCardsToDiscardPile(int viewID, int[] cardIds)
    {
        if (!PlayerManager.TryGetRemotePlayer(viewID, out var player)) return;

        foreach (Transform cardObject in GameManager.Instance.playedCardsTransform)
        { Destroy(cardObject.gameObject); }

        List<CardData> playedCards = CardManager.Instance.ConvertCardIdsToCardData(cardIds);
        player.discardPile.AddRange(playedCards);
        GameManager.Instance.playedCards.Clear();
    }

    #region Turn Management
    [PunRPC]
    public void RPC_SetTurnOrder(int[] actorNumbers)
    {
        GameManager.Instance.playerActorNumbers = actorNumbers.ToList();
        Debug.Log("Turn order received.");
    }

    [PunRPC]
    public void RPC_StartTurn(int actorNumber)
    {
        GameManager.Instance.currentPlayerIndex = GameManager.Instance.playerActorNumbers.IndexOf(actorNumber);
        Debug.Log($"Starting turn for player with ActorNumber: {actorNumber}");
        bool isCurrentPlayer = PhotonNetwork.LocalPlayer.ActorNumber == actorNumber;
        PlayerManager.Local.SetTurnActive(isCurrentPlayer);
    }
    #endregion

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