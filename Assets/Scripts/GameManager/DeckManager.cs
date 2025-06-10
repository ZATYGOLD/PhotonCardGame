using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// Centralized manager for player deck operations: shuffling, drawing, and refilling discard piles.
/// </summary>
public class DeckManager : MonoBehaviourPun
{
    public static DeckManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
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
            CardManager.Instance.SpawnHandCard(card.GetCardID(), viewID);

            // Notify others to sync
            photonView.RPC(nameof(RPC_SyncDraw), RpcTarget.OthersBuffered, viewID, card.GetCardID());
        }

        //pm.RefreshCounters(); //TODO
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

        var card = CardManager.Instance.GetCardById(cardID);
        pm.hand.Add(card);
        //pm.RefreshCounters(); //TODO
    }

    [PunRPC]
    private void RPC_SyncShuffle(int viewID, int[] deckIds)
    {
        var pm = PlayerManager.Get(viewID);
        if (pm == null || pm.IsLocal) return;

        pm.deck = deckIds
            .Select(id => CardManager.Instance.GetCardById(id))
            .ToList();
        //pm.RefreshCounters(); //TODO
    }

    [PunRPC]
    private void RPC_SyncRefill(int viewID, int[] deckIds)
    {
        var pm = PlayerManager.Get(viewID);
        if (pm == null || pm.IsLocal) return;

        pm.deck = deckIds
            .Select(id => CardManager.Instance.GetCardById(id))
            .ToList();
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
