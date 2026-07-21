using System.Collections.Generic;
using ProjectAurora.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Reconstroi a sinalizacao de piso da pista (Round 17).
///
/// PROBLEMA ORIGINAL (feedback de playtest):
///   - Os "Floor Chevron" apontavam para TRAS. As barras giravam L=-34 / R=+34, o que
///     faz as pontas convergirem em -Z; como o player corre para +Z, a seta apontava
///     no sentido contrario ao da corrida.
///   - Ritmo irregular: grupos de 3 fileiras a cada 45m (rajada + 45m de vazio) e o
///     Setor A ainda tinha 6 marcadores extras do vestibulo em outra base de Z.
///   - Geometria simples demais: duas caixas chapadas, sem hierarquia visual.
///
/// SOLUCAO: um unico sistema coerente ao longo de toda a pista —
///   - Seta FORWARD (L=+34 / R=-34 => convergem em +Z).
///   - Glifo em duas camadas (chevron externo largo + interno estreito) para leitura
///     em velocidade e sensacao de profundidade.
///   - Cadencia regular (padrao 15m) do inicio ao fim, sem lacunas nem aglomerados.
///   - AuroraTrackGuidance anima uma crista de luz correndo para frente.
public static class AuroraTrackGuidanceBuilder
{
    private const string RootName = "Track Guidance";
    private const string MarkerPrefix = "Track Chevron";
    private const float MarkerY = 0.11f;
    private const float Angle = 34f;

    [MenuItem("Aurora/Pista/Reconstruir sinalizacao de piso", priority = 20)]
    public static void Rebuild()
    {
        const float startZ = -12f;
        const float endZ = 2580f;
        const float spacing = 15f;

        Material mat = ResolveEmissionMaterial();
        if (mat == null)
        {
            EditorUtility.DisplayDialog("Aurora",
                "Material M_F01_CyanEmission nao encontrado na cena nem no projeto.", "OK");
            return;
        }

        int removed = RemoveLegacyChevrons();

        GameObject root = GameObject.Find(RootName);
        if (root != null)
        {
            Undo.DestroyObjectImmediate(root);
        }
        root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Track Guidance");

        var renderers = new List<Renderer>();
        int markers = 0;
        for (float z = startZ; z <= endZ; z += spacing)
        {
            GameObject marker = new GameObject(MarkerPrefix + " " + markers.ToString("D3"));
            marker.transform.SetParent(root.transform);
            marker.transform.position = new Vector3(0f, 0f, z);

            // camada externa: chevron largo e longo (le de longe)
            renderers.Add(Bar(marker.transform, "Outer L", -0.44f, 0.13f, 1.30f, +Angle, mat));
            renderers.Add(Bar(marker.transform, "Outer R", +0.44f, 0.13f, 1.30f, -Angle, mat));
            // camada interna: chevron curto encaixado (da hierarquia ao glifo)
            renderers.Add(Bar(marker.transform, "Inner L", -0.21f, 0.105f, 0.74f, +Angle, mat));
            renderers.Add(Bar(marker.transform, "Inner R", +0.21f, 0.105f, 0.74f, -Angle, mat));

            markers++;
        }

        AuroraTrackGuidance guidance = Object.FindFirstObjectByType<AuroraTrackGuidance>();
        if (guidance == null)
        {
            guidance = root.AddComponent<AuroraTrackGuidance>();
        }
        else if (guidance.gameObject != root)
        {
            Object.DestroyImmediate(guidance);
            guidance = root.AddComponent<AuroraTrackGuidance>();
        }
        guidance.markers = renderers;
        guidance.markerNamePrefix = MarkerPrefix;
        EditorUtility.SetDirty(guidance);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[Aurora/Pista] {removed} chevrons antigos removidos · " +
                  $"{markers} marcadores novos ({renderers.Count} barras) de z={startZ} a z={endZ} " +
                  $"a cada {spacing}m · setas apontando para +Z.");
    }

    private static Renderer Bar(Transform parent, string name, float x, float width,
        float length, float angleY, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.localPosition = new Vector3(x, MarkerY, 0f);
        go.transform.localRotation = Quaternion.Euler(0f, angleY, 0f);
        go.transform.localScale = new Vector3(width, 0.024f, length);

        Object.DestroyImmediate(go.GetComponent<Collider>());
        Renderer r = go.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        GameObjectUtility.SetStaticEditorFlags(go,
            StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic);
        return r;
    }

    private static int RemoveLegacyChevrons()
    {
        var doomed = new List<GameObject>();
        foreach (Transform t in Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t != null && t.name.Contains("Floor Chevron"))
            {
                doomed.Add(t.gameObject);
            }
        }
        foreach (GameObject go in doomed)
        {
            Undo.DestroyObjectImmediate(go);
        }
        return doomed.Count;
    }

    private static Material ResolveEmissionMaterial()
    {
        // 1) reaproveita o material ja usado na cena (garante _EMISSION serializado)
        foreach (Renderer r in Object.FindObjectsByType<Renderer>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Material m = r.sharedMaterial;
            if (m != null && m.name.Contains("M_F01_CyanEmission"))
            {
                return m;
            }
        }
        // 2) fallback: procura no projeto
        foreach (string guid in AssetDatabase.FindAssets("M_F01_CyanEmission t:Material"))
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (m != null)
            {
                return m;
            }
        }
        return null;
    }
}
