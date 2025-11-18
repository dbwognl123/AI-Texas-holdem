// PlayerAction.cs
namespace Poker
{
    public enum ActionType { Fold, Check, Call, Raise, AllIn }

    public struct PlayerAction
    {
        public int seat;          // 0..5
        public ActionType type;
        public int amount;        // 칩(없으면 0)
        public override string ToString() => $"seat {seat} -> {type} {amount}";
    }
}
