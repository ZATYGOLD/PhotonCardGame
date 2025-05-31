// // Filename: PhotonEventManager.cs
// using System;
// using System.Collections.Generic;
// using ExitGames.Client.Photon;
// using Photon.Pun;
// using Photon.Realtime;
// using UnityEngine;

// /// <summary>
// /// Interface for event listeners.
// /// </summary>
// public interface IEventListener
// {
//     void OnEventReceived(BaseEventData data);
// }

// /// <summary>
// /// Enumeration of different event types used in the game.
// /// Ensure that these match with those defined in other scripts.
// /// </summary>
// public enum EventType
// {
//     OnCardPlayed,
//     OnCardDiscarded,
//     OnCardObtained,
//     OnCardSelected,
//     OnTurnStart,
//     OnTurnEnd,
//     OnGameStart,
//     OnGameEnd,
//     OnPlayerJoined,
//     OnPlayerLeft,
//     OnGameStartRequest
//     // Add more event types as needed
// }

// /// <summary>
// /// Manages custom Photon events and facilitates communication between players.
// /// </summary>
// public class PhotonEventManager : MonoBehaviourPunCallbacks, IOnEventCallback
// {
//     #region Singleton Implementation

//     /// <summary>
//     /// Singleton instance for easy access.
//     /// </summary>
//     public static PhotonEventManager Instance { get; private set; }

//     private void Awake()
//     {
//         // Initialize Singleton
//         if (Instance == null)
//         {
//             Instance = this;
//             DontDestroyOnLoad(gameObject);

//             // Initialize reverse mapping
//             foreach (var pair in eventTypeToCode)
//             {
//                 codeToEventType[pair.Value] = pair.Key;
//             }
//         }
//         else
//         {
//             Destroy(gameObject);
//         }
//     }

//     #endregion

//     #region EventType Definitions

//     /// <summary>
//     /// Mapping from EventType to unique byte codes for Photon.
//     /// </summary>
//     private Dictionary<EventType, byte> eventTypeToCode = new Dictionary<EventType, byte>()
//     {
//         { EventType.OnCardPlayed, 1 },
//         { EventType.OnCardDiscarded, 2 },
//         { EventType.OnCardObtained, 3 },
//         { EventType.OnCardSelected, 4 },
//         { EventType.OnTurnStart, 5 },
//         { EventType.OnTurnEnd, 6 },
//         { EventType.OnGameStart, 7 },
//         { EventType.OnGameEnd, 8 },
//         { EventType.OnPlayerJoined, 9 },
//         { EventType.OnPlayerLeft, 10 },
//         { EventType.OnGameStartRequest, 11 }
//         // Add more mappings as needed
//     };

//     /// <summary>
//     /// Reverse mapping from byte code to EventType.
//     /// </summary>
//     private Dictionary<byte, EventType> codeToEventType = new Dictionary<byte, EventType>();

//     #endregion

//     #region Event Subscription Management

//     /// <summary>
//     /// Dictionary to hold listeners for each EventType.
//     /// </summary>
//     private Dictionary<EventType, List<IEventListener>> listeners = new Dictionary<EventType, List<IEventListener>>();

//     /// <summary>
//     /// Subscribe a listener to a specific EventType.
//     /// </summary>
//     /// <param name="eventType">The type of event to subscribe to.</param>
//     /// <param name="listener">The listener implementing IEventListener.</param>
//     public void Subscribe(EventType eventType, IEventListener listener)
//     {
//         if (!listeners.ContainsKey(eventType))
//         {
//             listeners[eventType] = new List<IEventListener>();
//         }

//         if (!listeners[eventType].Contains(listener))
//         {
//             listeners[eventType].Add(listener);
//         }
//     }

//     /// <summary>
//     /// Unsubscribe a listener from a specific EventType.
//     /// </summary>
//     /// <param name="eventType">The type of event to unsubscribe from.</param>
//     /// <param name="listener">The listener implementing IEventListener.</param>
//     public void Unsubscribe(EventType eventType, IEventListener listener)
//     {
//         if (listeners.ContainsKey(eventType))
//         {
//             listeners[eventType].Remove(listener);
//             if (listeners[eventType].Count == 0)
//             {
//                 listeners.Remove(eventType);
//             }
//         }
//     }

//     #endregion

//     #region Photon Callbacks

//     /// <summary>
//     /// Photon callback for receiving events.
//     /// </summary>
//     /// <param name="photonEvent">The Photon event data.</param>
//     public void OnEvent(EventData photonEvent)
//     {
//         byte eventCode = photonEvent.Code;

//         if (codeToEventType.TryGetValue(eventCode, out EventType eventType))
//         {
//             string jsonData = photonEvent.CustomData as string;
//             if (string.IsNullOrEmpty(jsonData))
//             {
//                 Debug.LogWarning("Received empty event data.");
//                 return;
//             }

//             // Deserialize JSON string back to EventData object
//             BaseEventData eventData = DeserializeEventData(eventType, jsonData);
//             if (eventData != null)
//             {
//                 // Notify all listeners subscribed to this event type
//                 if (listeners.ContainsKey(eventType))
//                 {
//                     foreach (var listener in listeners[eventType])
//                     {
//                         listener.OnEventReceived(eventData);
//                     }
//                 }
//             }
//         }
//     }

//     /// <summary>
//     /// Add the Photon callback target when the object is enabled.
//     /// </summary>
//     new void OnEnable()
//     {
//         base.OnEnable();
//         PhotonNetwork.AddCallbackTarget(this);
//     }

//     /// <summary>
//     /// Remove the Photon callback target when the object is disabled.
//     /// </summary>
//     new void OnDisable()
//     {
//         base.OnDisable();
//         PhotonNetwork.RemoveCallbackTarget(this);
//     }

//     #endregion

//     #region Event Triggering

//     /// <summary>
//     /// Trigger an event across the network.
//     /// </summary>
//     /// <typeparam name="T">Type of BaseEventData.</typeparam>
//     /// <param name="eventType">The type of event to trigger.</param>
//     /// <param name="data">The event data to send.</param>
//     public void TriggerEventAcrossNetwork<T>(EventType eventType, T data) where T : BaseEventData
//     {
//         if (eventTypeToCode.TryGetValue(eventType, out byte code))
//         {
//             // Serialize EventData to JSON string
//             string jsonData = JsonUtility.ToJson(data);
//             PhotonNetwork.RaiseEvent(code, jsonData, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
//         }
//         else
//         {
//             Debug.LogError($"No byte code mapped for EventType: {eventType}");
//         }
//     }

//     #endregion

//     #region Event Serialization

//     /// <summary>
//     /// Deserialize JSON string to the appropriate BaseEventData subclass based on EventType.
//     /// </summary>
//     /// <param name="eventType">The type of event.</param>
//     /// <param name="jsonData">The JSON serialized event data.</param>
//     /// <returns>The deserialized event data.</returns>
//     private BaseEventData DeserializeEventData(EventType eventType, string jsonData)
//     {
//         switch (eventType)
//         {
//             case EventType.OnCardPlayed:
//             case EventType.OnCardDiscarded:
//             case EventType.OnCardObtained:
//             case EventType.OnCardSelected:
//                 return JsonUtility.FromJson<CardEventData>(jsonData);
//             case EventType.OnTurnStart:
//             case EventType.OnTurnEnd:
//                 return JsonUtility.FromJson<TurnEventData>(jsonData);
//             case EventType.OnGameStart:
//             case EventType.OnGameEnd:
//                 return JsonUtility.FromJson<GameEventData>(jsonData);
//             case EventType.OnPlayerJoined:
//             case EventType.OnPlayerLeft:
//                 return JsonUtility.FromJson<PlayerEventData>(jsonData);
//             case EventType.OnGameStartRequest:
//                 return JsonUtility.FromJson<GameStartEventData>(jsonData);
//             // Add more cases as needed
//             default:
//                 Debug.LogWarning($"Unhandled EventType during deserialization: {eventType}");
//                 return null;
//         }
//     }

//     #endregion
// }
