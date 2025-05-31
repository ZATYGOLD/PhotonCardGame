using Unity.VisualScripting;
using UnityEngine;


[CreateAssetMenu(fileName = "HeroCardData", menuName = "Card/Hero", order = 2)]
public class HeroCardData : CardData
{
    public HeroCardName cardName;

    [Header("Stats")]
    [Range(0, 3)]
    public int HeroCardValue;
    [Range(2, 8)]
    public int HeroCardCost;

    protected void OnEnable()
    {
        // Synchronize subclass fields with base class fields
        if (cardType != CardType.Hero)
        {
            Debug.LogWarning($"{name}: cardType is not set to Hero. Setting it to Hero.");
            cardType = CardType.Hero;
        }

        cardValue = HeroCardValue;
        cardCost = HeroCardCost;
        cardNameID = (int)cardName;
    }

    // Ensure card has all required fields
    protected override void OnValidate()
    {
        ValidateCardName<HeroCardName>(cardName);

        if (cardType != CardType.Hero)
        {
            Debug.LogWarning($"{name}: cardType is not set to Hero. Setting it to Hero.");
            cardType = CardType.Hero;
        }

        cardValue = HeroCardValue;
        cardCost = HeroCardCost;
        cardNameID = (int)cardName;

        base.OnValidate();
    }

    /// <summary>
    /// Overrides the CardName property to return the hero's name.
    /// </summary>
    public override string CardName
    {
        get
        {
            return cardName.ToString();
        }
    }
}

public enum HeroCardName
{
    BlueBeetle = 01,
    Catwomen = 02,
    DarkKnight = 03,
    EneraldKnight = 04,
    GreenArrow = 05,
    HighTechHero = 06,
    JonnJonzz = 07,
    KidFlash = 08,
    KingOfAtlantis = 09,
    ManOfSteel = 10,
    Mera = 11,
    PrincessDianaOfThemyscira = 12,
    Robin = 13,
    Supergirl = 14,
    SwampThing = 15,
    TheFastestManAlive = 16,
    ZatannaZatara = 17
}
