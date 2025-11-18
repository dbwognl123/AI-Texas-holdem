using UnityEngine;

namespace Poker {
    public class CommunityView : MonoBehaviour {
        public CardSlot flop1, flop2, flop3, turn, river;
        public void Clear(CardSpriteProvider sp){
            flop1.ShowBack(sp); flop2.ShowBack(sp); flop3.ShowBack(sp);
            turn.ShowBack(sp);  river.ShowBack(sp);
        }
        public void SetFlop(Card[] f, CardSpriteProvider sp){
            flop1.ShowFace(f[0], sp); flop2.ShowFace(f[1], sp); flop3.ShowFace(f[2], sp);
        }
        public void SetTurn(Card t, CardSpriteProvider sp){ turn.ShowFace(t, sp); }
        public void SetRiver(Card r, CardSpriteProvider sp){ river.ShowFace(r, sp); }
    }
}
