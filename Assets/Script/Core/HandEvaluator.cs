using System;
using System.Collections.Generic;
using System.Linq;

namespace Poker
{
    // 족보 카테고리 (값이 클수록 강함)
    public enum HandCategory {
        HighCard = 0, OnePair = 1, TwoPair = 2, ThreeKind = 3,
        Straight = 4, Flush = 5, FullHouse = 6, FourKind = 7, StraightFlush = 8
    }

    // 비교 가능한 핸드 점수(카테고리 + 타이브레이커)
    public readonly struct HandRank : IComparable<HandRank>
    {
        public readonly HandCategory Category;
        public readonly int[] Tie; // 내림차순(A=12 … 2=0)

        public HandRank(HandCategory cat, IEnumerable<int> tie) {
            Category = cat; Tie = tie.ToArray();
        }
        public int CompareTo(HandRank other) {
            int c = Category.CompareTo(other.Category);
            if (c != 0) return c;
            int n = Math.Max(Tie.Length, other.Tie.Length);
            for (int i = 0; i < n; i++) {
                int a = i < Tie.Length ? Tie[i] : -1;
                int b = i < other.Tie.Length ? other.Tie[i] : -1;
                if (a != b) return a.CompareTo(b);
            }
            return 0;
        }
        public override string ToString() => $"{Category} [{string.Join(",", Tie)}]";
    }

    public static class HandEvaluator
    {
        // 7장(홀2+보드5)에서 최상 5장을 찾아 점수
        public static HandRank Evaluate7(Card[] seven) {
            if (seven == null || seven.Length != 7) throw new ArgumentException("needs 7 cards");
            HandRank best = default; bool has = false;
            for (int a=0; a<7; a++)
            for (int b=a+1; b<7; b++)
            for (int c=b+1; c<7; c++)
            for (int d=c+1; d<7; d++)
            for (int e=d+1; e<7; e++) {
                var r = Evaluate5(seven[a], seven[b], seven[c], seven[d], seven[e]);
                if (!has || r.CompareTo(best) > 0) { best = r; has = true; }
            }
            return best;
        }

        // 쇼다운: 동점 허용
        public static (List<int> winners, HandRank best, Dictionary<int,HandRank> ranks)
            GetWinners(Card[,] hole, Card[] flop, Card turn, Card river)
        {
            int n = hole.GetLength(0);
            var board = new Card[]{ flop[0], flop[1], flop[2], turn, river };
            var ranks = new Dictionary<int,HandRank>(n);
            HandRank best = default; bool has = false;

            for (int s=0; s<n; s++) {
                var seven = new Card[]{ hole[s,0], hole[s,1], board[0],board[1],board[2],board[3],board[4] };
                var r = Evaluate7(seven);
                ranks[s] = r;
                if (!has || r.CompareTo(best) > 0) { best = r; has = true; }
            }
            var winners = ranks.Where(kv => kv.Value.CompareTo(best) == 0).Select(kv => kv.Key).ToList();
            return (winners, best, ranks);
        }

        // ---------- 내부: 5장 평가 ----------
        static HandRank Evaluate5(Card c1, Card c2, Card c3, Card c4, Card c5)
        {
            var cs = new[] { c1, c2, c3, c4, c5 };
            int[] suitCnt = new int[4];
            int[] rankCnt = new int[13];
            int rankMask = 0;

            foreach (var c in cs) { suitCnt[c.Suit]++; rankCnt[c.Rank]++; rankMask |= (1 << c.Rank); }

            bool isFlush = suitCnt[0]==5 || suitCnt[1]==5 || suitCnt[2]==5 || suitCnt[3]==5;
            int straightHigh = HighestStraight(rankMask);
            bool isStraight = straightHigh >= 0;

            var groups = new List<(int count,int rank)>();
            for (int r=12;r>=0;r--) if (rankCnt[r]>0) groups.Add((rankCnt[r], r));
            groups.Sort((a,b)=> a.count!=b.count ? a.count.CompareTo(b.count) : a.rank.CompareTo(b.rank));

            if (isFlush && isStraight && IsStraightFlush(cs, straightHigh))
                return new HandRank(HandCategory.StraightFlush, new[]{ straightHigh });

            if (groups[^1].count == 4) { // 포카드
                int quad = groups[^1].rank, kicker = groups[^2].rank;
                return new HandRank(HandCategory.FourKind, new[]{ quad, kicker });
            }

            if (groups[^1].count == 3 && groups[^2].count >= 2) { // 풀하우스
                int trips = groups[^1].rank, pair = groups[^2].rank;
                return new HandRank(HandCategory.FullHouse, new[]{ trips, pair });
            }

            if (isFlush) {
                var ranks = cs.OrderByDescending(c=>c.Rank).Select(c=>c.Rank);
                return new HandRank(HandCategory.Flush, ranks);
            }

            if (isStraight) return new HandRank(HandCategory.Straight, new[]{ straightHigh });

            if (groups[^1].count == 3) { // 트립스
                int t = groups[^1].rank;
                var kick = TakeRanks(rankCnt, new[]{t}, 2);
                return new HandRank(HandCategory.ThreeKind, new[]{ t }.Concat(kick));
            }

            if (groups[^1].count == 2 && groups[^2].count == 2) { // 투페어
                int hi = Math.Max(groups[^1].rank, groups[^2].rank);
                int lo = Math.Min(groups[^1].rank, groups[^2].rank);
                var k = TakeRanks(rankCnt, new[]{hi,lo}, 1);
                return new HandRank(HandCategory.TwoPair, new[]{ hi, lo }.Concat(k));
            }

            if (groups[^1].count == 2) { // 원페어
                int p = groups[^1].rank;
                var k = TakeRanks(rankCnt, new[]{p}, 3);
                return new HandRank(HandCategory.OnePair, new[]{ p }.Concat(k));
            }

            var highs = TakeRanks(rankCnt, Array.Empty<int>(), 5);
            return new HandRank(HandCategory.HighCard, highs);
        }

        static IEnumerable<int> TakeRanks(int[] rankCnt, IEnumerable<int> exclude, int take) {
            var ex = new HashSet<int>(exclude ?? Array.Empty<int>());
            var res = new List<int>(take);
            for (int r=12; r>=0 && res.Count<take; r--) if (!ex.Contains(r) && rankCnt[r]>0) res.Add(r);
            return res;
        }

        static int HighestStraight(int mask)
        {
            for (int hi=12; hi>=4; hi--) {
                int seq = (1<<hi)|(1<<(hi-1))|(1<<(hi-2))|(1<<(hi-3))|(1<<(hi-4));
                if ((mask & seq) == seq) return hi;
            }
            int wheel = (1<<12)|(1<<3)|(1<<2)|(1<<1)|(1<<0); // A-5
            if ((mask & wheel) == wheel) return 3;
            return -1;
        }

        static bool IsStraightFlush(Card[] five, int straightHigh)
        {
            int suit = five[0].Suit;
            foreach (var c in five) if (c.Suit != suit) return false;
            int mask = 0; foreach (var c in five) mask |= (1 << c.Rank);
            if (straightHigh == 3) { // A-2-3-4-5
                int wheel = (1<<12)|(1<<3)|(1<<2)|(1<<1)|(1<<0);
                return (mask & wheel) == wheel;
            }
            int seq = (1<<straightHigh)|(1<<(straightHigh-1))|(1<<(straightHigh-2))|(1<<(straightHigh-3))|(1<<(straightHigh-4));
            return (mask & seq) == seq;
        }
    }
}
