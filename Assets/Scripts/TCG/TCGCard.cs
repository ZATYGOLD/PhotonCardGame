// // Filename: TCGCard.cs
// using Photon.Pun;
// using TMPro;
// using UnityEngine;
// using UnityEngine.UI;

// /// <summary>
// /// Represents a card in the Trading Card Game (TCG).
// /// Handles visual display, player interactions, and network synchronization.
// /// </summary>
// public class TCGCard : MonoBehaviourPun, IPunObservable
// {
//     [Header("Card Data")]
//     [Tooltip("Reference to the CardData ScriptableObject containing the card's static data.")]
//     public CardData CardData;

//     [Header("UI Components")]
//     [Tooltip("Image component for the card's artwork.")]
//     public Image ArtworkImage;

//     [Tooltip("TMP_Text component for the card's inner cost.")]
//     public TMP_Text CostInnerText;

//     [Tooltip("TMP_Text component for the card's middle cost.")]
//     public TMP_Text CostMiddleText;

//     [Tooltip("TMP_Text component for the card's outer cost.")]
//     public TMP_Text CostOuterText;

//     [Tooltip("TMP_Text component for the card's value.")]
//     public TMP_Text ValueText;

//     [Tooltip("Image component for the card's glow effect.")]
//     public Image GlowImage;

//     [Header("Card States")]
//     [Tooltip("Indicates whether the card is currently selected.")]
//     public bool IsSelected = false;

//     [Tooltip("Indicates whether the card has been played.")]
//     public bool IsPlayed = false;

//     // Reference to the parent player (if needed)
//     private TCGPlayer parentPlayer;

//     private void Start()
//     {
//         // Initialize the card's visual representation
//         InitializeCard();

//         // Find and assign the parent player (assuming the card is a child of the player's GameObject)
//         parentPlayer = GetComponentInParent<TCGPlayer>();

//         // Add click listener
//         Button button = GetComponent<Button>();
//         if (button != null)
//         {
//             button.onClick.AddListener(OnCardClicked);
//         }
//         else
//         {
//             Debug.LogWarning("TCGCard: No Button component found on the card.");
//         }
//     }

//     /// <summary>
//     /// Initializes the card's visual components based on CardData.
//     /// </summary>
//     public void InitializeCard()
//     {
//         if (CardData == null)
//         {
//             Debug.LogError("TCGCard: CardData is not assigned.");
//             return;
//         }

//         // Set the artwork
//         if (ArtworkImage != null)
//         {
//             ArtworkImage.sprite = CardData.GetCardImage();
//             ArtworkImage.SetNativeSize();
//         }
//         else
//         {
//             Debug.LogWarning("TCGCard: ArtworkImage is not assigned.");
//         }

//         // Set the costs (all three texts display the same card cost)
//         if (CostInnerText != null && CostMiddleText != null && CostOuterText != null)
//         {
//             string cardCostStr = CardData.GetCardCost().ToString();
//             CostInnerText.text = cardCostStr;
//             CostMiddleText.text = cardCostStr;
//             CostOuterText.text = cardCostStr;
//         }
//         else
//         {
//             Debug.LogWarning("TCGCard: One or more Cost TMP_Text components are not assigned.");
//         }

//         // Set the value
//         if (ValueText != null)
//         {
//             ValueText.text = CardData.GetCardValue().ToString();
//         }
//         else
//         {
//             Debug.LogWarning("TCGCard: ValueText is not assigned.");
//         }

//         // Initialize glow effect
//         UpdateGlow();
//     }

//     /// <summary>
//     /// Handles the card click event.
//     /// </summary>
//     private void OnCardClicked()
//     {
//         if (!photonView.IsMine)
//         {
//             Debug.LogWarning("TCGCard: Attempted to interact with a card not owned by this player.");
//             return;
//         }

//         // Toggle selection state
//         IsSelected = !IsSelected;
//         UpdateGlow();

//         // Trigger the OnCardSelected event
//         CardEventData eventData = new CardEventData(EventType.OnCardSelected, CardData.CardName.ToString(), CardData.GetCardID(), parentPlayer.PlayerID);
//         PhotonEventManager.Instance.TriggerEventAcrossNetwork(EventType.OnCardSelected, eventData);
//     }

//     /// <summary>
//     /// Updates the glow effect based on the card's state.
//     /// </summary>
//     private void UpdateGlow()
//     {
//         if (GlowImage == null)
//         {
//             Debug.LogWarning("TCGCard: GlowImage is not assigned.");
//             return;
//         }

//         if (IsSelected || IsPlayed)
//         {
//             GlowImage.enabled = true;
//         }
//         else
//         {
//             GlowImage.enabled = false;
//         }
//     }

//     /// <summary>
//     /// Plays the card, moving it to the battlefield.
//     /// </summary>
//     public void PlayCard()
//     {
//         if (!photonView.IsMine)
//         {
//             Debug.LogWarning("TCGCard: Attempted to play a card not owned by this player.");
//             return;
//         }

//         if (IsPlayed)
//         {
//             Debug.LogWarning("TCGCard: Card has already been played.");
//             return;
//         }

//         // // Move the card to the battlefield
//         // Transform battlefield = parentPlayer.Battlefield;
//         // if (battlefield != null)
//         // {
//         //     transform.SetParent(battlefield, false);
//         //     IsPlayed = true;
//         //     UpdateGlow();
//         // }
//         // else
//         // {
//         //     Debug.LogError("TCGCard: Battlefield reference is not assigned in the parent player.");
//         // }

//         // Trigger the OnCardPlayed event
//         CardEventData eventData = new CardEventData(EventType.OnCardSelected, CardData.CardName.ToString(), CardData.GetCardID(), parentPlayer.PlayerID);
//         PhotonEventManager.Instance.TriggerEventAcrossNetwork(EventType.OnCardPlayed, eventData);
//     }

//     /// <summary>
//     /// Discards the card, moving it to the discard pile.
//     /// </summary>
//     public void DiscardCard()
//     {
//         if (!photonView.IsMine)
//         {
//             Debug.LogWarning("TCGCard: Attempted to discard a card not owned by this player.");
//             return;
//         }

//         // // Move the card to the discard pile
//         // Transform discardPile = parentPlayer.DiscardPileTransform;
//         // if (discardPile != null)
//         // {
//         //     transform.SetParent(discardPile, false);
//         //     IsPlayed = false;
//         //     IsSelected = false;
//         //     UpdateGlow();
//         // }
//         // else
//         // {
//         //     Debug.LogError("TCGCard: DiscardPileTransform reference is not assigned in the parent player.");
//         // }

//         // Trigger the OnCardDiscarded event
//         CardEventData eventData = new CardEventData(EventType.OnCardSelected, CardData.CardName.ToString(), CardData.GetCardID(), parentPlayer.PlayerID);
//         PhotonEventManager.Instance.TriggerEventAcrossNetwork(EventType.OnCardDiscarded, eventData);
//     }

//     /// <summary>
//     /// Handles network synchronization for the card.
//     /// </summary>
//     /// <param name="stream">PhotonStream for reading/writing data.</param>
//     /// <param name="info">PhotonMessageInfo with event details.</param>
//     public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
//     {
//         if (stream.IsWriting)
//         {
//             // Send data to other clients
//             stream.SendNext(IsSelected);
//             stream.SendNext(IsPlayed);
//             // Add more data if needed
//         }
//         else
//         {
//             // Receive data from other clients
//             IsSelected = (bool)stream.ReceiveNext();
//             IsPlayed = (bool)stream.ReceiveNext();
//             UpdateGlow();
//             // Update UI or other components as needed
//         }
//     }
// }
