using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;

/// <summary>
/// Manages turn order and phases in a Photon-enabled card game.
/// </summary>
public enum TurnPhase { StartPhase, MainPhase, EndPhase }

public class TurnManager : MonoBehaviourPun
{
    public static TurnManager Instance { get; private set; }

    public event Action<int> OnTurnStarted;
    public event Action<int> OnMainPhaseStarted;
    public event Action<int> OnTurnEnded;

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

    /// <summary>
    /// Only the MasterClient should call this to shuffle and set turn order.
    /// </summary>
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
        photonView.RPC(nameof(RPC_StartTurn), RpcTarget.AllBuffered, playerOrder[currentIndex]);
    }

    public void NextTurn()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        currentIndex = (currentIndex + 1) % playerOrder.Count;
        photonView.RPC(nameof(RPC_StartTurn), RpcTarget.AllBuffered, playerOrder[currentIndex]);
    }

    [PunRPC]
    public void RPC_StartTurn(int actorNumber)
    {
        currentActor = actorNumber;
        OnTurnStarted?.Invoke(actorNumber);
        currentPhase = TurnPhase.StartPhase;
        ProcessCurrentPhase();
    }

    private void ProcessCurrentPhase()
    {
        switch (currentPhase)
        {
            case TurnPhase.StartPhase:
                currentPhase = TurnPhase.MainPhase;
                ProcessCurrentPhase();
                break;

            case TurnPhase.MainPhase:
                OnMainPhaseStarted?.Invoke(currentActor);
                break;

            case TurnPhase.EndPhase:
                // TODO: Insert draw logic here or raise an OnDrawPhase event
                RequestEndTurn();
                break;
        }
    }

    /// <summary>
    /// Call this from UI when the local player ends their main phase.
    /// </summary>
    public void EndMainPhase()
    {
        if (currentPhase != TurnPhase.MainPhase) return;
        currentPhase = TurnPhase.EndPhase;
        ProcessCurrentPhase();
    }

    /// <summary>
    /// Sends end-turn request to MasterClient or processes it if this client is Master.
    /// </summary>
    public void RequestEndTurn()
    {
        if (PhotonNetwork.IsMasterClient)
            ProcessEndTurn(currentActor);
        else
            photonView.RPC(nameof(RPC_RequestEndTurn), RpcTarget.MasterClient, currentActor);
    }

    [PunRPC]
    private void RPC_RequestEndTurn(int actorNumber)
    {
        ProcessEndTurn(actorNumber);
    }

    private void ProcessEndTurn(int actorNumber)
    {
        if (actorNumber != playerOrder[currentIndex]) return;
        photonView.RPC(nameof(RPC_OnTurnEnded), RpcTarget.AllBuffered, actorNumber);
        NextTurn();
    }

    [PunRPC]
    private void RPC_OnTurnEnded(int actorNumber)
    {
        OnTurnEnded?.Invoke(actorNumber);
    }

    // Fisher-Yates shuffle
    private void Shuffle<T>(List<T> list)
    {
        var rand = new Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rand.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}
