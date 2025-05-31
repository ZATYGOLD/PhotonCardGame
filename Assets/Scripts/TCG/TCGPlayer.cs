// // Filename: TCGPlayer.cs
// using System.Collections.Generic;
// using Photon.Pun;
// using Photon.Realtime;
// using TMPro;
// using UnityEngine;
// using UnityEngine.UI;

// /// <summary>
// /// Represents a player in the Trading Card Game (TCG).
// /// Manages player-specific actions, UI updates, deck management, and interacts with Photon for multiplayer synchronization.
// /// </summary>
// public class TCGPlayer : MonoBehaviourPun, IEventListener, IPunObservable
// {
//     #region Player Information

//     [Header("Player Information")]
//     [Tooltip("Unique identifier for the player, typically set to PhotonNetwork.NickName.")]
//     public string PlayerID;

//     #endregion

//     #region UI Components

//     [Header("UI Components")]
//     [Tooltip("TMP_Text component for displaying the player's name.")]
//     public TMP_Text PlayerNameText;

//     [Tooltip("TMP_Text component for displaying the deck count.")]
//     public TMP_Text DeckCountText;

//     [Tooltip("TMP_Text component for displaying the discard pile count.")]
//     public TMP_Text DiscardCountText;

//     [Tooltip("TMP_Text component for displaying the timer.")]
//     public TMP_Text TimerText;

//     [Tooltip("Button component for ending the turn.")]
//     public Button EndTurnButton;

//     [Tooltip("Parent transform for the player's hand.")]
//     public Transform HandPanel;

//     #endregion

//     #region Card Management

//     [Header("Card Management")]
//     [Tooltip("List of cards currently in the player's hand.")]
//     public List<CardData> Hand = new List<CardData>();

//     [Tooltip("List of cards in the player's deck.")]
//     public List<CardData> Deck = new List<CardData>();

//     [Tooltip("List of cards in the player's discard pile.")]
//     public List<CardData> DiscardPile = new List<CardData>();

//     [Tooltip("List of CardData assets to initialize the deck.")]
//     public List<CardData> InitialDeck = new List<CardData>();

//     [Header("Card Prefab")]
//     [Tooltip("Reference to the TCGCard prefab for instantiation.")]
//     public GameObject TCGCardPrefab;

//     #endregion

//     #region Player Resources

//     [Header("Player Resources")]
//     [Tooltip("Current power level of the player.")]
//     public int Power = 0;

//     #endregion

//     #region Battlefield Reference

//     // [Header("Battlefield")]
//     // [Tooltip("Reference to the shared battlefield where played cards are placed.")]
//     // public RectTransform Battlefield;

//     #endregion

//     #region Timer Variables

//     [Header("Turn Timer")]
//     [Tooltip("Duration of each turn in seconds.")]
//     public float TurnDuration = 60f; // Example: 60 seconds per turn

//     private float turnTimer = 60f;

//     #endregion

//     #region Photon Networking

//     // Reference to the parent player (local player)
//     private TCGPlayer parentPlayer;

//     #endregion

//     #region Unity Lifecycle Methods

//     /// <summary>
//     /// Initializes the player's deck and subscribes to necessary events.
//     /// </summary>
//     private void Start()
//     {
//         if (photonView.IsMine)
//         {
//             // Initialize local player information
//             PlayerID = PhotonNetwork.NickName;
//             PlayerNameText.text = PlayerID;
//             Debug.Log($"Initialized local player with ID: {PlayerID}");

//             // Initialize and shuffle the deck
//             InitializeDeck();
//             ShuffleDeck();

//             // Draw the starting hand
//             DrawStartingHand(5);

//             // Assign End Turn button listener
//             EndTurnButton.onClick.AddListener(EndTurn);
//         }
//         else
//         {
//             // For remote players, set PlayerID based on Photon owner
//             PlayerID = photonView.Owner.NickName;
//             PlayerNameText.text = PlayerID;
//             // Optionally, hide or disable certain UI elements if not needed
//             // For example:
//             // DeckCountText.gameObject.SetActive(false);
//             // DiscardCountText.gameObject.SetActive(false);
//             // TimerText.gameObject.SetActive(false);
//             // EndTurnButton.gameObject.SetActive(false);
//             // HandPanel.gameObject.SetActive(false);
//         }

//         // Subscribe to relevant game events
//         PhotonEventManager.Instance.Subscribe(EventType.OnTurnStart, this);
//         PhotonEventManager.Instance.Subscribe(EventType.OnTurnEnd, this);
//         PhotonEventManager.Instance.Subscribe(EventType.OnGameStart, this);
//         PhotonEventManager.Instance.Subscribe(EventType.OnGameEnd, this);
//         PhotonEventManager.Instance.Subscribe(EventType.OnCardPlayed, this);
//         PhotonEventManager.Instance.Subscribe(EventType.OnCardDiscarded, this);
//         PhotonEventManager.Instance.Subscribe(EventType.OnCardObtained, this);
//         PhotonEventManager.Instance.Subscribe(EventType.OnCardSelected, this);

//         // Register with GameManager
//         if (TCGGameManager.Instance != null)
//         {
//             TCGGameManager.Instance.AddPlayer(this);
//         }
//         else
//         {
//             Debug.LogError("TCGPlayer: TCGGameManager.Instance is null. Ensure that TCGGameManager is present in the scene.");
//         }
//     }

//     /// <summary>
//     /// Handles the turn timer countdown and automatic end turn.
//     /// </summary>
//     private void Update()
//     {
//         if (photonView.IsMine)
//         {
//             if (turnTimer > 0)
//             {
//                 turnTimer -= Time.deltaTime;
//                 UpdateTimerUI();
//             }
//             else
//             {
//                 // Time's up, end turn automatically
//                 EndTurn();
//             }
//         }
//     }

//     /// <summary>
//     /// Unsubscribes from game events and performs cleanup when the player object is destroyed.
//     /// </summary>
//     private void OnDestroy()
//     {
//         // Unsubscribe from game events when the player object is destroyed
//         PhotonEventManager.Instance.Unsubscribe(EventType.OnTurnStart, this);
//         PhotonEventManager.Instance.Unsubscribe(EventType.OnTurnEnd, this);
//         PhotonEventManager.Instance.Unsubscribe(EventType.OnGameStart, this);
//         PhotonEventManager.Instance.Unsubscribe(EventType.OnGameEnd, this);
//         PhotonEventManager.Instance.Unsubscribe(EventType.OnCardPlayed, this);
//         PhotonEventManager.Instance.Unsubscribe(EventType.OnCardDiscarded, this);
//         PhotonEventManager.Instance.Unsubscribe(EventType.OnCardObtained, this);
//         PhotonEventManager.Instance.Unsubscribe(EventType.OnCardSelected, this);
//     }

//     /// <summary>
//     /// Handles received events from the PhotonEventManager.
//     /// </summary>
//     /// <param name="data">The event data received.</param>
//     public void OnEventReceived(BaseEventData data)
//     {
//         switch (data.EventType)
//         {
//             case EventType.OnTurnStart:
//                 HandleTurnStart(data as TurnEventData);
//                 break;
//             case EventType.OnTurnEnd:
//                 HandleTurnEnd(data as TurnEventData);
//                 break;
//             case EventType.OnGameStart:
//                 HandleGameStart(data as GameEventData);
//                 break;
//             case EventType.OnGameEnd:
//                 HandleGameEnd(data as GameEventData);
//                 break;
//             case EventType.OnCardPlayed:
//                 HandleCardPlayed(data as CardEventData);
//                 break;
//             case EventType.OnCardDiscarded:
//                 HandleCardDiscarded(data as CardEventData);
//                 break;
//             case EventType.OnCardObtained:
//                 HandleCardObtained(data as CardEventData);
//                 break;
//             case EventType.OnCardSelected:
//                 HandleCardSelected(data as CardEventData);
//                 break;
//             // Add more cases as needed
//             default:
//                 Debug.LogWarning($"Unhandled EventType received: {data.EventType}");
//                 break;
//         }
//     }

//     #endregion

//     #region Event Handlers

//     private void HandleTurnStart(TurnEventData data)
//     {
//         if (data.CurrentPlayerID == this.PlayerID)
//         {
//             Debug.Log($"{PlayerID}'s turn has started.");
//             // Example action: Draw a card at turn start
//             DrawCard();
//             // Additional turn start logic can be added here
//             // e.g., increase Power, refresh abilities
//             ResetTimer();
//         }
//     }

//     private void HandleTurnEnd(TurnEventData data)
//     {
//         if (data.CurrentPlayerID == this.PlayerID)
//         {
//             Debug.Log($"{PlayerID}'s turn has ended.");
//             // Example action: Reset temporary resources or effects
//             // Additional turn end logic can be added here
//             // e.g., remove temporary buffs
//         }
//     }

//     private void HandleGameStart(GameEventData data)
//     {
//         Debug.Log("Game has started!");
//         // Perform any additional setup required at game start
//         // e.g., initialize game timers, UI elements
//     }

//     private void HandleGameEnd(GameEventData data)
//     {
//         Debug.Log($"Game has ended! Winner: {data.WinnerPlayerID}");
//         // Perform any cleanup or UI updates required at game end
//         // e.g., display winner, reset game state
//     }

//     private void HandleCardPlayed(CardEventData data)
//     {
//         Debug.Log($"{data.PlayerID} played card: {data.CardName}");
//         // Update UI or game state accordingly
//         // e.g., move card from battlefield, apply effects
//     }

//     private void HandleCardDiscarded(CardEventData data)
//     {
//         Debug.Log($"{data.PlayerID} discarded card: {data.CardName}");
//         // Update UI or game state accordingly
//         // e.g., remove card from play, add to discard pile
//     }

//     private void HandleCardObtained(CardEventData data)
//     {
//         if (data.PlayerID != this.PlayerID)
//             return; // Only handle if the obtained card belongs to this player

//         Debug.Log($"{data.PlayerID} obtained card: {data.CardName}");
//         // Update UI or game state accordingly
//         // e.g., add card to hand
//         //ObtainCard(data.CardID);
//     }

//     private void HandleCardSelected(CardEventData data)
//     {
//         if (data.PlayerID != this.PlayerID)
//             return; // Only handle if the selected card belongs to this player

//         Debug.Log($"{data.PlayerID} selected card: {data.CardName}");
//         // Update UI or game state accordingly
//         // e.g., highlight selected card
//         // This can be implemented as needed
//     }

//     #endregion

//     #region Player Actions

//     /// <summary>
//     /// Plays a card from the player's hand.
//     /// </summary>
//     /// <param name="cardData">The CardData of the card to play.</param>
//     public void PlayCard(CardData cardData)
//     {
//         if (!photonView.IsMine)
//         {
//             Debug.LogWarning("TCGPlayer: Attempted to play a card not owned by this player.");
//             return;
//         }

//         if (!Hand.Contains(cardData))
//         {
//             Debug.LogWarning("TCGPlayer: Attempted to play a card not in hand.");
//             return;
//         }

//         // Remove card from hand
//         Hand.Remove(cardData);

//         // Instantiate the TCGCard prefab and assign the CardData
//         GameObject cardObject = PhotonNetwork.Instantiate(TCGCardPrefab.name, Vector3.zero, Quaternion.identity);
//         TCGCard card = cardObject.GetComponent<TCGCard>();

//         if (card != null)
//         {
//             card.CardData = cardData;
//             card.InitializeCard(); // Initialize visual components
//             //card.transform.SetParent(Battlefield, false); // Set parent to Battlefield
//             card.PlayCard(); // Trigger the play action
//         }
//         else
//         {
//             Debug.LogError("TCGPlayer: Failed to get TCGCard component from instantiated prefab.");
//         }

//         // Trigger the OnCardPlayed event across the network
//         CardEventData eventData = new CardEventData(EventType.OnCardPlayed, cardData.CardName, cardData.GetCardID(), PlayerID);
//         PhotonEventManager.Instance.TriggerEventAcrossNetwork(EventType.OnCardPlayed, eventData);

//         // Update UI
//         UpdateDeckCount();
//         UpdateDiscardCount();
//     }

//     /// <summary>
//     /// Discards a card from the player's hand.
//     /// </summary>
//     /// <param name="cardData">The CardData of the card to discard.</param>
//     public void DiscardCard(CardData cardData)
//     {
//         if (!photonView.IsMine)
//         {
//             Debug.LogWarning("TCGPlayer: Attempted to discard a card not owned by this player.");
//             return;
//         }

//         if (!Hand.Contains(cardData))
//         {
//             Debug.LogWarning("TCGPlayer: Attempted to discard a card not in hand.");
//             return;
//         }

//         // Remove card from hand
//         Hand.Remove(cardData);

//         // Instantiate the TCGCard prefab and assign the CardData
//         GameObject cardObject = PhotonNetwork.Instantiate(TCGCardPrefab.name, Vector3.zero, Quaternion.identity);
//         TCGCard card = cardObject.GetComponent<TCGCard>();

//         if (card != null)
//         {
//             card.CardData = cardData;
//             card.InitializeCard(); // Initialize visual components
//             //card.transform.SetParent(GameStateManager.Instance.DiscardPileTransform, false); // Set parent to Discard Pile
//             card.DiscardCard(); // Trigger the discard action
//         }
//         else
//         {
//             Debug.LogError("TCGPlayer: Failed to get TCGCard component from instantiated prefab.");
//         }

//         // Trigger the OnCardDiscarded event across the network
//         CardEventData eventData = new CardEventData(EventType.OnCardDiscarded, cardData.CardName, cardData.GetCardID(), PlayerID);
//         PhotonEventManager.Instance.TriggerEventAcrossNetwork(EventType.OnCardDiscarded, eventData);

//         // Update UI
//         UpdateDiscardCount();
//     }

//     /// <summary>
//     /// Obtains a card and adds it to the player's hand.
//     /// </summary>
//     /// <param name="cardData">The CardData of the card to obtain.</param>
//     public void ObtainCard(CardData cardData)
//     {
//         if (!photonView.IsMine)
//         {
//             Debug.LogWarning("TCGPlayer: Attempted to obtain a card not owned by this player.");
//             return;
//         }

//         // Add card to hand
//         Hand.Add(cardData);

//         // Instantiate the TCGCard prefab and assign the CardData
//         GameObject cardObject = PhotonNetwork.Instantiate(TCGCardPrefab.name, Vector3.zero, Quaternion.identity);
//         TCGCard card = cardObject.GetComponent<TCGCard>();

//         if (card != null)
//         {
//             card.CardData = cardData;
//             card.InitializeCard(); // Initialize visual components
//             card.transform.SetParent(HandPanel, false); // Set parent to Hand Panel
//         }
//         else
//         {
//             Debug.LogError("TCGPlayer: Failed to get TCGCard component from instantiated prefab.");
//         }

//         // Trigger the OnCardObtained event across the network
//         CardEventData eventData = new CardEventData(EventType.OnCardObtained, cardData.CardName, cardData.GetCardID(), PlayerID);
//         PhotonEventManager.Instance.TriggerEventAcrossNetwork(EventType.OnCardObtained, eventData);

//         // Update UI
//         UpdateDeckCount();
//         UpdateDiscardCount();
//     }

//     /// <summary>
//     /// Selects a card for a specific action.
//     /// </summary>
//     /// <param name="cardData">The CardData of the card to select.</param>
//     public void SelectCard(CardData cardData)
//     {
//         if (!photonView.IsMine)
//         {
//             Debug.LogWarning("TCGPlayer: Attempted to select a card not owned by this player.");
//             return;
//         }

//         if (!Hand.Contains(cardData))
//         {
//             Debug.LogWarning("TCGPlayer: Attempted to select a card not in hand.");
//             return;
//         }

//         // Implement selection logic
//         // For example, toggle selection state or mark for action
//         // This can be handled within the TCGCard script via events

//         // Trigger the OnCardSelected event across the network
//         CardEventData eventData = new CardEventData(EventType.OnCardSelected, cardData.CardName, cardData.GetCardID(), PlayerID);
//         PhotonEventManager.Instance.TriggerEventAcrossNetwork(EventType.OnCardSelected, eventData);
//     }

//     /// <summary>
//     /// Draws a card from the player's deck into their hand.
//     /// </summary>
//     public void DrawCard()
//     {
//         if (Deck.Count > 0)
//         {
//             CardData drawnCard = Deck[0];
//             Deck.RemoveAt(0);
//             Hand.Add(drawnCard);

//             UpdateDeckCount();

//             // Instantiate the TCGCard prefab and assign the CardData
//             //GameObject cardObject = PhotonNetwork.Instantiate(TCGCardPrefab.name, Vector3.zero, Quaternion.identity);
//             GameObject cardObject = PhotonNetwork.Instantiate(TCGCardPrefab.name, Vector3.zero, Quaternion.identity);
//             TCGCard card = cardObject.GetComponent<TCGCard>();

//             if (card != null)
//             {
//                 card.CardData = drawnCard;
//                 card.InitializeCard(); // Initialize visual components
//                 card.transform.SetParent(HandPanel, false); // Set parent to Hand Panel
//             }
//             else
//             {
//                 Debug.LogError("TCGPlayer: Failed to get TCGCard component from instantiated prefab.");
//             }
//         }
//         else
//         {
//             Debug.LogWarning($"{PlayerID}'s deck is empty! Cannot draw a card.");
//             // Handle deck exhaustion if necessary (e.g., reshuffle discard pile into deck)
//         }
//     }

//     /// <summary>
//     /// Shuffles the player's deck.
//     /// </summary>
//     public void ShuffleDeck()
//     {
//         for (int i = 0; i < Deck.Count; i++)
//         {
//             CardData temp = Deck[i];
//             int randomIndex = Random.Range(i, Deck.Count);
//             Deck[i] = Deck[randomIndex];
//             Deck[randomIndex] = temp;
//         }

//         UpdateDeckCount();
//     }

//     /// <summary>
//     /// Draws the initial hand at the start of the game.
//     /// </summary>
//     /// <param name="handSize">Number of cards to draw.</param>
//     public void DrawStartingHand(int numberOfCards)
//     {
//         for (int i = 0; i < numberOfCards; i++)
//         {
//             DrawCard();
//         }
//     }

//     /// <summary>
//     /// Ends the player's turn.
//     /// </summary>
//     public void EndTurn()
//     {
//         if (!photonView.IsMine)
//             return;

//         // Trigger the end turn event across the network
//         TurnEventData eventData = new TurnEventData(EventType.OnTurnEnd, PlayerID);
//         PhotonEventManager.Instance.TriggerEventAcrossNetwork(EventType.OnTurnEnd, eventData);

//         // Reset timer for the next turn
//         ResetTimer();
//     }

//     #endregion

//     #region Helper Methods

//     /// <summary>
//     /// Updates the deck count UI element.
//     /// </summary>
//     private void UpdateDeckCount()
//     {
//         if (DeckCountText != null)
//         {
//             DeckCountText.text = $"Deck: {Deck.Count}";
//         }
//     }

//     /// <summary>
//     /// Updates the discard pile count UI element.
//     /// </summary>
//     private void UpdateDiscardCount()
//     {
//         if (DiscardCountText != null)
//         {
//             DiscardCountText.text = $"Discard: {DiscardPile.Count}";
//         }
//     }

//     /// <summary>
//     /// Updates the timer UI element.
//     /// </summary>
//     private void UpdateTimerUI()
//     {
//         if (TimerText != null)
//         {
//             int minutes = Mathf.FloorToInt(turnTimer / 60);
//             int seconds = Mathf.FloorToInt(turnTimer % 60);
//             TimerText.text = $"Time: {minutes:00}:{seconds:00}";
//         }
//     }

//     /// <summary>
//     /// Resets the turn timer to the initial duration.
//     /// </summary>
//     private void ResetTimer()
//     {
//         turnTimer = TurnDuration;
//     }

//     #endregion

//     #region Initialize Deck

//     /// <summary>
//     /// Initializes the player's deck with the initial set of cards.
//     /// </summary>
//     private void InitializeDeck()
//     {
//         if (InitialDeck == null || InitialDeck.Count == 0)
//         {
//             Debug.LogWarning("TCGPlayer: InitialDeck is empty. Cannot initialize deck.");
//             return;
//         }

//         foreach (CardData card in InitialDeck)
//         {
//             Deck.Add(card);
//         }

//         Debug.Log($"{PlayerID}'s deck initialized with {Deck.Count} cards.");
//     }

//     #endregion

//     #region Photon Networking

//     /// <summary>
//     /// Handles serialization for network synchronization.
//     /// </summary>
//     /// <param name="stream">PhotonStream for reading/writing data.</param>
//     /// <param name="info">PhotonMessageInfo with event details.</param>
//     public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
//     {
//         if (stream.IsWriting)
//         {
//             // Send data to other clients
//             stream.SendNext(Hand.Count);
//             stream.SendNext(Deck.Count);
//             stream.SendNext(DiscardPile.Count);
//             stream.SendNext(Power);
//             stream.SendNext(turnTimer);
//         }
//         else
//         {
//             // Receive data from other clients
//             int receivedHandCount = (int)stream.ReceiveNext();
//             int receivedDeckCount = (int)stream.ReceiveNext();
//             int receivedDiscardCount = (int)stream.ReceiveNext();
//             Power = (int)stream.ReceiveNext();
//             turnTimer = (float)stream.ReceiveNext();

//             // Optionally, update UI or handle received data
//             UpdateDeckCount();
//             UpdateDiscardCount();
//             UpdateTimerUI();
//         }
//     }

//     #endregion
// }
