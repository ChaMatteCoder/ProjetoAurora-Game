using UnityEngine;

/// Tela decorativa (Tela_LP) do corredor do Núcleo: flutua suavemente e, quando
/// o Dr. Elias se aproxima, acende — a emissão sobe e uma luz é ativada.
public class ProximityScreen : MonoBehaviour
{
    [Header("Flutuação")]
    public float floatAmplitude = 0.14f;
    public float floatSpeed = 0.9f;
    public float swayDegrees = 3f;

    [Header("Acender por proximidade")]
    public float glowRange = 13f;
    public Color glowColor = new Color(0.35f, 0.9f, 1f);
    public float glowLightIntensity = 3.2f;
    public float emissionBoost = 3.5f;

    private Transform player;
    private Material mat;
    private Light glowLight;
    private Vector3 basePos;
    private Quaternion baseRot;
    private float phase;
    private Color baseColorOrig = Color.white;
    private Color emissionOrig = Color.black;
    private bool hasMat;

    private void Awake()
    {
        basePos = transform.localPosition;
        baseRot = transform.localRotation;
        phase = transform.position.z * 0.4f;

        var rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            mat = rend.material; // instância só desta tela
            hasMat = true;
            if (mat.HasProperty("_BaseColor")) baseColorOrig = mat.GetColor("_BaseColor");
            if (mat.HasProperty("_EmissionColor")) emissionOrig = mat.GetColor("_EmissionColor");
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        var lgo = new GameObject("ScreenGlow");
        lgo.transform.SetParent(transform, false);
        lgo.transform.localPosition = Vector3.up * 1.6f;
        glowLight = lgo.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.color = glowColor;
        glowLight.range = 4.5f;
        glowLight.intensity = 0f;
        glowLight.shadows = LightShadows.None;
    }

    private void Update()
    {
        float s = Mathf.Sin(phase + Time.time * floatSpeed * 2f * Mathf.PI);
        transform.localPosition = basePos + Vector3.up * (floatAmplitude * s);
        transform.localRotation = baseRot * Quaternion.Euler(0f, swayDegrees * 0.5f * s, 0f);

        if (player == null)
        {
            var go = GameObject.Find("Dr. Elias - Player");
            if (go == null) return;
            player = go.transform;
        }

        float d = Vector3.Distance(player.position, transform.position);
        float t = 1f - Mathf.Clamp01(d / Mathf.Max(0.5f, glowRange));
        t = t * t;

        if (hasMat)
        {
            mat.SetColor("_BaseColor", Color.Lerp(baseColorOrig, glowColor, t * 0.7f));
            mat.SetColor("_EmissionColor", Color.Lerp(emissionOrig, glowColor * emissionBoost, t));
        }
        if (glowLight != null) glowLight.intensity = t * glowLightIntensity;
    }

    private void OnDestroy()
    {
        if (mat != null) Destroy(mat);
    }
}
