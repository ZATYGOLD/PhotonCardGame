using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;

public enum TurnPhase { StartPhase, MainPhase, EndPhase }

public class TurnManager : MonoBehaviourPun
{
    public static TurnManager Instance { get; private set; }

    public event Action<int, TurnPhase> OnPhaseChanged;

    private List<int> playerOrder = new();
    private int currentIndex;
    private int currentActor;
    private TurnPhase currentPhase;

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

    public void SetupTurnOrder()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        var order = PhotonNetwork.PlayerList.Select(p => p.ActorNumber).ToList();
        Shuffle(order);
        photonView.RPC(nameof(RPC_SetTurnOrder), RpcTarget.AllBuffered, order.ToArray());
    }

    [PunRPC]
    private void RPC_SetTurnOrder(int[] order)
    {
        playerOrder = order.ToList();
        currentIndex = 0;
        StartTurnFor(playerOrder[currentIndex]);
    }

    public void NextTurn()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        currentIndex = (currentIndex + 1) % playerOrder.Count;
        StartTurnFor(playerOrder[currentIndex]);
    }

    private void StartTurnFor(int actorNumber)
    {
        photonView.RPC(nameof(RPC_StartTurn), RpcTarget.AllBuffered, actorNumber);
    }

    [PunRPC]
    public void RPC_StartTurn(int actorNumber)
    {
        currentActor = actorNumber;

        currentPhase = TurnPhase.StartPhase;
        OnPhaseChanged?.Invoke(currentActor, currentPhase);

        currentPhase = TurnPhase.MainPhase;
        OnPhaseChanged?.Invoke(currentActor, currentPhase);
    }

    public void EndMainPhase()
    {
        if (currentPhase != TurnPhase.MainPhase) return;

        // End Phase
        currentPhase = TurnPhase.EndPhase;
        OnPhaseChanged?.Invoke(currentActor, currentPhase);

        RequestNextTurn();
    }

    private void RequestNextTurn()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            AdvanceTurn(currentActor);
        }
        else
        {
            photonView.RPC(nameof(RPC_RequestNextTurn),
                RpcTarget.MasterClient, currentActor);
        }
    }

    [PunRPC]
    private void RPC_RequestNextTurn(int actorNumber)
    {
        AdvanceTurn(actorNumber);
    }

    private void AdvanceTurn(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (actorNumber != playerOrder[currentIndex]) return;
        NextTurn();
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
}
