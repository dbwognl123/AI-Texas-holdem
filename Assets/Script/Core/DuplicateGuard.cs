using System;

namespace Poker {
    public static class DuplicateGuard {
        public static ulong Set(ulong used, Card c)=> used | (1UL<<c.id);
        public static bool Taken(ulong used, Card c)=> (used & (1UL<<c.id))!=0;
        public static void AssertNew(ref ulong used, Card c, string where){
            if (Taken(used,c)) throw new Exception($"[DUP] {c} already used at {where}");
            used = Set(used,c);
        }
    }
}
