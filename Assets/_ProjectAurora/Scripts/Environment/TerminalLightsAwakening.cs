using System.Collections.Generic;
using UnityEngine;

/// Entrada atmosférica do Terminal Central (Round 16).
/// Todas as luzes principais (emissivos de parede/teto/chão + Lights) começam APAGADAS —
/// obstáculos continuam visíveis (não pertencem a estes grupos). Conforme o Dr. Elias
/// avança, bancos de luz acendem em sequência com flicker de ignição, como se ele
/// chegasse a um lugar importante.
/// Implementação por MaterialPropertyBlock: NUNCA altera os materiais compartilhados.
public class TerminalLightsAwakening : MonoBehaviour
{
    [Tooltip("Faixa de z do jogador que dirige a ignição dos bancos.")]
    public float playerZStart = 2596f;
    public float playerZEnd = 2678f;
    public int bankCount = 6;
    public float igniteFlickerSeconds = 0.55f;
    [Tooltip("Nomes de grupos cujos emissivos participam do despertar.")]
    public string[] groupNames = { "Fase05 - Terminal Central", "Terminal_Rework_R15" };
    [Tooltip("Grupos ignorados (cutscene/gate da perseguição).")]
    public string[] excludeNames = { "CUTSCENE STAGING", "FinaleRedLights", "Cutscene Corruption Layer" };

    private class Piece
    {
        public Renderer renderer;
        public Color baseEmission;
        public Color baseColor;
        public int bank;
        public float igniteAt = -1f;
    }

    private class Lamp
    {
        public Light light;
        public float baseIntensity;
        public int bank;
        public float igniteAt = -1f;
    }

    private readonly List<Piece> pieces = new List<Piece>();
    private readonly List<Lamp> lamps = new List<Lamp>();
    private MaterialPropertyBlock mpb;
    private int banksLit;
    private Transform player;
    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private void Start()
    {
        mpb = new MaterialPropertyBlock();
        float zMin = float.MaxValue, zMax = float.MinValue;
        var collected = new List<Renderer>();

        foreach (string groupName in groupNames)
        {
            GameObject group = GameObject.Find(groupName);
            if (group == null)
            {
                continue;
            }

            foreach (Renderer r in group.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || r.sharedMaterial == null || IsExcluded(r.transform))
                {
                    continue;
                }
                string mn = r.sharedMaterial.name;
                if (mn.Contains("Emission") || mn.Contains("Holo") || mn.Contains("GlassCyan") || mn.Contains("Screen"))
                {
                    collected.Add(r);
                    zMin = Mathf.Min(zMin, r.transform.position.z);
                    zMax = Mathf.Max(zMax, r.transform.position.z);
                }
            }

            foreach (Light l in group.GetComponentsInChildren<Light>(true))
            {
                if (l == null || IsExcluded(l.transform))
                {
                    continue;
                }
                lamps.Add(new Lamp { light = l, baseIntensity = l.intensity });
            }
        }

        if (collected.Count == 0)
        {
            enabled = false;
            return;
        }

        float span = Mathf.Max(1f, zMax - zMin);
        foreach (Renderer r in collected)
        {
            var piece = new Piece
            {
                renderer = r,
                baseEmission = r.sharedMaterial.HasProperty(EmissionId) ? r.sharedMaterial.GetColor(EmissionId) : Color.black,
                baseColor = r.sharedMaterial.HasProperty(BaseColorId) ? r.sharedMaterial.GetColor(BaseColorId) : r.sharedMaterial.color,
                bank = Mathf.Clamp(Mathf.FloorToInt((r.transform.position.z - zMin) / span * bankCount), 0, bankCount - 1)
            };
            pieces.Add(piece);
            ApplyOff(piece);
        }
        foreach (Lamp lamp in lamps)
        {
            lamp.bank = Mathf.Clamp(Mathf.FloorToInt((lamp.light.transform.position.z - zMin) / span * bankCount), 0, bankCount - 1);
            lamp.light.intensity = 0f;
        }
    }

    private bool IsExcluded(Transform t)
    {
        Transform p = t;
        while (p != null)
        {
            foreach (string ex in excludeNames)
            {
                if (p.name.Contains(ex))
                {
                    return true;
                }
            }
            p = p.parent;
        }
        return false;
    }

    private void ApplyOff(Piece piece)
    {
        piece.renderer.GetPropertyBlock(mpb);
        mpb.SetColor(EmissionId, Color.black);
        // base escurecida (30%) — a peça segue visível, mas "desligada"
        Color dim = piece.baseColor * 0.3f;
        dim.a = piece.baseColor.a;
        mpb.SetColor(BaseColorId, dim);
        piece.renderer.SetPropertyBlock(mpb);
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.player == null)
        {
            return;
        }
        if (player == null)
        {
            player = gm.player.transform;
        }

        // progresso do jogador dispara bancos (sempre acende, nunca apaga)
        float progress = Mathf.Clamp01((player.position.z - playerZStart) / Mathf.Max(1f, playerZEnd - playerZStart));
        int targetBanks = Mathf.Clamp(Mathf.CeilToInt(progress * bankCount), 0, bankCount);
        if (targetBanks > banksLit)
        {
            float now = Time.time;
            for (int b = banksLit; b < targetBanks; b++)
            {
                float delay = (b - banksLit) * 0.12f;
                foreach (Piece piece in pieces)
                {
                    if (piece.bank == b && piece.igniteAt < 0f)
                    {
                        piece.igniteAt = now + delay;
                    }
                }
                foreach (Lamp lamp in lamps)
                {
                    if (lamp.bank == b && lamp.igniteAt < 0f)
                    {
                        lamp.igniteAt = now + delay;
                    }
                }
            }
            banksLit = targetBanks;
        }

        // anima ignicoes pendentes (flicker -> pleno)
        float time = Time.time;
        foreach (Piece piece in pieces)
        {
            if (piece.igniteAt < 0f || piece.renderer == null)
            {
                continue;
            }
            float t = (time - piece.igniteAt) / Mathf.Max(0.1f, igniteFlickerSeconds);
            if (t < 0f)
            {
                continue;
            }

            piece.renderer.GetPropertyBlock(mpb);
            if (t >= 1f)
            {
                mpb.SetColor(EmissionId, piece.baseEmission);
                mpb.SetColor(BaseColorId, piece.baseColor);
                piece.renderer.SetPropertyBlock(mpb);
                piece.igniteAt = float.NegativeInfinity; // concluida (nao reprocessa)
                continue;
            }
            // flicker: degraus pseudo-aleatorios subindo
            float step = Mathf.Floor(time * 24f);
            float noise = Mathf.PerlinNoise(step * 0.37f, piece.bank * 1.7f);
            float k = Mathf.Clamp01(t * (0.5f + noise));
            mpb.SetColor(EmissionId, piece.baseEmission * k);
            mpb.SetColor(BaseColorId, Color.Lerp(piece.baseColor * 0.3f, piece.baseColor, k));
            piece.renderer.SetPropertyBlock(mpb);
        }
        foreach (Lamp lamp in lamps)
        {
            if (lamp.igniteAt < 0f || lamp.light == null)
            {
                continue;
            }
            float t = (time - lamp.igniteAt) / Mathf.Max(0.1f, igniteFlickerSeconds);
            if (t < 0f)
            {
                continue;
            }
            if (t >= 1f)
            {
                lamp.light.intensity = lamp.baseIntensity;
                lamp.igniteAt = float.NegativeInfinity;
                continue;
            }
            float step = Mathf.Floor(time * 24f);
            float noise = Mathf.PerlinNoise(step * 0.41f, lamp.bank * 2.3f);
            lamp.light.intensity = lamp.baseIntensity * Mathf.Clamp01(t * (0.4f + noise));
        }
    }
}
