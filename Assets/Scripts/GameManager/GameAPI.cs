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
    public void RPC_ReceiveCharacterIndex(int charIndex)
    {
        PlayerManager local = PlayerManager.Local;
        local.character = GameManager.Instance.characterDeck[charIndex];
        local.Setup(PhotonNetwork.LocalPlayer);
    }

    public void DrawCard(PlayerManager player, int count = 1)
    {
        var sourceList = player.deck;
        var destinationList = player.hand;

        for (int i = 0; i < count; i++)
        {
            if (sourceList.Count <= 0)
            {
                AddDiscardPileToDeck(player);
                if (sourceList.Count == 0) return;
            }

            CardData card = sourceList[0];
            sourceList.RemoveAt(0);
            destinationList.Add(card);

            PhotonNetwork.Instantiate(CardManager.Instance.handCardPrefab.name, Vector3.zero, Quaternion.identity, 0,
                new object[] { card.GetCardID(), player.GetViewID() });

            photonView.RPC(nameof(RPC_OnPlayerDraw), RpcTarget.OthersBuffered, player.GetViewID(), card.GetCardID());
        }
    }

    [PunRPC]
    public void RPC_OnPlayerDraw(int viewID, int cardId)
    {
        if (!PlayerManager.TryGetRemotePlayer(viewID, out var player)) return;
        int index = player.deck.FindIndex(card => card.GetCardID() == cardId);
        if (index >= 0) player.deck.RemoveAt(index);

        CardData card = CardManager.Instance.FindCardDataById(cardId);
        player.hand.Add(card);
    }

    public void AddDiscardPileToDeck(PlayerManager player)
    {
        if (player.discardPile.Count == 0) return;

        player.deck.AddRange(player.discardPile);
        player.discardPile.Clear();
        Shuffle(player.deck);

        photonView.RPC(nameof(RPC_AddDiscardPileToDeck), RpcTarget.OthersBuffered,
            player.GetViewID(), CardManager.Instance.ConvertCardDataToIds(player.deck));
    }

    [PunRPC]
    public void RPC_AddDiscardPileToDeck(int playerViewID, int[] cardIds)
    {
        if (!PlayerManager.TryGetRemotePlayer(playerViewID, out var playerManager)) return;

        playerManager.deck = CardManager.Instance.ConvertCardIdsToCardData(cardIds);
        playerManager.discardPile.Clear();
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