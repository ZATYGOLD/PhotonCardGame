using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class GameLogic : MonoBehaviourPun
{
    public static GameLogic Instance { get; private set; }

    [Header("Card Prefabs")]
    private GameObject handCardPrefab;
    private GameObject boardCardPrefab;

    public delegate void PowerChange();
    public event PowerChange OnPowerChange;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        handCardPrefab = CardManager.Instance.handCardPrefab;
        boardCardPrefab = CardManager.Instance.boardCardPrefab;
    }

    #region Public Modular API

    public void DrawCard(PlayerManager playerManager, int count = 1)
    {
        List<CardData> source = playerManager.deck;
        List<CardData> destination = playerManager.hand;

        DrawCard(source, destination, count);
    }


    public void DrawCard(List<CardData> source, List<CardData> destination, int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            if (source.Count <= 0)
            {
                ShuffleDeck(source);
                if (source.Count == 0) return;
            }

            CardData card = source[0];
            source.RemoveAt(0);
            destination.Add(card);

            PhotonNetwork.Instantiate(boardCardPrefab.name, Vector3.zero, Quaternion.identity, 0,
                new object[] { card.GetCardID(), -1, 0 }
            );

            photonView.RPC(nameof(RPC_SyncDrawnCard), RpcTarget.OthersBuffered, card.GetCardID());
        }
        //GameUI.Get().UpdateDeckCountText(source);
    }

    [PunRPC]
    public void RPC_SyncDrawnCard(int cardId)
    {
        GameManager gm = GameManager.Instance;
        int removeIndex = gm.mainDeck.FindIndex(card => card.GetCardID() == cardId);
        if (removeIndex >= 0) gm.mainDeck.RemoveAt(removeIndex);
        gm.lineUpCards.Add(CardManager.Instance.FindCardDataById(cardId));
    }


    public void ManagePower(PowerOperation operation, int amount = 0, CardData cardData = null)
    {
        switch (operation)
        {
            case PowerOperation.Add:
                GameManager.Instance.power += amount;
                break;
            case PowerOperation.Subtract:
                if (cardData != null) GameManager.Instance.power -= cardData.GetCardCost();
                else if (cardData == null) GameManager.Instance.power -= amount;
                break;
            case PowerOperation.Reset:
                GameManager.Instance.power = 0;
                break;
        }

        //GameUI.Get().UpdatePowerText(power);
    }

    #endregion

    #region Helper

    private void InstantiateCard(CardData data, CardZone zone, int ownerViewID)
    {

        PhotonNetwork.Instantiate(handCardPrefab.name, Vector3.zero, Quaternion.identity, 0,
            new object[] { data.GetCardID(), ownerViewID, (int)zone });

    }

    private void InstantiateHandCard(CardData data, int ownerViewID)
    {
        PhotonNetwork.Instantiate(handCardPrefab.name, Vector3.zero, Quaternion.identity, 0,
            new object[] { data.GetCardID(), ownerViewID });
    }

    private void InstantiateBoardCard(CardData data, int zoneIndex)
    {
        PhotonNetwork.Instantiate(boardCardPrefab.name, Vector3.zero, Quaternion.identity, 0,
            new object[] { data.GetCardID(), -1, zoneIndex });
    }

    private void ShuffleDeck(List<CardData> source)
    {
        if (source == GameManager.Instance.playerDeck && PlayerManager.Local.discardPile.Count != 0)
        {
            source.AddRange(PlayerManager.Local.discardPile);
            PlayerManager.Local.discardPile.Clear();
        }

        Shuffle(source);
    }

    private void Shuffle<T>(List<T> list)
    {
        var rand = new System.Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rand.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    #endregion
}

#region Enums
public enum Ownership
{
    Unknown,
    Local,
    Remote,
    Shared
}
// public enum CardZone
// {
//     Unknown = -1,
//     Hand = 0,
//     Lineup = 1,
//     SuperVillain = 2,
//     Played = 3
// }
#endregion