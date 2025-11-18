// Assets/Scripts/Core/TableManager.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Poker {
    public class TableManager : MonoBehaviour
    {
        [Range(0,5)] public int humanSeat = 0;          // 본인 좌석 인덱스
        public bool revealHumanOnStart = true; 


        [Header("Refs")]
        public CardSpriteProvider spriteProvider;
        public CommunityView communityView;
        public CardPairView[] seats = new CardPairView[6]; // 0:Human, 1~5:AI
        public ActionUI actionUI;                           // UI 버튼 연결

        [Header("Rules")]
        public int smallBlind = 50;
        public int bigBlind = 100;
        [Range(0, 5)] public int buttonSeat = 0;
        public int startingStack = 10000;

        // 런타임 상태
        Deck deck;
        Card[,] hole; Card[] flop; Card turn; Card river;

        class PRuntime
        {
            public CardPairView view;
            public int stack;
            public int betThisRound;
            public bool folded;
            public bool allIn;
            public Card c0, c1;
        }
        PRuntime[] P;
        int pot;
        int currentBet;     // 이번 라운드 기준 금액(콜하려면 betThisRound를 이 값까지 맞춰야)
        int lastRaise;      // 마지막 레이즈의 증가량(최소 레이즈 계산에 사용)
        int playersAlive;   // 폴드/올인 제외하고 행동 가능한 사람 수(라운드 종결 판단에 사용)

        void Start()
        {
            // 플레이어 런타임 준비
            P = new PRuntime[6];
            for (int i = 0; i < 6; i++) P[i] = new PRuntime { view = seats[i], stack = startingStack };
            StartCoroutine(PlayOneHand());
        }

        [ContextMenu("Deal New Hand")]
        public void DebugDeal() { StopAllCoroutines(); StartCoroutine(PlayOneHand()); }

        IEnumerator PlayOneHand()
{
    // ---- 1) 딜 / 초기화
    deck = new Deck(); 
    deck.Reset();

    hole  = Dealer.DealHole(deck, 6, buttonSeat);
    flop  = Dealer.DealFlop(deck);
    turn  = Dealer.DealTurn(deck);
    river = Dealer.DealRiver(deck);

    pot = 0;
    foreach (var pr in P) { pr.betThisRound = 0; pr.folded = false; pr.allIn = false; }
    for (int s = 0; s < 6; s++) { P[s].c0 = hole[s, 0]; P[s].c1 = hole[s, 1]; }

    // ---- 홀카드 표시 (사람 좌석만 앞면, 나머지는 뒷면)
    for (int s = 0; s < 6; s++)
    {
        bool faceUp = revealHumanOnStart && (s == humanSeat);
        seats[s].SetHole(P[s].c0, P[s].c1, spriteProvider, faceUp);
    }
    communityView.Clear(spriteProvider);

    // (안전장치) 한 프레임 양보 후 사람 카드 다시 한 번 강제 공개
    yield return null;
    if (revealHumanOnStart)
        seats[humanSeat].Reveal(P[humanSeat].c0, P[humanSeat].c1, spriteProvider);

    // ---- 2) 블라인드
    int sbSeat = (buttonSeat + 1) % 6, bbSeat = (buttonSeat + 2) % 6;
    PostBlind(sbSeat, smallBlind);
    PostBlind(bbSeat, bigBlind);
    currentBet = bigBlind;
    lastRaise  = bigBlind;

    // ---- 3) 프리플랍
    int start = (buttonSeat + 3) % 6; // UTG
    yield return StartCoroutine(BettingRound(start));
    if (CountActive() <= 1) { AwardPotToLast(); yield break; }

    // ---- 4) 플랍
    communityView.SetFlop(flop, spriteProvider);
    ResetRoundVars();
    start = (buttonSeat + 1) % 6; // 버튼 다음(SB)부터
    yield return StartCoroutine(BettingRound(start));
    if (CountActive() <= 1) { AwardPotToLast(); yield break; }

    // ---- 5) 턴
    communityView.SetTurn(turn, spriteProvider);
    ResetRoundVars();
    yield return StartCoroutine(BettingRound(start));
    if (CountActive() <= 1) { AwardPotToLast(); yield break; }

    // ---- 6) 리버
    communityView.SetRiver(river, spriteProvider);
    ResetRoundVars();
    yield return StartCoroutine(BettingRound(start));
    if (CountActive() <= 1) { AwardPotToLast(); yield break; }

    // ---- 7) 쇼다운 (사이드팟 미구현: 전원 동일 팟으로 가정)
    var result = HandEvaluator.GetWinners(hole, flop, turn, river);
    foreach (var w in result.winners) Debug.Log($"Winner Seat {w} — {result.ranks[w]}");
    int share = pot / result.winners.Count;
    foreach (var w in result.winners) P[w].stack += share;
    Debug.Log($"Pot {pot} split {result.winners.Count} -> each {share}");
    RevealAll();
    yield return null;
}


        void ResetRoundVars()
        {
            foreach (var pr in P) pr.betThisRound = 0;
            currentBet = 0; lastRaise = bigBlind; // 플랍 이후 첫 베팅 최소=BB
        }

        void PostBlind(int seat, int amount)
        {
            int pay = Mathf.Min(amount, P[seat].stack);
            P[seat].stack -= pay;
            P[seat].betThisRound += pay;
            pot += pay;
            Debug.Log($"Seat {seat} posts blind {pay}");
        }

        int CountActive() => P.Count(x => !x.folded);

        void AwardPotToLast()
        {
            int winner = System.Array.FindIndex(P, p => !p.folded);
            if (winner < 0) return;
            P[winner].stack += pot;
            Debug.Log($"All others folded. Seat {winner} wins {pot}");
            RevealAll();
        }
        private async Task<PlayerAction> RequestActionAsync(int seat, int toCall, ActionType[] legal)
{
    if (seat == 0)
    {
        // 사람: UI 버튼 활성 상태 반영 후 선택 기다림
        if (actionUI) actionUI.EnableLegalButtons(legal);
        var res = await actionUI.WaitUserActionAsync();
        return res;
    }
    else
    {
        // 간이 AI: 약간의 비동기 양보 후 랜덤 정책
        await Task.Yield();
        var rnd = UnityEngine.Random.value;

        if (toCall > 0)
        {
            if (legal.Contains(ActionType.Fold) && rnd < 0.2f)
                return new PlayerAction { seat = seat, type = ActionType.Fold };

            if (legal.Contains(ActionType.Call) && rnd < 0.9f)
                return new PlayerAction { seat = seat, type = ActionType.Call, amount = toCall };

            // Raise
            int raiseTo = currentBet + Mathf.Max(lastRaise, bigBlind);
            return new PlayerAction { seat = seat, type = ActionType.Raise, amount = raiseTo };
        }
        else
        {
            if (legal.Contains(ActionType.Check) && rnd < 0.8f)
                return new PlayerAction { seat = seat, type = ActionType.Check };

            int raiseTo = currentBet + Mathf.Max(lastRaise, bigBlind);
            return new PlayerAction { seat = seat, type = ActionType.Raise, amount = raiseTo };
        }
    }
}

        void RevealAll()
        {
            for (int s = 0; s < 6; s++) seats[s].Reveal(P[s].c0, P[s].c1, spriteProvider);
        }

        // ---------- 베팅 라운드 (간이 규칙: 사이드팟 X, 고정 최소 레이즈) ----------
        IEnumerator BettingRound(int startSeat)
        {
            // 라운드 시작 시점의 행동 가능 인원
            playersAlive = P.Count(p => !p.folded && !p.allIn);
            if (playersAlive <= 1) yield break;

            int seat = startSeat;
            int consecutiveNoRaise = 0; // raise 없이 지나간 연속 인원 수(행동 가능 인원만 카운트)

            // 무한 루프 방지
            int guard = 0, guardMax = 200;

            while (playersAlive > 1 && consecutiveNoRaise < playersAlive && guard++ < guardMax)
            {
                if (P[seat].folded || P[seat].allIn) { seat = NextSeat(seat); continue; }

                int toCall = currentBet - P[seat].betThisRound;
                var legal = GetLegalActions(seat, toCall);
                var task = RequestActionAsync(seat, toCall, legal); // ← 비동기 요청 시작

                while (!task.IsCompleted) yield return null;        // ← 코루틴에서 완료 대기
                var pa = task.Result;                               // ← 결과 수령




                ApplyAction(seat, ref pa, toCall);

                // 라운드 종결 카운트 업데이트
                if (pa.type == ActionType.Raise || pa.type == ActionType.AllIn)
                {
                    consecutiveNoRaise = 1; // 레이즈한 사람 1로 리셋
                }
                else
                {
                    consecutiveNoRaise++;
                }

                // 다음 좌석
                seat = NextSeat(seat);
            }
            yield return null;
        }

        int NextSeat(int s) { do { s = (s + 1) % 6; } while (P[s].folded || P[s].allIn); return s; }

        ActionType[] GetLegalActions(int seat, int toCall)
        {
            var list = new List<ActionType>();
            if (toCall > 0) { list.Add(ActionType.Fold); list.Add(ActionType.Call); }
            else { list.Add(ActionType.Check); }
            // 레이즈/베팅 가능 조건(스택에 여유가 있어야)
            bool canRaise = P[seat].stack > toCall + 0;
            if (canRaise) list.Add(ActionType.Raise);
            // 올인은 항상 허용(스택>0)
            if (P[seat].stack > 0) list.Add(ActionType.AllIn);
            return list.ToArray();
        }

        

        void ApplyAction(int seat, ref PlayerAction a, int toCall)
        {
            switch (a.type)
            {
                case ActionType.Fold:
                    P[seat].folded = true;
                    playersAlive = Mathf.Max(0, playersAlive - 1);
                    Debug.Log($"Seat {seat} FOLD");
                    break;

                case ActionType.Check:
                    Debug.Log($"Seat {seat} CHECK");
                    break;

                case ActionType.Call:
                    {
                        int pay = Mathf.Min(toCall, P[seat].stack);
                        P[seat].stack -= pay;
                        P[seat].betThisRound += pay;
                        pot += pay;
                        if (P[seat].stack == 0) { P[seat].allIn = true; playersAlive = Mathf.Max(0, playersAlive - 1); }
                        Debug.Log($"Seat {seat} CALL {pay}");
                        break;
                    }

                case ActionType.Raise:
                    {
                        // 간이: Raise "to" 금액으로 해석 (미입력시 최소 레이즈)
                        int minRaiseTo = (currentBet == 0)
                          ? Mathf.Max(bigBlind, lastRaise)           // 첫 베팅
                          : currentBet + Mathf.Max(lastRaise, bigBlind);
                        int raiseTo = a.amount > 0 ? Mathf.Max(a.amount, minRaiseTo) : minRaiseTo;
                        int need = raiseTo - P[seat].betThisRound;
                        need = Mathf.Clamp(need, 0, P[seat].stack);
                        P[seat].stack -= need;
                        P[seat].betThisRound += need;
                        pot += need;

                        lastRaise = (currentBet == 0) ? raiseTo : (raiseTo - currentBet);
                        currentBet = Mathf.Max(currentBet, raiseTo);

                        if (P[seat].stack == 0) { a.type = ActionType.AllIn; P[seat].allIn = true; playersAlive = Mathf.Max(0, playersAlive - 1); }
                        Debug.Log($"Seat {seat} RAISE TO {raiseTo} (need {need})");
                        break;
                    }

                case ActionType.AllIn:
                    {
                        int need = P[seat].stack;
                        P[seat].stack = 0;
                        P[seat].betThisRound += need;
                        pot += need;

                        // 올인이 레이즈 역할을 할 수도 있으므로 currentBet 갱신
                        int newBet = P[seat].betThisRound;
                        if (newBet > currentBet)
                        {
                            lastRaise = (currentBet == 0) ? newBet : (newBet - currentBet);
                            currentBet = newBet;
                        }

                        P[seat].allIn = true;
                        playersAlive = Mathf.Max(0, playersAlive - 1);
                        Debug.Log($"Seat {seat} ALL-IN ({need})");
                        break;
                    }
            }
        }
    }
  
}
