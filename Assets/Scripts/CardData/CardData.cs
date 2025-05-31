using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

// Card type with a number set associated with it
// The number (Id) will help with creating a look up card feature
public enum CardType
{
    Character = 10,
    SuperVillian = 20,
    Hero = 30,
    Villian = 40,
    SuperPower = 50,
    Equipment = 60,
    Location = 70,
    Starter = 80,
    None = 90
}

public abstract class CardData : ScriptableObject
{
    [SerializeField] private int cardID;

    [Header("Primary Data")]
    [SerializeField] protected CardType cardType;
    [SerializeField] protected Sprite cardImage;

    protected int cardCost;
    protected int cardValue;
    protected int cardNameID;

    protected virtual void OnValidate()
    {
        if (cardImage == null)
            Debug.LogWarning($"{name} ({cardType} Card): Missing Image");

        if (cardCost < 0)
            Debug.LogWarning($"{name} ({cardType} Card): Card Cost cannot be negative.");

        if (cardID < 1000)
        {
            cardID = GenerateUniqueID();
        }
    }

    protected void InitializeCharacterCardData(CardType type, int id)
    {
        cardType = type;
        cardNameID = id;
    }

    protected void ValidateCardName<T>(System.Enum cardName) where T : System.Enum
    {
        if (!System.Enum.IsDefined(typeof(T), cardName))
        {
            Debug.LogWarning($"{name}: Invalid card name for {GetCardType()} type.");
        }
    }

    private int GenerateUniqueID()
    {
        int typeValue = (int)cardType;
        int cardNameValue = cardNameID;
        return int.Parse($"{typeValue}{cardNameValue}");
    }

    public int GetCardID() => cardID;
    public CardType GetCardType() => cardType;
    public Sprite GetCardImage() => cardImage;
    public int GetCardCost() => cardCost;
    public int GetCardValue() => cardValue;
    public abstract string CardName { get; }
}