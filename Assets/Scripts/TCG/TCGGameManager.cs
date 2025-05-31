// // Filename: TCGGameManager.cs
// using System.Collections;
// using System.Collections.Generic;
// using Photon.Pun;
// using UnityEngine;

// /// <summary>
// /// Manages the overall game flow for the Trading Card Game (TCG).
// /// Handles game initialization, turn management, game phases, and win conditions.
// /// </summary>
// public class TCGGameManager : MonoBehaviourPunCallbacks, IEventListener
// {
//     #region Singleton Implementation

//     public static TCGGameManager Instance { get; private set; }

//     private void Awake()
//     {
//         if (Instance == null)
//         {
//             Instance = this;
//             DontDestroyOnLoad(gameObject);
//             Debug.Log("TCGGameManager: Singleton instance initialized.");
//         }
//         else
//         {
//             Destroy(gameObject);
//             Debug.LogWarning("TCGGameManager: Duplicate instance destroyed.");
//         }
//     }

//     #endregion

//     #region Game State Variables

//     [Header("Game Configuration")]
//     [Tooltip("Number of players required to start the game.")]
//     public int RequiredPlayers = 2;

//     [Tooltip("Maximum number of players allowed in the game.")]
//     public int MaxPlayers = 2;

//     [Header("Player Prefab")]
//     [Tooltip("Reference to the TCGPlayer prefab for instantiation.")]
//     public GameObject TCGPlayerPrefab;
//     [SerializeField] private RectTransform spawnPoint;

//     [Header("Turn Management")]
//     [Tooltip("List of players participating in the game.")]
//     public List<TCGPlayer> Players = new List<TCGPlayer>();

//     [Tooltip("Index of the current player's turn.")]
//     private int currentPlayerIndex = 0;

//     [Tooltip("Flag indicating if the game has started.")]
//     public bool GameStarted = false;

//     #endregion

//     #region Photon Event Handling

//     private void Start()
//     {
//         // Subscribe to necessary events
//         PhotonEventManager.Instance.Subscribe(EventType.OnPlayerJoined, this);
//         PhotonEventManager.Instance.Subscribe(EventType.OnPlayerLeft, this);
//         PhotonEventManager.Instance.Subscribe(EventType.OnGameStartRequest, this);

//         SpawnPlayer();
//     }

//     private void OnDestroy()
//     {
//         // Unsubscribe from events to prevent memory leaks
//         PhotonEventManager.Instance.Unsubscribe(EventType.OnPlayerJoined, this);
//         PhotonEventManager.Instance.Unsubscribe(EventType.OnPlayerLeft, this);
//         PhotonEventManager.Instance.Unsubscribe(EventType.OnGameStartRequest, this);
//     }

//     private void SpawnPlayer()
//     {
//         Debug.Log("TCGGameManager: Local player joined the room.");

//         // Instantiate the TCGPlayer prefab for the local player
//         if (TCGPlayerPrefab != null)
//         {
//             GameObject playerObject = PhotonNetwork.Instantiate(TCGPlayerPrefab.name, spawnPoint.localPosition, Quaternion.identity);
//             playerObject.transform.SetParent(spawnPoint, false);
//             Debug.Log($"TCGGameManager: Instantiated TCGPlayer prefab for {PhotonNetwork.NickName}.");
//         }
//         else
//         {
//             Debug.LogError("TCGGameManager: TCGPlayerPrefab is not assigned in the Inspector.");
//         }
//     }

//     /// <summary>
//     /// Handles received events from the PhotonEventManager.
//     /// </summary>
//     /// <param name="data">The event data received.</param>
//     public void OnEventReceived(BaseEventData data)
//     {
//         switch (data.EventType)
//         {
//             case EventType.OnPlayerJoined:
//                 HandlePlayerJoined(data as PlayerEventData);
//                 break;
//             case EventType.OnPlayerLeft:
//                 HandlePlayerLeft(data as PlayerEventData);
//                 break;
//             case EventType.OnGameStartRequest:
//                 HandleGameStartRequest(data as GameStartEventData);
//                 break;
//             // Add more cases as needed
//             default:
//                 Debug.LogWarning($"Unhandled EventType received in GameManager: {data.EventType}");
//                 break;
//         }
//     }

//     #endregion

//     #region Event Handlers

//     /// <summary>
//     /// Handles the OnPlayerJoined event.
//     /// </summary>
//     /// <param name="data">PlayerEventData containing player information.</param>
//     private void HandlePlayerJoined(PlayerEventData data)
//     {
//         if (data.PlayerID != PhotonNetwork.NickName)
//         {
//             Debug.Log($"{data.PlayerID} has joined the game.");
//             // Optionally, add the player to the Players list if they are already in the scene
//             TCGPlayer newPlayer = FindPlayerByID(data.PlayerID);
//             if (newPlayer != null && !Players.Contains(newPlayer))
//             {
//                 Players.Add(newPlayer);
//             }
//         }

//         // Check if the game can start
//         if (PhotonNetwork.IsMasterClient && Players.Count >= RequiredPlayers)
//         {
//             StartGame();
//             Debug.Log("TCGGameManager: Game started automatically as Master Client.");
//         }
//     }

//     /// <summary>
//     /// Handles the OnPlayerLeft event.
//     /// </summary>
//     /// <param name="data">PlayerEventData containing player information.</param>
//     private void HandlePlayerLeft(PlayerEventData data)
//     {
//         Debug.Log($"{data.PlayerID} has left the game.");
//         TCGPlayer departingPlayer = FindPlayerByID(data.PlayerID);
//         if (departingPlayer != null)
//         {
//             Players.Remove(departingPlayer);
//         }

//         // Handle game end if a player leaves
//         if (GameStarted && Players.Count < RequiredPlayers)
//         {
//             EndGame("A player has left the game. Game ended.");
//         }
//     }

//     /// <summary>
//     /// Handles the OnGameStartRequest event.
//     /// </summary>
//     /// <param name="data">GameStartEventData containing request information.</param>
//     private void HandleGameStartRequest(GameStartEventData data)
//     {
//         if (PhotonNetwork.IsMasterClient)
//         {
//             StartGame();
//         }
//     }

//     #endregion

//     #region Game Lifecycle Methods

//     /// <summary>
//     /// Initializes and starts the game.
//     /// </summary>
//     public void StartGame()
//     {
//         if (GameStarted)
//         {
//             Debug.LogWarning("Attempted to start the game, but it has already started.");
//             return;
//         }

//         if (Players.Count < RequiredPlayers)
//         {
//             Debug.LogWarning($"Not enough players to start the game. Required: {RequiredPlayers}, Current: {Players.Count}");
//             return;
//         }

//         GameStarted = true;
//         Debug.Log("Game is starting...");

//         // // Shuffle each player's deck
//         // foreach (TCGPlayer player in Players)
//         // {
//         //     player.ShuffleDeck();
//         //     player.DrawStartingHand(5);
//         // }

//         // Determine turn order (e.g., randomly)
//         currentPlayerIndex = Random.Range(0, Players.Count);
//         Debug.Log($"{Players[currentPlayerIndex].PlayerID} will take the first turn.");

//         // Trigger the OnGameStart event
//         GameEventData gameStartData = new GameEventData(EventType.OnGameStart, "Game has started!", null);
//         PhotonEventManager.Instance.TriggerEventAcrossNetwork(EventType.OnGameStart, gameStartData);

//         // Start the first turn
//         StartTurn(Players[currentPlayerIndex]);
//     }

//     /// <summary>
//     /// Ends the game and declares the winner.
//     /// </summary>
//     /// <param name="winnerID">The PlayerID of the winner.</param>
//     public void EndGame(string winnerID)
//     {
//         if (!GameStarted)
//         {
//             Debug.LogWarning("Attempted to end the game, but it hasn't started yet.");
//             return;
//         }

//         GameStarted = false;
//         Debug.Log($"Game has ended! Winner: {winnerID}");

//         // Trigger the OnGameEnd event
//         GameEventData gameEndData = new GameEventData(EventType.OnGameEnd, $"Winner: {winnerID}", winnerID);
//         PhotonEventManager.Instance.TriggerEventAcrossNetwork(EventType.OnGameEnd, gameEndData);

//         // Optionally, perform cleanup or reset game state
//     }

//     #endregion

//     #region Turn Management

//     /// <summary>
//     /// Starts a player's turn.
//     /// </summary>
//     /// <param name="player">The TCGPlayer whose turn is starting.</param>
//     private void StartTurn(TCGPlayer player)
//     {
//         Debug.Log($"It's now {player.PlayerID}'s turn.");

//         // Trigger the OnTurnStart event
//         TurnEventData turnStartData = new TurnEventData(EventType.OnTurnStart, player.PlayerID);
//         PhotonEventManager.Instance.TriggerEventAcrossNetwork(EventType.OnTurnStart, turnStartData);

//         // Optionally, enable player's UI for actions
//         // This can be handled within the TCGPlayer script based on the event
//     }

//     /// <summary>
//     /// Ends the current player's turn and starts the next player's turn.
//     /// </summary>
//     public void EndTurn()
//     {
//         if (!GameStarted)
//         {
//             Debug.LogWarning("Attempted to end a turn, but the game hasn't started.");
//             return;
//         }

//         // Trigger the OnTurnEnd event for the current player
//         TCGPlayer currentPlayer = Players[currentPlayerIndex];
//         TurnEventData turnEndData = new TurnEventData(EventType.OnTurnEnd, currentPlayer.PlayerID);
//         PhotonEventManager.Instance.TriggerEventAcrossNetwork(EventType.OnTurnEnd, turnEndData);

//         // Determine the next player's index
//         currentPlayerIndex = (currentPlayerIndex + 1) % Players.Count;
//         TCGPlayer nextPlayer = Players[currentPlayerIndex];

//         // Start the next turn
//         StartTurn(nextPlayer);
//     }

//     #endregion

//     #region Player Management

//     /// <summary>
//     /// Adds a player to the game.
//     /// </summary>
//     /// <param name="player">The TCGPlayer to add.</param>
//     public void AddPlayer(TCGPlayer player)
//     {
//         if (!Players.Contains(player))
//         {
//             Players.Add(player);
//             Debug.Log($"{player.PlayerID} has been added to the game.");

//             // Trigger the OnPlayerJoined event
//             PlayerEventData playerJoinedData = new PlayerEventData(EventType.OnPlayerJoined, player.PlayerID);
//             PhotonEventManager.Instance.TriggerEventAcrossNetwork(EventType.OnPlayerJoined, playerJoinedData);
//         }
//     }

//     /// <summary>
//     /// Removes a player from the game.
//     /// </summary>
//     /// <param name="player">The TCGPlayer to remove.</param>
//     public void RemovePlayer(TCGPlayer player)
//     {
//         if (Players.Contains(player))
//         {
//             Players.Remove(player);
//             Debug.Log($"{player.PlayerID} has been removed from the game.");

//             // Trigger the OnPlayerLeft event
//             PlayerEventData playerLeftData = new PlayerEventData(EventType.OnPlayerLeft, player.PlayerID);
//             PhotonEventManager.Instance.TriggerEventAcrossNetwork(EventType.OnPlayerLeft, playerLeftData);
//         }
//     }

//     /// <summary>
//     /// Finds a player by their PlayerID.
//     /// </summary>
//     /// <param name="playerID">The PlayerID to search for.</param>
//     /// <returns>The TCGPlayer with the matching PlayerID, or null if not found.</returns>
//     private TCGPlayer FindPlayerByID(string playerID)
//     {
//         foreach (TCGPlayer player in Players)
//         {
//             if (player.PlayerID == playerID)
//                 return player;
//         }
//         return null;
//     }

//     #endregion

//     #region Helper Methods

//     /// <summary>
//     /// Requests the Master Client to start the game.
//     /// Can be triggered via UI (e.g., Start Game button).
//     /// </summary>
//     public void RequestStartGame()
//     {
//         if (!PhotonNetwork.IsMasterClient)
//         {
//             Debug.LogWarning("Only the Master Client can request to start the game.");
//             return;
//         }

//         if (Players.Count < RequiredPlayers)
//         {
//             Debug.LogWarning($"Not enough players to start the game. Required: {RequiredPlayers}, Current: {Players.Count}");
//             return;
//         }

//         // Trigger the OnGameStartRequest event
//         GameStartEventData gameStartRequest = new GameStartEventData(EventType.OnGameStartRequest, "Master Client requests to start the game.");
//         PhotonEventManager.Instance.TriggerEventAcrossNetwork(EventType.OnGameStartRequest, gameStartRequest);
//     }

//     #endregion
// }
