using UnityEngine;
using UnityEngine.UI;

namespace Poker {
    public class CardSlot : MonoBehaviour {
        [Header("둘 중 하나만 연결해도 됩니다")]
        public SpriteRenderer spriteRenderer; // 2D 스프라이트라면 여기에 할당
        public Image image;                   // UGUI 이미지라면 여기에 할당

        // 카드 뒷면 표시
        public void ShowBack(CardSpriteProvider sp) {
            if (sp == null) return;
            SetSprite(sp.backSprite);
        }

        // 카드 앞면 표시
        public void ShowFace(Card card, CardSpriteProvider sp) {
            if (sp == null) return;
            SetSprite(sp.GetSprite(card));
        }

        private void SetSprite(Sprite s) {
            if (spriteRenderer != null) spriteRenderer.sprite = s;
            if (image != null) image.sprite = s;
        }
    }
}
