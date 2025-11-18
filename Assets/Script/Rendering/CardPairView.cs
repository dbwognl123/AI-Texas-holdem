using UnityEngine;

namespace Poker {
    public class CardPairView : MonoBehaviour {
        public CardSlot leftSlot;
        public CardSlot rightSlot;

        public void SetHole(Card a, Card b, CardSpriteProvider sp, bool faceUp=false) {
            if (faceUp) { leftSlot.ShowFace(a,sp); rightSlot.ShowFace(b,sp); }
            else        { leftSlot.ShowBack(sp);  rightSlot.ShowBack(sp);  }
        }
        public void Reveal(Card a, Card b, CardSpriteProvider sp) {
            leftSlot.ShowFace(a,sp); rightSlot.ShowFace(b,sp);
        }
        public void Hide(CardSpriteProvider sp) { leftSlot.ShowBack(sp); rightSlot.ShowBack(sp); }
    }
}
