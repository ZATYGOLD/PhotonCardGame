using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZXArcade
{
    [CreateAssetMenu(fileName = "StarterCardData", menuName = "Card/Starter", order = 7)]
    public class StarterCardData : CardData
    {
        public StarterCardName cardName;

        [Header("Stats")]
        [Range(0, 0)]
        public int StarterCardValue;
        [Range(0, 0)]
        public int StarterCardCost;

        protected void OnEnable()
        {
            if (cardType != CardType.Starter)
            {
                Debug.LogWarning($"{name}: cardType is not set to Starter. Setting it to Starter.");
                cardType = CardType.Starter;
            }

            cardValue = StarterCardValue;
            cardCost = StarterCardCost;
            cardNameID = (int)cardName;
        }

        protected override void OnValidate()
        {
            ValidateCardName<StarterCardName>(cardName);

            if (cardType != CardType.Starter)
            {
                Debug.LogWarning($"{name}: cardType is not set to Starter. Setting it to Starter.");
                cardType = CardType.Starter;
            }

            cardValue = StarterCardValue;
            cardCost = StarterCardCost;
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

    public enum StarterCardName
    {
        Punch = 01,
        Vulnerability = 02
    }
}