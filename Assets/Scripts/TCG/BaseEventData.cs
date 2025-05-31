// // Filename: BaseEventData.cs
// using UnityEngine;

// /// <summary>
// /// Base class for all event data.
// /// </summary>
// public abstract class BaseEventData
// {
//     public EventType EventType;

//     protected BaseEventData(EventType eventType)
//     {
//         EventType = eventType;
//     }
// }

// /// <summary>
// /// Event data for player-related events.
// /// </summary>
// public class PlayerEventData : BaseEventData
// {
//     public string PlayerID;

//     public PlayerEventData(EventType eventType, string playerID) : base(eventType)
//     {
//         PlayerID = playerID;
//     }
// }

// /// <summary>
// /// Event data for game start and end events.
// /// </summary>
// public class GameEventData : BaseEventData
// {
//     public string Message;
//     public string WinnerPlayerID; // Applicable for game end

//     public GameEventData(EventType eventType, string message, string winnerPlayerID = null) : base(eventType)
//     {
//         Message = message;
//         WinnerPlayerID = winnerPlayerID;
//     }
// }

// /// <summary>
// /// Event data for turn-related events.
// /// </summary>
// public class TurnEventData : BaseEventData
// {
//     public string CurrentPlayerID;

//     public TurnEventData(EventType eventType, string currentPlayerID) : base(eventType)
//     {
//         CurrentPlayerID = currentPlayerID;
//     }
// }

// /// <summary>
// /// Event data for card-related events.
// /// </summary>
// public class CardEventData : BaseEventData
// {
//     public string CardName { get; private set; }
//     public int CardID { get; private set; }
//     public string PlayerID { get; private set; }

//     public CardEventData(EventType eventType, string cardName, int cardID, string playerID)
//         : base(eventType)
//     {
//         CardName = cardName;
//         CardID = cardID;
//         PlayerID = playerID;
//     }
// }

// /// <summary>
// /// Event data for game start requests.
// /// </summary>
// public class GameStartEventData : BaseEventData
// {
//     public string RequestMessage;

//     public GameStartEventData(EventType eventType, string requestMessage) : base(eventType)
//     {
//         RequestMessage = requestMessage;
//     }
// }