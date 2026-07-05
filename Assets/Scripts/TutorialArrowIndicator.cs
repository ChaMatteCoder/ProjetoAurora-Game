using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// Setas animadas do tutorial (Round 11). Indicadores semi-diegeticos no mundo,
/// proximos da faixa/obstaculo correto: chevrons ciano emissivos com pulso + deslize
/// na direcao da acao, e etiquetas de tecla ("ESPACO"/"E") viradas para a camera.
/// Aparece apenas quando a acao da etapa e liberada; some quando executada.
public class TutorialArrowIndicator : MonoBehaviour
{
    public Color arrowColor = new Color(0.05f, 0.88f, 1f);
    public float pulseSpeed = 5f;
    public float slideDistance = 0.55f;
    public float slideSpeed = 2.2f;

    private readonly List<Transform> chevrons = new List<Transform>();
    private Transform chevronGroup;
    private TMP_Text keyLabel;
    private Material arrowMaterial;
    private Vector3 slideDirection = Vector3.right;
    private Vector3 baseGroupPosition;
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
        arrowMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        arrowMaterial.color = arrowColor;
        if (arrowMaterial.HasProperty("_BaseColor"))
        {
            arrowMaterial.SetColor("_BaseColor", arrowColor);
        }
        arrowMaterial.EnableKeyword("_EMISSION");
        arrowMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        arrowMaterial.SetColor("_EmissionColor", arrowColor * 2.2f);

        chevronGroup = new GameObject("Chevrons").transform;
        chevronGroup.SetParent(transform, false);
        for (int i = 0; i < 3; i++)
        {
            chevrons.Add(BuildChevron(chevronGroup, i * 0.62f));
        }

        var labelGo = new GameObject("KeyLabel");
        labelGo.transform.SetParent(transform, false);
        keyLabel = labelGo.AddComponent<TextMeshPro>();
        keyLabel.fontSize = 7f;
        keyLabel.alignment = TextAlignmentOptions.Center;
        keyLabel.color = arrowColor;
        keyLabel.rectTransform.sizeDelta = new Vector2(4f, 1.2f);
        keyLabel.gameObject.SetActive(false);

        gameObject.SetActive(false);
        visible = false;
    }

    /// Duas laminas formando um ">" apontando +X local.
    private Transform BuildChevron(Transform parent, float offsetX)
    {
        var root = new GameObject("Chevron").transform;
        root.SetParent(parent, false);
        root.localPosition = new Vector3(offsetX, 0f, 0f);

        CreateBlade(root, new Vector3(0f, 0.16f, 0f), 35f);
        CreateBlade(root, new Vector3(0f, -0.16f, 0f), -35f);
        return root;
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

    public void ShowLane(Vector3 worldPosition, int direction)
    {
        slideDirection = direction >= 0 ? Vector3.right : Vector3.left;
        chevronGroup.localRotation = direction >= 0
            ? Quaternion.identity
            : Quaternion.Euler(0f, 180f, 0f);
        ShowInternal(worldPosition, null);
    }

    public void ShowJump(Vector3 worldPosition)
    {
        slideDirection = Vector3.up;
        chevronGroup.localRotation = Quaternion.Euler(0f, 0f, 90f);
        ShowInternal(worldPosition, "ESPAÇO");
    }

    public void ShowInteract(Vector3 worldPosition)
    {
        slideDirection = Vector3.zero;
        chevronGroup.localRotation = Quaternion.identity;
        ShowInternal(worldPosition, "E");
        // interacao: sem chevrons, so a tecla pulsando sobre o painel
        chevronGroup.gameObject.SetActive(false);
    }

    private void ShowInternal(Vector3 worldPosition, string key)
    {
        transform.position = worldPosition;
        baseGroupPosition = Vector3.zero;
        chevronGroup.gameObject.SetActive(true);
        chevronGroup.localPosition = baseGroupPosition;

        if (keyLabel != null)
        {
            bool hasKey = !string.IsNullOrEmpty(key);
            keyLabel.gameObject.SetActive(hasKey);
            if (hasKey)
            {
                keyLabel.text = key;
                keyLabel.transform.localPosition = slideDirection == Vector3.up
                    ? new Vector3(0f, 1.05f, 0f)
                    : new Vector3(0f, 0.8f, 0f);
            }
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

    private void Update()
    {
        if (!visible)
        {
            return;
        }

        float pulse = 0.55f + 0.45f * Mathf.Sin(Time.time * pulseSpeed);
        if (arrowMaterial != null)
        {
            arrowMaterial.SetColor("_EmissionColor", arrowColor * (1.2f + 2.2f * pulse));
        }
        if (keyLabel != null && keyLabel.gameObject.activeSelf)
        {
            Color c = arrowColor;
            c.a = 0.55f + 0.45f * pulse;
            keyLabel.color = c;

            Camera cam = Camera.main;
            if (cam != null)
            {
                keyLabel.transform.rotation = Quaternion.LookRotation(cam.transform.forward);
            }
        }

        // deslize em loop na direcao da acao (parado para "E")
        if (slideDirection != Vector3.zero && chevronGroup.gameObject.activeSelf)
        {
            float t = Mathf.Repeat(Time.time * slideSpeed, 1f);
            chevronGroup.localPosition = baseGroupPosition + slideDirection * (t * slideDistance);
        }
    }
}
