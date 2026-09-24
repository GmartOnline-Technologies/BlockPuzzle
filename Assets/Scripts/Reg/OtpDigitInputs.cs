using System.Text;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// One real input receives keyboard/paste/backspace; six existing inputs are display-only.
public class OtpDigitInputs : MonoBehaviour
{
    public TMP_InputField hiddenInput;
    public TMP_InputField[] digits = new TMP_InputField[6];
    public Color activeColor = new Color(1f, 0.8f, 0.25f, 1f);
    private bool initialized;
    private string previous = "";
    private Vector3[] scales;
    private Tween[] pops;
    private Outline[] outlines;
    public string Code { get { return hiddenInput != null ? Clean(hiddenInput.text) : ""; } }

    private bool Initialize()
    {
        if (initialized) return true;
        if (hiddenInput == null || digits == null || digits.Length != 6) return false;
        foreach (TMP_InputField digit in digits) if (digit == null || digit == hiddenInput) return false;
        initialized = true;
        scales = new Vector3[6]; pops = new Tween[6]; outlines = new Outline[6];
        hiddenInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        hiddenInput.keyboardType = TouchScreenKeyboardType.NumberPad;
        hiddenInput.characterLimit = 6;
        hiddenInput.customCaretColor = true;
        hiddenInput.caretColor = Color.clear;
        hiddenInput.selectionColor = Color.clear;
        if (hiddenInput.textComponent != null)
        {
            Color color = hiddenInput.textComponent.color; color.a = 0f;
            hiddenInput.textComponent.color = color;
            hiddenInput.textComponent.raycastTarget = false;
        }
        if (hiddenInput.placeholder != null) hiddenInput.placeholder.gameObject.SetActive(false);
        hiddenInput.transition = Selectable.Transition.None;
        if (hiddenInput.targetGraphic != null)
        {
            hiddenInput.targetGraphic.color = Color.clear;
            hiddenInput.targetGraphic.raycastTarget = true;
        }
        for (int i = 0; i < 6; i++)
        {
            TMP_InputField digit = digits[i];
            digit.transition = Selectable.Transition.None;
            digit.readOnly = true; digit.interactable = false;
            foreach (Graphic graphic in digit.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            scales[i] = digit.transform.localScale;
            if (digit.targetGraphic != null)
            {
                outlines[i] = digit.targetGraphic.GetComponent<Outline>();
                if (outlines[i] == null) outlines[i] = digit.targetGraphic.gameObject.AddComponent<Outline>();
                outlines[i].effectColor = activeColor; outlines[i].effectDistance = new Vector2(2f, -2f);
                outlines[i].enabled = false;
            }
        }
        hiddenInput.onValueChanged.AddListener(Changed);
        Render(Code, false);
        return true;
    }
    private void Awake()
    {
        if (!Initialize()) Debug.LogError("Assign Hidden Input and all six visible OTP input boxes (left to right).", this);
    }
    private void OnEnable() { if (Initialize()) Render(Code, false); }
    private void Update()
    {
        if (!initialized) return;
        int next = Code.Length;
        for (int i = 0; i < 6; i++)
            if (outlines[i] != null) outlines[i].enabled = hiddenInput.isFocused && hiddenInput.interactable && i == next;
    }
    private static string Clean(string text)
    {
        StringBuilder code = new StringBuilder(6);
        foreach (char c in text ?? "")
        { if (c >= '0' && c <= '9' && code.Length < 6) code.Append(c); }
        return code.ToString();
    }
    private void Changed(string value)
    {
        string code = Clean(value);
        if (code != value) hiddenInput.SetTextWithoutNotify(code);
        Render(code, true);
    }
    private void Render(string code, bool animate)
    {
        for (int i = 0; i < 6; i++)
        {
            string value = i < code.Length ? code[i].ToString() : "";
            digits[i].SetTextWithoutNotify(value);
            if (pops[i] != null) pops[i].Kill();
            digits[i].transform.localScale = scales[i];
            if (animate && value.Length > 0 && (i >= previous.Length || previous[i] != code[i]))
                pops[i] = digits[i].transform.DOPunchScale(scales[i] * 0.10f, 0.22f, 3, 0.4f).SetUpdate(true);
        }
        previous = code;
    }
    public void Focus()
    {
        if (!Initialize() || !hiddenInput.interactable) return;
        hiddenInput.Select(); hiddenInput.ActivateInputField();
    }
    public void Clear()
    {
        if (hiddenInput != null) hiddenInput.SetTextWithoutNotify("");
        if (Initialize()) Render("", false);
    }
    public void SetInteractable(bool value)
    { if (hiddenInput != null) hiddenInput.interactable = value; }
    private void OnDisable()
    {
        if (!initialized) return;
        for (int i = 0; i < 6; i++)
        {
            if (pops[i] != null) pops[i].Kill();
            if (digits[i] != null) digits[i].transform.localScale = scales[i];
            if (outlines[i] != null) outlines[i].enabled = false;
        }
    }
    private void OnDestroy()
    {
        if (hiddenInput != null) hiddenInput.onValueChanged.RemoveListener(Changed);
        if (pops != null) foreach (Tween pop in pops) if (pop != null) pop.Kill();
    }
}
