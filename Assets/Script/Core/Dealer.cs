namespace Poker {
    public static class Dealer {
        // 버튼 다음(SB)부터 1장씩 두 바퀴
        public static Card[,] DealHole(Deck deck, int playerCount, int buttonSeat) {
            Card[,] hole = new Card[playerCount,2];
            int start = (buttonSeat + 1) % playerCount;
            for (int i=0;i<playerCount;i++) hole[(start+i)%playerCount,0] = deck.Draw();
            for (int i=0;i<playerCount;i++) hole[(start+i)%playerCount,1] = deck.Draw();
            return hole;
        }
        public static Card[] DealFlop(Deck d){ d.Burn(1); return new[]{ d.Draw(), d.Draw(), d.Draw() }; }
        public static Card DealTurn(Deck d){ d.Burn(1); return d.Draw(); }
        public static Card DealRiver(Deck d){ d.Burn(1); return d.Draw(); }
    }
}
