using TMPro;
using UnityEngine;

/// Marcador persistente de interação "E" sobre um painel (Round 16c).
/// Sinaliza de longe que o painel aceita interação com E: hexágono ciano emissivo +
/// "E" grande, flutuando acima do painel, virado para a câmera e com pulso.
/// Some quando o painel já foi usado (oneShot) ou fora da gameplay livre.
/// Estilo idêntico ao indicador do tutorial (TutorialArrowIndicator).
public class PanelInteractMarker : MonoBehaviour
{
    public float markerHeight = 2.5f;
    public float pulseSpeed = 4f;
    public Color color = new Color(0.05f, 0.88f, 1f);
    [Tooltip("Se true, só aparece no estado Playing (fora do tutorial/cutscenes).")]
    public bool onlyDuringGameplay = true;

    private Transform marker;
    private TMP_Text label;
    private Material glowMat;
    private readonly System.Collections.Generic.List<Renderer> glowRenderers = new System.Collections.Generic.List<Renderer>();
    private InteractableObject interactable;
    private Vector3 anchorXZ;
    private bool built;

    private void Start()
    {
        interactable = GetComponent<InteractableObject>();

        // ancoragem em x/z do modelo do painel (ou do proprio objeto)
        Transform model = transform.Find("PainelLazer_Model");
        Transform glow = transform.Find("Screen_Glow");
        Vector3 src = model != null ? model.position : (glow != null ? glow.position : transform.position);
        anchorXZ = new Vector3(src.x, 0f, src.z);

        Build();
        SetVisible(false);
        built = true;
    }

    private void Build()
    {
        glowMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        glowMat.color = color;
        if (glowMat.HasProperty("_BaseColor")) glowMat.SetColor("_BaseColor", color);
        glowMat.EnableKeyword("_EMISSION");
        glowMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        glowMat.SetColor("_EmissionColor", color * 2.2f);

        var plate = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        plate.color = new Color(0.015f, 0.05f, 0.075f);

        marker = new GameObject("InteractMarker_E").transform;
        marker.SetParent(transform, false);

        // disco escuro (fundo do hexagono)
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "HexPlate"; Object.Destroy(disc.GetComponent<Collider>());
        disc.transform.SetParent(marker, false);
        disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        disc.transform.localPosition = new Vector3(0f, 0f, 0.06f);
        disc.transform.localScale = new Vector3(0.86f, 0.02f, 0.86f);
        SetMat(disc, plate);

        // moldura hexagonal (6 barras)
        for (int i = 0; i < 6; i++)
        {
            float a0 = (90f + i * 60f) * Mathf.Deg2Rad;
            float a1 = (90f + (i + 1) * 60f) * Mathf.Deg2Rad;
            Vector3 v0 = new Vector3(Mathf.Cos(a0) * 0.5f, Mathf.Sin(a0) * 0.5f, 0f);
            Vector3 v1 = new Vector3(Mathf.Cos(a1) * 0.5f, Mathf.Sin(a1) * 0.5f, 0f);
            Vector3 mid = (v0 + v1) * 0.5f;
            float len = Vector3.Distance(v0, v1);
            float ang = Mathf.Atan2(v1.y - v0.y, v1.x - v0.x) * Mathf.Rad2Deg;
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "HexEdge_" + i; Object.Destroy(bar.GetComponent<Collider>());
            bar.transform.SetParent(marker, false);
            bar.transform.localPosition = new Vector3(mid.x, mid.y, 0f);
            bar.transform.localRotation = Quaternion.Euler(0f, 0f, ang);
            bar.transform.localScale = new Vector3(len * 1.04f, 0.07f, 0.06f);
            SetMat(bar, glowMat);
            glowRenderers.Add(bar.GetComponent<Renderer>());
        }

        var txtGo = new GameObject("E");
        txtGo.transform.SetParent(marker, false);
        txtGo.transform.localPosition = new Vector3(0f, 0f, -0.05f);
        label = txtGo.AddComponent<TextMeshPro>();
        label.text = "E"; label.fontSize = 5.5f; label.alignment = TextAlignmentOptions.Center;
        label.color = color; label.rectTransform.sizeDelta = new Vector2(1f, 1f);
        label.fontStyle = FontStyles.Bold; label.outlineWidth = 0.22f;
        label.outlineColor = new Color32(0, 12, 18, 255);
        label.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private static void SetMat(GameObject go, Material m)
    {
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private void SetVisible(bool v)
    {
        if (marker != null && marker.gameObject.activeSelf != v)
        {
            marker.gameObject.SetActive(v);
        }
    }

    private void Update()
    {
        if (!built)
        {
            return;
        }

        // some se o painel ja foi usado, ou fora da gameplay livre
        bool available = interactable == null || interactable.CanInteractLegacy;
        bool stateOk = !onlyDuringGameplay || (GameManager.Instance != null && GameManager.Instance.State == GameState.Playing);
        bool show = available && stateOk;
        SetVisible(show);
        if (!show)
        {
            return;
        }

        marker.position = anchorXZ + Vector3.up * markerHeight;

        Camera cam = Camera.main;
        if (cam != null)
        {
            marker.rotation = Quaternion.LookRotation(cam.transform.forward);
        }

        float pulse = 0.55f + 0.45f * Mathf.Sin(Time.time * pulseSpeed);
        float s = 0.92f + 0.12f * pulse;
        marker.localScale = new Vector3(s, s, s);
        if (glowMat != null)
        {
            glowMat.SetColor("_EmissionColor", color * (1.4f + 2.2f * pulse));
        }
        if (label != null)
        {
            Color c = color; c.a = 0.7f + 0.3f * pulse;
            label.color = c;
        }
    }
}
