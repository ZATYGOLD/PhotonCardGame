using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class GameLogic : MonoBehaviourPun
{
    public static GameLogic Instance { get; private set; }

    [Header("Card Prefabs")]
    private GameObject handCardPrefab;

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
    }

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

    #endregion
}