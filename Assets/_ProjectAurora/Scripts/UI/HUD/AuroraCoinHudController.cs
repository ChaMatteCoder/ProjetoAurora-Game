using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class AuroraCoinHudController : MonoBehaviour
{
    [SerializeField] private TMP_Text balanceText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private RectTransform pulseTarget;
    [SerializeField] private Image iconGlow;
    [SerializeField, Range(0.15f, 0.3f)] private float pulseDuration = 0.22f;
    [SerializeField, Range(1f, 1.15f)] private float pulseScale = 1.08f;
    [SerializeField, Range(0.5f, 3f)] private float statusDuration = 1.6f;
    [SerializeField, Range(2f, 10f)] private float limitMessageCooldown = 5f;

    private AuroraCoinWallet wallet;
    private Coroutine pulseRoutine;
    private Coroutine statusRoutine;
    private Vector3 baseScale = Vector3.one;
    private Color baseGlowColor = Color.white;
    private bool firstCoinMessageShown;
    private float lastLimitMessageAt = -100f;

    public string DisplayedBalance => balanceText == null ? string.Empty : balanceText.text;

    private void Awake()
    {
        if (pulseTarget == null)
        {
            pulseTarget = transform as RectTransform;
        }

        if (pulseTarget != null)
        {
            baseScale = pulseTarget.localScale;
        }

        if (iconGlow != null)
        {
            baseGlowColor = iconGlow.color;
        }
    }

    private void OnEnable()
    {
        BindWallet();
    }

    private void Start()
    {
        if (wallet == null)
        {
            BindWallet();
        }
    }

    private void OnDisable()
    {
        UnbindWallet();
        if (pulseTarget != null)
        {
            pulseTarget.localScale = baseScale;
        }

        if (statusText != null)
        {
            statusText.text = string.Empty;
        }
    }

    public void Configure(TMP_Text targetBalance, TMP_Text targetStatus, RectTransform targetPulse, Image targetGlow)
    {
        balanceText = targetBalance;
        statusText = targetStatus;
        pulseTarget = targetPulse;
        iconGlow = targetGlow;
    }

    private void BindWallet()
    {
        AuroraCoinWallet target = AuroraCoinWallet.Instance;
        if (target == null || target == wallet)
        {
            return;
        }

        UnbindWallet();
        wallet = target;
        wallet.OnBalanceChanged += HandleBalanceChanged;
        wallet.OnCoinsAdded += HandleCoinsAdded;
        wallet.OnBalanceLimitReached += HandleBalanceLimitReached;
        HandleBalanceChanged(wallet.Balance);
    }

    private void UnbindWallet()
    {
        if (wallet == null)
        {
            return;
        }

        wallet.OnBalanceChanged -= HandleBalanceChanged;
        wallet.OnCoinsAdded -= HandleCoinsAdded;
        wallet.OnBalanceLimitReached -= HandleBalanceLimitReached;
        wallet = null;
    }

    private void HandleBalanceChanged(int value)
    {
        if (balanceText != null)
        {
            balanceText.text = Mathf.Clamp(value, 0, AuroraCoinWallet.MaxBalance).ToString("000");
        }
    }

    private void HandleCoinsAdded(int amount, int newBalance)
    {
        if (amount <= 0)
        {
            return;
        }

        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
        }

        pulseRoutine = StartCoroutine(Pulse());
        if (!firstCoinMessageShown)
        {
            firstCoinMessageShown = true;
            ShowStatus("AURORACOIN ADQUIRIDA");
        }
    }

    private void HandleBalanceLimitReached()
    {
        if (Time.unscaledTime - lastLimitMessageAt < limitMessageCooldown)
        {
            return;
        }

        lastLimitMessageAt = Time.unscaledTime;
        ShowStatus("LIMITE DE AURORACOINS ATINGIDO");
    }

    private void ShowStatus(string message)
    {
        if (statusText == null)
        {
            return;
        }

        if (statusRoutine != null)
        {
            StopCoroutine(statusRoutine);
        }

        statusRoutine = StartCoroutine(ShowStatusTemporarily(message));
    }

    private IEnumerator Pulse()
    {
        if (pulseTarget == null)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < pulseDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / pulseDuration);
            float wave = Mathf.Sin(normalized * Mathf.PI);
            pulseTarget.localScale = baseScale * Mathf.Lerp(1f, pulseScale, wave);
            if (iconGlow != null)
            {
                iconGlow.color = Color.Lerp(baseGlowColor, Color.white, wave * 0.7f);
            }

            yield return null;
        }

        pulseTarget.localScale = baseScale;
        if (iconGlow != null)
        {
            iconGlow.color = baseGlowColor;
        }

        pulseRoutine = null;
    }

    private IEnumerator ShowStatusTemporarily(string message)
    {
        statusText.text = message;
        float elapsed = 0f;
        while (elapsed < statusDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        statusText.text = string.Empty;
        statusRoutine = null;
    }
}
