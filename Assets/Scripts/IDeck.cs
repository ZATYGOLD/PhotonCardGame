using System.Collections.Generic;

/// <summary>
/// Interface defining common deck operations.
/// </summary>
public interface IDeck
{
    void InitializeDeck(List<CardData> cards);
    void ShuffleDeck();
    CardData DrawCard();
    void AddCard(CardData card);
    void RemoveCard(CardData card);
    int GetDeckCount();
}
