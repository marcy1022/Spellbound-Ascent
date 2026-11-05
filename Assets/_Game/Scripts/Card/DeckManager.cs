using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpellboundAscent.Card
{
    public class DeckManager
    {
        public List<CardInstance> DrawPile { get; private set; } = new();
        public List<CardInstance> Hand { get; private set; } = new();
        public List<CardInstance> DiscardPile { get; private set; } = new();
        public List<CardInstance> ExhaustPile { get; private set; } = new();

        public event Action<CardInstance> OnCardDrawn;
        public event Action<CardInstance> OnCardDiscarded;
        public event Action<CardInstance> OnCardExhausted;
        public event Action OnHandChanged;

        int _maxHandSize;

        public DeckManager(int maxHandSize)
        {
            _maxHandSize = maxHandSize;
        }

        public void InitFromDeck(List<CardInstance> deck)
        {
            DrawPile.Clear();
            Hand.Clear();
            DiscardPile.Clear();
            ExhaustPile.Clear();

            DrawPile.AddRange(deck);
            Shuffle(DrawPile);
        }

        public void DrawCards(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (Hand.Count >= _maxHandSize) break;

                if (DrawPile.Count == 0)
                {
                    if (DiscardPile.Count == 0) break;
                    ReshuffleDiscard();
                }

                if (DrawPile.Count == 0) break;

                var card = DrawPile[0];
                DrawPile.RemoveAt(0);
                Hand.Add(card);
                OnCardDrawn?.Invoke(card);
            }
            OnHandChanged?.Invoke();
        }

        public void DiscardCard(CardInstance card)
        {
            Hand.Remove(card);
            DiscardPile.Add(card);
            OnCardDiscarded?.Invoke(card);
            OnHandChanged?.Invoke();
        }

        public void ExhaustCard(CardInstance card)
        {
            Hand.Remove(card);
            ExhaustPile.Add(card);
            OnCardExhausted?.Invoke(card);
            OnHandChanged?.Invoke();
        }

        public void DiscardHand()
        {
            while (Hand.Count > 0)
            {
                var card = Hand[0];
                Hand.RemoveAt(0);

                if (card.Data.ethereal)
                    ExhaustPile.Add(card);
                else
                    DiscardPile.Add(card);
            }
            OnHandChanged?.Invoke();
        }

        public void PlayCard(CardInstance card)
        {
            Hand.Remove(card);
            if (card.Data.exhaust || card.Data.type == "power")
                ExhaustPile.Add(card);
            else
                DiscardPile.Add(card);
            OnHandChanged?.Invoke();
        }

        void ReshuffleDiscard()
        {
            DrawPile.AddRange(DiscardPile);
            DiscardPile.Clear();
            Shuffle(DrawPile);
        }

        static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
