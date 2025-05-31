using UnityEngine;


[CreateAssetMenu(fileName = "LocationCardData", menuName = "Card/Location", order = 3)]
public class LocationCardData : CardData
{
    public LocationCardName cardName;

    [Header("Stats")]
    [Range(0, 1)]
    public int LocationCardValue;
    [Range(2, 5)]
    public int LocationCardCost;

    protected void OnEnable()
    {
        // Synchronize subclass fields with base class fields
        if (cardType != CardType.Location)
        {
            Debug.LogWarning($"{name}: cardType is not set to Location. Setting it to Location.");
            cardType = CardType.Location;
        }

        cardValue = LocationCardValue;
        cardCost = LocationCardCost;
        cardNameID = (int)cardName;
    }

    // Ensure card has all required fields
    protected override void OnValidate()
    {
        ValidateCardName<LocationCardName>(cardName);

        if (cardType != CardType.Location)
        {
            Debug.LogWarning($"{name}: cardType is not set to Location. Setting it to Location.");
            cardType = CardType.Location;
        }

        cardValue = LocationCardValue;
        cardCost = LocationCardCost;
        cardNameID = (int)cardName;

        base.OnValidate();
    }

    /// <summary>
    /// Overrides the CardName property to return the location's name.
    /// </summary>
    public override string CardName
    {
        get
        {
            return cardName.ToString();
        }
    }
}

public enum LocationCardName
{
    ArkhamAsylum = 01,
    FortressOfSolitude = 02,
    TheBatcave = 03,
    TheWatchtower = 04,
    TitansTower = 05
}
