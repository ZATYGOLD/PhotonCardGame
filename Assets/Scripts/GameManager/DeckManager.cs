using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// Centralized manager for player deck operations: shuffling, drawing, and refilling discard piles.
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class DeckManager : MonoBehaviourPun
{
    public static DeckManager Instance { get; private set; }
    private GameManager gm;
    private CardManager cm;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        gm = GameManager.Instance;
        cm = CardManager.Instance;
    }

    /// <summary>Draws cards for the local player and syncs with others.</summary>
    /// <param name="viewID">PhotonView ID of the PlayerManager.</param>
    /// <param name="count">Number of cards to draw.</param>
    public void DrawCards(int viewID, int count = 1)
    {
        var pm = PlayerManager.Get(viewID);
        if (pm == null || !pm.IsLocal) return;

        for (int i = 0; i < count; i++)
        {
            if (pm.deck.Count == 0)
            {
                RefillDeckFromDiscard(viewID);
                if (pm.deck.Count == 0) break;
            }

            var card = pm.deck[0];
            pm.deck.RemoveAt(0);
            pm.hand.Add(card);

            // Instantiate visual card locally
            cm.SpawnHandCard(card.GetCardID(), viewID);

            // Notify others to sync
            photonView.RPC(nameof(RPC_SyncDraw), RpcTarget.OthersBuffered, viewID, card.GetCardID());
        }

        //pm.RefreshCounters(); //TODO
    }

    public void DiscardHand(int viewID)
    {
        var pm = PlayerManager.Get(viewID);
        if (pm == null || !pm.IsLocal) return;

        pm.discardPile.AddRange(pm.hand);
        pm.hand.Clear();

        photonView.RPC(nameof(RPC_SyncDiscardHand), RpcTarget.OthersBuffered,
            viewID, pm.discardPile.Select(c => c.GetCardID()).ToArray());
        PlayerManager.Local.DestroyZoneVisual(CardZone.Hand);

        //pm.RefreshCounters(); //TODO
    }

    public void DiscardPlayedCards(int viewID)
    {
        var pm = PlayerManager.Get(viewID);
        if (pm == null || !pm.IsLocal) return;

        pm.discardPile.AddRange(gm.playedCards);
        gm.playedCards.Clear();

        photonView.RPC(nameof(RPC_SyncPlayedCards), RpcTarget.OthersBuffered,
            viewID, pm.discardPile.Select(c => c.GetCardID()).ToArray());
        PlayerManager.Local.DestroyZoneVisual(CardZone.Played);
    }

    /// <summary>Shuffles the specified player's deck and syncs with others.</summary>
    public void ShuffleDeck(int viewID)
    {
        var pm = PlayerManager.Get(viewID);
        if (pm == null || !pm.IsLocal) return;

        Shuffle(pm.deck);

        photonView.RPC(nameof(RPC_SyncShuffle), RpcTarget.OthersBuffered,
            viewID, pm.deck.Select(c => c.GetCardID()).ToArray());

        //pm.RefreshCounters(); //TODO
    }

    /// <summary>Moves all discard pile cards back into deck for the specified player.</summary>
    public void RefillDeckFromDiscard(int viewID)
    {
        var pm = PlayerManager.Get(viewID);
        if (pm == null || !pm.IsLocal) return;

        if (pm.discardPile.Count == 0) return;

        pm.deck.AddRange(pm.discardPile);
        pm.discardPile.Clear();

        Shuffle(pm.deck);

        photonView.RPC(nameof(RPC_SyncRefill), RpcTarget.OthersBuffered,
            viewID, pm.deck.Select(c => c.GetCardID()).ToArray());

        //pm.RefreshCounters(); //TODO
    }

    [PunRPC]
    private void RPC_SyncDraw(int viewID, int cardID)
    {
        var pm = PlayerManager.Get(viewID);
        if (pm == null || pm.IsLocal) return;

        var card = cm.GetCardById(cardID);
        int index = pm.deck.FindIndex(c => c.GetCardID() == cardID);
        if (index >= 0) pm.deck.RemoveAt(index);

        pm.hand.Add(card);
        //pm.RefreshCounters(); //TODO
    }

    [PunRPC]
    private void RPC_SyncDiscardHand(int viewID, int[] ids)
    {
        var pm = PlayerManager.Get(viewID);
        if (pm == null || pm.IsLocal) return;

        pm.discardPile = ids.Select(id => cm.GetCardById(id)).ToList();
        pm.hand.Clear();

        //TODO: Destroy Card object
    }

    [PunRPC]
    private void RPC_SyncPlayedCards(int viewID, int[] ids)
    {
        var pm = PlayerManager.Get(viewID);
        if (pm == null || pm.IsLocal) return;

        pm.discardPile = ids.Select(id => cm.GetCardById(id)).ToList();
        gm.playedCards.Clear();

        PlayerManager.Local.DestroyZoneVisual(CardZone.Played);
    }

    [PunRPC]
    private void RPC_SyncShuffle(int viewID, int[] ids)
    {
        var pm = PlayerManager.Get(viewID);
        if (pm == null || pm.IsLocal) return;

        pm.deck = ids.Select(id => cm.GetCardById(id)).ToList();
        //pm.RefreshCounters(); //TODO
    }

    [PunRPC]
    private void RPC_SyncRefill(int viewID, int[] deckIds)
    {
        var pm = PlayerManager.Get(viewID);
        if (pm == null || pm.IsLocal) return;

        pm.deck = deckIds.Select(id => cm.GetCardById(id)).ToList();
        pm.discardPile.Clear();
        //pm.RefreshCounters(); //TODO
    }

    /// <summary>Fisher-Yates shuffle algorithm.</summary>
    private void Shuffle<T>(List<T> list)
    {
        var rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}
