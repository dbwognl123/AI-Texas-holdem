using System;

namespace Poker {
    public class Deck {
        private readonly int[] cards = new int[52];
        private int top;
        private System.Random rng;

        public Deck(int? seed=null) {
            rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            Reset(seed);
        }
        public void Reset(int? seed=null) {
            if (seed.HasValue) rng = new System.Random(seed.Value);
            for (int i=0;i<52;i++) cards[i]=i;
            // Fisher–Yates
            for (int i=51;i>0;i--) {
                int j = rng.Next(i+1);
                (cards[i], cards[j]) = (cards[j], cards[i]);
            }
            top = 0;
        }
        public Card Draw() {
            if (top >= 52) throw new InvalidOperationException("Deck empty");
            return new Card(cards[top++]);
        }
        public void Burn(int count=1){ for(int i=0;i<count;i++) Draw(); }
    }
}
