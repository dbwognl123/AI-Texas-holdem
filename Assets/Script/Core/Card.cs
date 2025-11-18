using System;

namespace Poker {
    public enum Suit { Clubs=0, Diamonds=1, Hearts=2, Spades=3 }
    public enum Rank { Two=0, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }

    public readonly struct Card {
        public readonly int id;        // 0..51
        public int Rank => id % 13;    // 0..12 (2..A)
        public int Suit => id / 13;    // 0..3 (c,d,h,s)
        public Card(int id) { this.id = id; }
        static readonly char[] R = "23456789TJQKA".ToCharArray();
        static readonly char[] S = "cdhs".ToCharArray();
        public override string ToString() => $"{R[Rank]}{S[Suit]}";  // 예: As, Td
    }
}
