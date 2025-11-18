// Assets/Scripts/Rendering/CardSpriteProvider.cs
using UnityEngine;
using System.Linq;

namespace Poker {
  [System.Serializable] public struct CardSpriteEntry {
    public string key; public Sprite sprite; // (문자열 키 방식도 계속 지원)
  }

  public class CardSpriteProvider : MonoBehaviour {
    [Header("Common")]
    public Sprite backSprite;

    [Header("Option A) 문자열 키 매핑 (As, Td ...)")]
    public CardSpriteEntry[] entries; // 써도 되고 안 써도 됨

    [Header("Option B) Deck06 번호 매핑")]
    public Sprite[] deck06;           // Deck06_0..54 를 '이름순'으로 전부 드래그
    // 배열 인덱스 기준 시작 위치(빈 칸 포함/미포함에 맞춰 수정)
    public int startHearts   = 0;     // Hearts 시작 인덱스
    public int startClubs    = 14;    // Clubs  시작 인덱스
    public int startDiamonds = 28;    // Diamonds 시작 인덱스
    public int startSpades   = 42;    // Spades  시작 인덱스

    [Tooltip("아틀라스의 랭크 순서. 보통 \"A23456789TJQK\" 또는 \"23456789TJQKA\"")]
    public string rankOrderInAtlas = "23456789TJQKA";

    // 내부 캐시
    System.Collections.Generic.Dictionary<string, Sprite> dict;
    int[] rankIndexMap = new int[13]; // 내 랭크(2..A) -> 아틀라스 내 위치

    void Awake() {
      // 문자열 키 매핑 사용 시
      if (entries != null && entries.Length > 0)
        dict = entries.ToDictionary(e => e.key.ToLower(), e => e.sprite);

      // 내 랭크 문자(2..A) -> 아틀라스 랭크 위치(0..12) 만들기
      const string our = "23456789TJQKA"; // Card.Rank 0..12 에 대응
      for (int i = 0; i < 13; i++) {
        char c = our[i];
        int idx = rankOrderInAtlas.ToUpper().IndexOf(char.ToUpper(c));
        rankIndexMap[i] = idx >= 0 ? idx : i; // 못 찾으면 동일 인덱스
      }
    }

    public Sprite GetSprite(Card c) {
      // 번호 매핑이 세팅돼 있으면 우선 사용
      if (deck06 != null && deck06.Length > 0) {
        int start = c.Suit switch {    // Card.Suit: 0C,1D,2H,3S (우리 enum 기준)
          2 => startHearts,
          0 => startClubs,
          1 => startDiamonds,
          3 => startSpades,
          _ => 0
        };
        int idx = start + rankIndexMap[c.Rank];
        if (0 <= idx && idx < deck06.Length && deck06[idx] != null) return deck06[idx];
        return backSprite;
      }

      // 아니면 문자열 키 방식 사용 (As/Td/..)
      if (dict != null && dict.TryGetValue(c.ToString().ToLower(), out var sp)) return sp;
      return backSprite;
    }
  }
}
