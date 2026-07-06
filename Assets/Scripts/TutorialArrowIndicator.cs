using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// Indicadores de acao do tutorial (Round 11, revisado Round 14).
/// Semi-diegeticos no mundo: chevrons ciano emissivos com pulso + deslize na direcao
/// correta, etiqueta "ESPACO" legivel (placa escura + contorno) acima da seta, e um
/// indicador "E" cinematografico (moldura hexagonal + pulso) para interacao.
/// Aparece apenas quando a acao da etapa e liberada; some quando executada.
public class TutorialArrowIndicator : MonoBehaviour
{
    public Color arrowColor = new Color(0.05f, 0.88f, 1f);
    public float pulseSpeed = 5f;
    public float slideDistance = 0.5f;
    public float slideSpeed = 2.2f;

    private Transform chevronGroup;
    private Material arrowMaterial;
    private Material plateMaterial;
    private Vector3 slideDirection = Vector3.right;

    // ESPACO
    private Transform spaceLabelRoot;
    private TMP_Text spaceLabelText;

    // E (interacao)
    private Transform interactRoot;
    private TMP_Text interactLabel;
    private readonly List<Renderer> interactGlowRenderers = new List<Renderer>();

    private bool visible;

    public static TutorialArrowIndicator GetOrCreate()
    {
        TutorialArrowIndicator existing = FindFirstObjectByType<TutorialArrowIndicator>(FindObjectsInactive.Include);
        if (existing != null)
        {
            return existing;
        }

        var go = new GameObject("Tutorial_ArrowIndicator");
        return go.AddComponent<TutorialArrowIndicator>();
    }

    private void Awake()
    {
        arrowMaterial = MakeEmissive(arrowColor, 2.2f);
        plateMaterial = MakeSolid(new Color(0.015f, 0.05f, 0.075f));

        BuildChevrons();
        BuildSpaceLabel();
        BuildInteractHex();

        gameObject.SetActive(false);
        visible = false;
    }

    // ===================== construcao =====================

    private Material MakeEmissive(Color c, float intensity)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        m.SetColor("_EmissionColor", c * intensity);
        return m;
    }

    private Material MakeSolid(Color c)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
        return m;
    }

    private void BuildChevrons()
    {
        chevronGroup = new GameObject("Chevrons").transform;
        chevronGroup.SetParent(transform, false);
        for (int i = 0; i < 3; i++)
        {
            BuildChevron(chevronGroup, i * 0.55f);
        }
    }

    /// Duas laminas formando um ">" cuja PONTA fica em +X local (Round 14: sinais de tilt
    /// corrigidos — antes a ponta apontava para -X, invertendo as setas).
    private void BuildChevron(Transform parent, float offsetX)
    {
        var root = new GameObject("Chevron").transform;
        root.SetParent(parent, false);
        root.localPosition = new Vector3(offsetX, 0f, 0f);
        CreateBlade(root, new Vector3(0f, 0.16f, 0f), -35f);
        CreateBlade(root, new Vector3(0f, -0.16f, 0f), 35f);
    }

    private void CreateBlade(Transform parent, Vector3 localPos, float tiltZ)
    {
        GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blade.name = "Blade";
        Object.Destroy(blade.GetComponent<Collider>());
        blade.transform.SetParent(parent, false);
        blade.transform.localPosition = localPos;
        blade.transform.localRotation = Quaternion.Euler(0f, 0f, tiltZ);
        blade.transform.localScale = new Vector3(0.5f, 0.09f, 0.06f);
        blade.GetComponent<Renderer>().sharedMaterial = arrowMaterial;
    }

    private void BuildSpaceLabel()
    {
        spaceLabelRoot = new GameObject("SpaceLabel").transform;
        spaceLabelRoot.SetParent(transform, false);

        // moldura ciano (atras), placa escura (meio), texto (frente)
        CreatePlate(spaceLabelRoot, "Border", new Vector3(0f, 0f, 0.02f), new Vector3(2.5f, 0.92f, 1f), arrowMaterial);
        CreatePlate(spaceLabelRoot, "Plate", new Vector3(0f, 0f, 0.0f), new Vector3(2.36f, 0.78f, 1f), plateMaterial);

        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(spaceLabelRoot, false);
        txtGo.transform.localPosition = new Vector3(0f, 0f, -0.02f);
        spaceLabelText = txtGo.AddComponent<TextMeshPro>();
        spaceLabelText.text = "ESPAÇO";
        spaceLabelText.fontSize = 4.4f;
        spaceLabelText.alignment = TextAlignmentOptions.Center;
        spaceLabelText.color = arrowColor;
        spaceLabelText.rectTransform.sizeDelta = new Vector2(2.3f, 0.75f);
        spaceLabelText.outlineWidth = 0.22f;
        spaceLabelText.outlineColor = new Color32(0, 12, 18, 255);
        spaceLabelText.fontStyle = FontStyles.Bold;

        spaceLabelRoot.gameObject.SetActive(false);
    }

    private void BuildInteractHex()
    {
        interactRoot = new GameObject("InteractHex").transform;
        interactRoot.SetParent(transform, false);

        // placa escura hexagonal (disco chato escuro) + moldura de 6 barras ciano
        CreateDarkDisc(interactRoot, 0.62f);
        BuildHexRing(interactRoot, 0.7f);

        var txtGo = new GameObject("E");
        txtGo.transform.SetParent(interactRoot, false);
        txtGo.transform.localPosition = new Vector3(0f, 0f, -0.05f);
        interactLabel = txtGo.AddComponent<TextMeshPro>();
        interactLabel.text = "E";
        interactLabel.fontSize = 8f;
        interactLabel.alignment = TextAlignmentOptions.Center;
        interactLabel.color = arrowColor;
        interactLabel.rectTransform.sizeDelta = new Vector2(1.4f, 1.4f);
        interactLabel.outlineWidth = 0.22f;
        interactLabel.outlineColor = new Color32(0, 12, 18, 255);
        interactLabel.fontStyle = FontStyles.Bold;

        interactRoot.gameObject.SetActive(false);
    }

    private void CreateDarkDisc(Transform parent, float radius)
    {
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "HexPlate";
        Object.Destroy(disc.GetComponent<Collider>());
        disc.transform.SetParent(parent, false);
        disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // face -Z (para a camera apos billboard)
        disc.transform.localPosition = new Vector3(0f, 0f, 0.06f);
        disc.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);
        disc.GetComponent<Renderer>().sharedMaterial = plateMaterial;
    }

    private void BuildHexRing(Transform parent, float radius)
    {
        // hexagono pointy-top: vertices a cada 60 graus a partir de 90
        for (int i = 0; i < 6; i++)
        {
            float a0 = (90f + i * 60f) * Mathf.Deg2Rad;
            float a1 = (90f + (i + 1) * 60f) * Mathf.Deg2Rad;
            Vector3 v0 = new Vector3(Mathf.Cos(a0) * radius, Mathf.Sin(a0) * radius, 0f);
            Vector3 v1 = new Vector3(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius, 0f);
            Vector3 mid = (v0 + v1) * 0.5f;
            float len = Vector3.Distance(v0, v1);
            float ang = Mathf.Atan2(v1.y - v0.y, v1.x - v0.x) * Mathf.Rad2Deg;

            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "HexEdge_" + i;
            Object.Destroy(bar.GetComponent<Collider>());
            bar.transform.SetParent(parent, false);
            bar.transform.localPosition = new Vector3(mid.x, mid.y, 0f);
            bar.transform.localRotation = Quaternion.Euler(0f, 0f, ang);
            bar.transform.localScale = new Vector3(len * 1.04f, 0.1f, 0.08f);
            bar.GetComponent<Renderer>().sharedMaterial = arrowMaterial;
            interactGlowRenderers.Add(bar.GetComponent<Renderer>());
        }
    }

    private void CreatePlate(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Quad);
        plate.name = name;
        Object.Destroy(plate.GetComponent<Collider>());
        plate.transform.SetParent(parent, false);
        plate.transform.localPosition = localPos;
        plate.transform.localScale = scale;
        plate.GetComponent<Renderer>().sharedMaterial = mat;
    }

    // ===================== API publica =====================

    public void ShowLane(Vector3 worldPosition, int direction)
    {
        // ponta do chevron em +X: direita = identidade (ponta +X), esquerda = 180 (ponta -X)
        slideDirection = direction >= 0 ? Vector3.right : Vector3.left;
        chevronGroup.localRotation = direction >= 0 ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
        Present(worldPosition, chevrons: true, space: false, interact: false);
    }

    public void ShowJump(Vector3 worldPosition)
    {
        slideDirection = Vector3.up;
        chevronGroup.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Present(worldPosition, chevrons: true, space: true, interact: false);
    }

    public void ShowInteract(Vector3 worldPosition)
    {
        slideDirection = Vector3.zero;
        Present(worldPosition, chevrons: false, space: false, interact: true);
    }

    private void Present(Vector3 worldPosition, bool chevrons, bool space, bool interact)
    {
        transform.position = worldPosition;
        chevronGroup.localPosition = Vector3.zero;
        chevronGroup.gameObject.SetActive(chevrons);

        if (spaceLabelRoot != null)
        {
            spaceLabelRoot.gameObject.SetActive(space);
            // ESPACO sempre ACIMA das setas (chevrons de pulo sobem ~1.8 no eixo Y)
            spaceLabelRoot.localPosition = new Vector3(0f, 2.35f, 0f);
        }
        if (interactRoot != null)
        {
            interactRoot.gameObject.SetActive(interact);
            interactRoot.localScale = Vector3.one;
        }

        gameObject.SetActive(true);
        visible = true;
    }

    public void Hide()
    {
        visible = false;
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    // ===================== animacao =====================

    private void Update()
    {
        if (!visible)
        {
            return;
        }

        float pulse = 0.55f + 0.45f * Mathf.Sin(Time.time * pulseSpeed);
        Camera cam = Camera.main;

        if (chevronGroup.gameObject.activeSelf)
        {
            arrowMaterial.SetColor("_EmissionColor", arrowColor * (1.2f + 2.2f * pulse));
            if (slideDirection != Vector3.zero)
            {
                float t = Mathf.Repeat(Time.time * slideSpeed, 1f);
                chevronGroup.localPosition = slideDirection * (t * slideDistance);
            }
        }

        if (spaceLabelRoot != null && spaceLabelRoot.gameObject.activeSelf && cam != null)
        {
            spaceLabelRoot.rotation = Quaternion.LookRotation(cam.transform.forward);
        }

        if (interactRoot != null && interactRoot.gameObject.activeSelf)
        {
            // pulso de escala (chamada) + billboard + brilho da moldura
            float s = 0.92f + 0.12f * pulse;
            interactRoot.localScale = new Vector3(s, s, s);
            if (cam != null)
            {
                interactRoot.rotation = Quaternion.LookRotation(cam.transform.forward);
            }
            Color glow = arrowColor * (1.4f + 2.4f * pulse);
            for (int i = 0; i < interactGlowRenderers.Count; i++)
            {
                if (interactGlowRenderers[i] != null)
                {
                    interactGlowRenderers[i].material.SetColor("_EmissionColor", glow);
                }
            }
            if (interactLabel != null)
            {
                Color c = arrowColor;
                c.a = 0.7f + 0.3f * pulse;
                interactLabel.color = c;
            }
        }
    }
}
