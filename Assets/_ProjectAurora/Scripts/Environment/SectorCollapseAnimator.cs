using System.Collections.Generic;
using UnityEngine;

/// Anima o colapso ambiental do Setor E / Ponte Técnica (Round 15).
/// Puramente VISUAL: balança cabos, faz placas cederem, tremor leve nas estruturas e
/// destroços caindo nas laterais/fundo (fora das faixas jogáveis — sem colisão/dano).
/// Coroutine-free (senoides no Update), sem Rigidbody, leve.
public class SectorCollapseAnimator : MonoBehaviour
{
    [Header("Auto-coleta por prefixo de nome (nos filhos)")]
    public string cablePrefix = "HangingCable";
    public string platePrefix = "CorruptPlate";

    [Header("Cabos (balanço)")]
    public float cableSwayDegrees = 7f;
    public float cableSwaySpeed = 1.6f;

    [Header("Placas (cedendo)")]
    public float plateSagDegrees = 9f;
    public float plateSagSpeed = 0.8f;

    [Header("Tremor geral")]
    public float trembleAmplitude = 0.015f;
    public float trembleSpeed = 22f;

    [Header("Destroços caindo (laterais, visual)")]
    public bool spawnFallingDebris = true;
    public int debrisCount = 8;
    public float debrisMinX = 8.5f;
    public float debrisMaxX = 13f;
    public float debrisTopY = 9f;
    public float debrisFloorY = 0.2f;
    public float debrisFallSpeed = 6f;
    public float debrisZStart = 1830f;
    public float debrisZEnd = 2230f;
    public Material debrisMaterial;

    private class Swayer { public Transform t; public Quaternion baseRot; public float phase; }
    private readonly List<Swayer> cables = new List<Swayer>();
    private readonly List<Swayer> plates = new List<Swayer>();

    private class Debris { public Transform t; public float speed; public float spin; }
    private readonly List<Debris> debris = new List<Debris>();
    private int seed;

    private void Awake()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child == transform)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(cablePrefix) && child.name.StartsWith(cablePrefix))
            {
                cables.Add(new Swayer { t = child, baseRot = child.localRotation, phase = cables.Count * 0.7f });
            }
            else if (!string.IsNullOrEmpty(platePrefix) && child.name.StartsWith(platePrefix))
            {
                plates.Add(new Swayer { t = child, baseRot = child.localRotation, phase = plates.Count * 1.1f });
            }
        }

        if (spawnFallingDebris)
        {
            BuildDebris();
        }
    }

    private void BuildDebris()
    {
        var mat = debrisMaterial;
        if (mat == null)
        {
            // fallback: material escuro simples
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.12f, 0.13f, 0.15f);
        }

        var host = new GameObject("FallingDebris").transform;
        host.SetParent(transform, false);

        for (int i = 0; i < Mathf.Max(0, debrisCount); i++)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Debris_" + i;
            Object.Destroy(go.GetComponent<Collider>()); // visual apenas — nunca colide com o player
            go.transform.SetParent(host, false);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            float s = PseudoRandom(i * 3 + 1) * 0.5f + 0.35f;
            go.transform.localScale = new Vector3(s, s * (PseudoRandom(i * 3 + 2) + 0.6f), s);
            var d = new Debris { t = go.transform, speed = debrisFallSpeed * (0.7f + PseudoRandom(i) * 0.9f), spin = (PseudoRandom(i * 2) - 0.5f) * 180f };
            ResetDebris(d, i, true);
            debris.Add(d);
        }
    }

    private void ResetDebris(Debris d, int i, bool randomizeY)
    {
        float side = (PseudoRandom(seed + i * 7 + 3) > 0.5f) ? 1f : -1f;
        float x = side * Mathf.Lerp(debrisMinX, debrisMaxX, PseudoRandom(seed + i * 5 + 1));
        float z = Mathf.Lerp(debrisZStart, debrisZEnd, PseudoRandom(seed + i * 11 + 2));
        float y = randomizeY ? Mathf.Lerp(debrisFloorY, debrisTopY, PseudoRandom(seed + i * 13 + 4)) : debrisTopY;
        d.t.position = new Vector3(x, y, z);
        seed++;
    }

    private void Update()
    {
        float time = Time.time;

        // cabos: balanço em Z (pêndulo)
        for (int i = 0; i < cables.Count; i++)
        {
            Swayer c = cables[i];
            if (c.t == null) continue;
            float ang = Mathf.Sin(time * cableSwaySpeed + c.phase) * cableSwayDegrees;
            c.t.localRotation = c.baseRot * Quaternion.Euler(0f, 0f, ang);
        }

        // placas: cedem (tilt lento) + micro-tremor
        for (int i = 0; i < plates.Count; i++)
        {
            Swayer p = plates[i];
            if (p.t == null) continue;
            float sag = (Mathf.Sin(time * plateSagSpeed + p.phase) * 0.5f + 0.5f) * plateSagDegrees;
            float tremble = Mathf.Sin(time * trembleSpeed + p.phase * 3f) * 1.5f;
            p.t.localRotation = p.baseRot * Quaternion.Euler(sag, 0f, tremble);
        }

        // destroços caindo (loop)
        for (int i = 0; i < debris.Count; i++)
        {
            Debris d = debris[i];
            if (d.t == null) continue;
            Vector3 pos = d.t.position;
            pos.y -= d.speed * Time.deltaTime;
            d.t.position = pos;
            d.t.Rotate(d.spin * Time.deltaTime, d.spin * 0.5f * Time.deltaTime, 0f, Space.Self);
            if (pos.y <= debrisFloorY)
            {
                ResetDebris(d, i, false);
            }
        }
    }

    // ruido deterministico [0,1) (evita Random.* proibido no editor codegen e mantem estabilidade)
    private static float PseudoRandom(int n)
    {
        n = (n << 13) ^ n;
        return ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 2147483647f;
    }
}
