using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// Gerencia os 12 DataFiles colecionáveis da corrida (Lore).
/// DEV: persistProgress = false — os arquivos aparecem em TODA corrida.
/// BUILD: ligar persistProgress para gravar em PlayerPrefs e esconder já coletados.
/// Ao coletar mostra um cartão holográfico estilo Aurora (placa escura + acentos
/// ciano + ticks de progresso), com slide/fade — mesmo vocabulário visual do
/// PanelInteractMarker e do HUD de integridade.
public class DataFileManager : MonoBehaviour
{
    public static DataFileManager Instance { get; private set; }

    [Tooltip("DEV: false = aparece toda corrida. Ligar apenas na build final.")]
    public bool persistProgress = false;
    public int totalFiles = 12;
    public float counterSeconds = 3f;
    public Color counterColor = new Color(0.05f, 0.88f, 1f);

    private readonly HashSet<string> collectedThisRun = new HashSet<string>();
    private const string PrefsKey = "Aurora_DataFiles";

    // ------ cartão holográfico ------
    private CanvasGroup group;
    private RectTransform card;
    private TMP_Text titleLabel;
    private TMP_Text subtitleLabel;
    private TMP_Text countLabel;
    private Image[] progressTicks;
    private Image iconCore;

    private const float CardRestY = -128f;   // abaixo do HUD de integridade
    private float showTime = -1f;

    public int CollectedCount => collectedThisRun.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildCounterUI();
    }

    /// Chamado pelo DataFileCollectible no Start para respeitar a persistência.
    public bool WasCollectedBefore(string fileId)
    {
        if (!persistProgress) return false;
        string saved = PlayerPrefs.GetString(PrefsKey, "");
        return ("," + saved + ",").Contains("," + fileId + ",");
    }

    public void Collect(string fileId)
    {
        if (!collectedThisRun.Add(fileId)) return;

        if (persistProgress)
        {
            string saved = PlayerPrefs.GetString(PrefsKey, "");
            if (!("," + saved + ",").Contains("," + fileId + ","))
            {
                PlayerPrefs.SetString(PrefsKey, string.IsNullOrEmpty(saved) ? fileId : saved + "," + fileId);
                PlayerPrefs.Save();
            }
        }

        ShowCard(fileId);
        Debug.Log("[DataFile] coletado " + fileId + " (" + collectedThisRun.Count + "/" + totalFiles + ")");
    }

    private void ShowCard(string fileId)
    {
        if (card == null) return;

        titleLabel.text = "ARQUIVO DE DADOS RECUPERADO";
        subtitleLabel.text = "REGISTRO " + fileId.ToUpperInvariant() + " TRANSFERIDO AO BANCO DE LORE";
        countLabel.text = collectedThisRun.Count.ToString("00") + "/" + totalFiles.ToString("00");

        for (int i = 0; i < progressTicks.Length; i++)
        {
            bool filled = i < collectedThisRun.Count;
            progressTicks[i].color = filled ? counterColor : new Color(1f, 1f, 1f, 0.10f);
        }

        showTime = Time.time;
        group.alpha = 0f;
    }

    private void Update()
    {
        if (group == null || showTime < 0f) return;

        float t = Time.time - showTime;
        const float slideIn = 0.28f;
        const float fadeOut = 0.55f;
        float holdEnd = slideIn + counterSeconds;

        if (t < slideIn)
        {
            // entrada: desliza de cima com ease-out + fade
            float k = 1f - Mathf.Pow(1f - t / slideIn, 3f);
            group.alpha = k;
            card.anchoredPosition = new Vector2(0f, CardRestY + 34f * (1f - k));
            card.localScale = Vector3.one * (1.05f - 0.05f * k);
        }
        else if (t < holdEnd)
        {
            group.alpha = 1f;
            card.anchoredPosition = new Vector2(0f, CardRestY);
            card.localScale = Vector3.one;
            // pulso sutil no núcleo do ícone
            if (iconCore != null)
            {
                float p = 0.6f + 0.4f * Mathf.Sin(t * 7f);
                iconCore.color = Color.Lerp(counterColor, Color.white, 0.35f * p);
            }
        }
        else if (t < holdEnd + fadeOut)
        {
            float k = (t - holdEnd) / fadeOut;
            group.alpha = 1f - k;
            card.anchoredPosition = new Vector2(0f, CardRestY + 14f * k);
        }
        else
        {
            group.alpha = 0f;
            showTime = -1f;
        }
    }

    // ------------------------------------------------------------------
    //  Construção procedural do cartão (sem sprites: Images de cor sólida)
    // ------------------------------------------------------------------
    private void BuildCounterUI()
    {
        var canvasGo = new GameObject("DataFileCounter_Canvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        group = canvasGo.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        card = NewRect("Card", canvasGo.transform);
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 1f);
        card.pivot = new Vector2(0.5f, 1f);
        card.anchoredPosition = new Vector2(0f, CardRestY);
        card.sizeDelta = new Vector2(760f, 104f);

        // placa de fundo escura quase opaca (legível mesmo contra as luzes do teto)
        AddImage(card, new Color(0.004f, 0.032f, 0.052f, 0.96f));

        // barras de acento (topo forte, base sutil) — assinatura ciano do projeto
        var top = NewRect("AccentTop", card);
        Stretch(top, 0f, 1f, 1f, 1f); top.sizeDelta = new Vector2(0f, 3f); top.pivot = new Vector2(0.5f, 1f); top.anchoredPosition = Vector2.zero;
        AddImage(top, counterColor);
        var bottom = NewRect("AccentBottom", card);
        Stretch(bottom, 0f, 0f, 1f, 0f); bottom.sizeDelta = new Vector2(0f, 2f); bottom.pivot = new Vector2(0.5f, 0f); bottom.anchoredPosition = Vector2.zero;
        AddImage(bottom, new Color(counterColor.r, counterColor.g, counterColor.b, 0.35f));

        // ícone: losango ciano com núcleo pulsante (eco do hexágono dos painéis E)
        var icon = NewRect("Icon", card);
        icon.anchorMin = icon.anchorMax = new Vector2(0f, 0.5f);
        icon.pivot = new Vector2(0.5f, 0.5f);
        icon.anchoredPosition = new Vector2(56f, 4f);
        icon.sizeDelta = new Vector2(44f, 44f);
        icon.localRotation = Quaternion.Euler(0f, 0f, 45f);
        AddImage(icon, counterColor);
        var iconInner = NewRect("IconInner", icon);
        Stretch(iconInner, 0f, 0f, 1f, 1f); iconInner.offsetMin = new Vector2(5f, 5f); iconInner.offsetMax = new Vector2(-5f, -5f);
        AddImage(iconInner, new Color(0.004f, 0.032f, 0.052f, 1f));
        var core = NewRect("IconCore", iconInner);
        core.anchorMin = core.anchorMax = new Vector2(0.5f, 0.5f);
        core.pivot = new Vector2(0.5f, 0.5f);
        core.sizeDelta = new Vector2(12f, 12f);
        iconCore = AddImage(core, counterColor);

        // título
        titleLabel = NewText("Title", card, 22f, FontStyles.Bold, counterColor, TextAlignmentOptions.BottomLeft);
        var trt = titleLabel.rectTransform;
        trt.anchorMin = new Vector2(0f, 0.5f); trt.anchorMax = new Vector2(1f, 1f);
        trt.offsetMin = new Vector2(104f, 2f); trt.offsetMax = new Vector2(-150f, -10f);
        titleLabel.characterSpacing = 4f;
        titleLabel.enableWordWrapping = false;
        titleLabel.overflowMode = TextOverflowModes.Overflow;

        // subtítulo
        subtitleLabel = NewText("Subtitle", card, 14f, FontStyles.Normal, new Color(0.72f, 0.9f, 1f, 0.85f), TextAlignmentOptions.TopLeft);
        var srt = subtitleLabel.rectTransform;
        srt.anchorMin = new Vector2(0f, 0f); srt.anchorMax = new Vector2(1f, 0.5f);
        srt.offsetMin = new Vector2(104f, 26f); srt.offsetMax = new Vector2(-140f, -2f);
        subtitleLabel.characterSpacing = 2f;

        // contador grande à direita
        countLabel = NewText("Count", card, 34f, FontStyles.Bold, Color.white, TextAlignmentOptions.Right);
        var crt = countLabel.rectTransform;
        crt.anchorMin = new Vector2(1f, 0f); crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(1f, 0.5f);
        crt.offsetMin = new Vector2(-130f, 14f); crt.offsetMax = new Vector2(-22f, -14f);

        // ticks de progresso (1..totalFiles) na base do cartão
        progressTicks = new Image[Mathf.Max(1, totalFiles)];
        float tickW = 15f, gap = 4f;
        float startX = 104f;
        for (int i = 0; i < progressTicks.Length; i++)
        {
            var tick = NewRect("Tick_" + (i + 1), card);
            tick.anchorMin = tick.anchorMax = new Vector2(0f, 0f);
            tick.pivot = new Vector2(0f, 0f);
            tick.anchoredPosition = new Vector2(startX + i * (tickW + gap), 9f);
            tick.sizeDelta = new Vector2(tickW, 4f);
            progressTicks[i] = AddImage(tick, new Color(1f, 1f, 1f, 0.10f));
        }
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    private static Image AddImage(RectTransform rt, Color color)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static void Stretch(RectTransform rt, float minX, float minY, float maxX, float maxY)
    {
        rt.anchorMin = new Vector2(minX, minY);
        rt.anchorMax = new Vector2(maxX, maxY);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static TMP_Text NewText(string name, Transform parent, float size, FontStyles style, Color color, TextAlignmentOptions align)
    {
        var rt = NewRect(name, parent);
        var txt = rt.gameObject.AddComponent<TextMeshProUGUI>();
        txt.fontSize = size;
        txt.fontStyle = style;
        txt.color = color;
        txt.alignment = align;
        txt.raycastTarget = false;
        return txt;
    }
}
