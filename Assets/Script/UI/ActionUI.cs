using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;   // UGUI
using TMPro;            // ⬅️ TMP
using Poker;

public class ActionUI : MonoBehaviour
{
    [Header("Buttons")]
    public Button btnFold, btnCheck, btnCall, btnRaise, btnAllIn;

    [Header("Raise Amount Input (둘 중 하나만 연결)")]
    public InputField inputRaise;        // UGUI용 (선택)
    public TMP_InputField tmpInputRaise; // TMP용  (선택)

    private TaskCompletionSource<PlayerAction> _tcs;

    void Awake()
    {
        if (btnFold)  btnFold.onClick.AddListener(()=> Resolve(ActionType.Fold, 0));
        if (btnCheck) btnCheck.onClick.AddListener(()=> Resolve(ActionType.Check, 0));
        if (btnCall)  btnCall.onClick.AddListener(()=> Resolve(ActionType.Call, 0));
        if (btnRaise) btnRaise.onClick.AddListener(()=>{
            int amt = ReadRaiseAmount();
            Resolve(ActionType.Raise, amt);
        });
        if (btnAllIn) btnAllIn.onClick.AddListener(()=> Resolve(ActionType.AllIn, 0));
    }

    int ReadRaiseAmount()
    {
        int amt = 0;
        if (tmpInputRaise) int.TryParse(tmpInputRaise.text, out amt);
        else if (inputRaise) int.TryParse(inputRaise.text, out amt);
        return amt; // 0이면 테이블 쪽에서 최소 레이즈로 보정
    }

    public Task<PlayerAction> WaitUserActionAsync()
    {
        _tcs = new TaskCompletionSource<PlayerAction>();
        return _tcs.Task;
    }

    void Resolve(ActionType type, int amount)
    {
        if (_tcs != null && !_tcs.Task.IsCompleted)
            _tcs.SetResult(new PlayerAction { seat = 0, type = type, amount = amount });
    }

    // (선택) 합법 액션만 보이게
    public void EnableLegalButtons(ActionType[] legal)
    {
        bool fold=false, check=false, call=false, raise=false, allin=false;
        if (legal != null) foreach (var a in legal)
        {
            if (a==ActionType.Fold) fold=true;
            else if (a==ActionType.Check) check=true;
            else if (a==ActionType.Call) call=true;
            else if (a==ActionType.Raise) raise=true;
            else if (a==ActionType.AllIn) allin=true;
        }
        if (btnFold)  btnFold.gameObject.SetActive(fold);
        if (btnCheck) btnCheck.gameObject.SetActive(check);
        if (btnCall)  btnCall.gameObject.SetActive(call);
        if (btnRaise) btnRaise.gameObject.SetActive(raise);
        if (btnAllIn) btnAllIn.gameObject.SetActive(allin);

        if (tmpInputRaise) tmpInputRaise.gameObject.SetActive(raise);
        if (inputRaise)    inputRaise.gameObject.SetActive(raise);
    }
}
