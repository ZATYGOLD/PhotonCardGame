using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZXArcade
{
    [CreateAssetMenu(fileName = "CharacterCardData", menuName = "Card/Character", order = 1)]
    public class CharacterCardData : CardData
    {
        public CharacterCardName cardName;

        protected void OnEnable()
        {
            if (cardType != CardType.Character)
            {
                Debug.LogWarning($"{name}: cardType is not set to Character. Setting it to Character.");
                cardType = CardType.Character;
            }
        }

        // Ensure card has all required fields
        protected override void OnValidate()
        {
            ValidateCardName<CharacterCardName>(cardName);

            if (cardType != CardType.Character)
            {
                Debug.LogWarning($"{name}: cardType is not set to Character. Setting it to Character.");
                cardType = CardType.Character;
            }

            InitializeCharacterCardData(CardType.Character, (int)cardName);

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

    public enum CharacterCardName
    {
        Aquaman = 01,
        Batman = 02,
        Cyborg = 03,
        GreenLantern = 04,
        Superman = 05,
        TheFlash = 06,
        Wonderwomen = 07
    }
}