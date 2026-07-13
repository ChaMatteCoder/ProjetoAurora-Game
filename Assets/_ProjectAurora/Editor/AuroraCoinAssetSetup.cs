using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class AuroraCoinAssetSetup
{
    private const string ModelPath = "Assets/_ProjectAurora/Art/Collectibles/AuroraCoin/Models/Aurora_HoloCoin.fbx";
    private const string MaterialFolder = "Assets/_ProjectAurora/Art/Collectibles/AuroraCoin/Materials";
    private const string FrameMaterialPath = MaterialFolder + "/MAT_AuroraCoin_Frame.mat";
    private const string HologramMaterialPath = MaterialFolder + "/MAT_AuroraCoin_Hologram.mat";
    private const string EmissionMaterialPath = MaterialFolder + "/MAT_AuroraCoin_Emission.mat";
    private const string PrefabPath = "Assets/_ProjectAurora/Prefabs/Collectibles/PF_Aurora_HoloCoin.prefab";

    [MenuItem("Tools/Projeto Aurora/Collectibles/Rebuild Aurora HoloCoin")]
    public static void BuildAsset()
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null)
        {
            Debug.LogError("[AuroraCoin] FBX not found at " + ModelPath);
            return;
        }

        ConfigureModelImporter();
        model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null)
        {
            Debug.LogError("[AuroraCoin] URP/Lit shader was not found.");
            return;
        }

        Material frameMaterial = GetOrCreateMaterial(FrameMaterialPath, litShader);
        frameMaterial.SetColor("_BaseColor", new Color(0.018f, 0.038f, 0.067f, 1f));
        frameMaterial.SetFloat("_Metallic", 0.78f);
        frameMaterial.SetFloat("_Smoothness", 0.62f);

        Material hologramMaterial = GetOrCreateMaterial(HologramMaterialPath, litShader);
        hologramMaterial.SetColor("_BaseColor", new Color(0f, 0.62f, 0.88f, 0.52f));
        hologramMaterial.SetColor("_EmissionColor", new Color(0f, 1.4f, 2.4f, 1f));
        hologramMaterial.SetFloat("_Metallic", 0.05f);
        hologramMaterial.SetFloat("_Smoothness", 0.88f);
        hologramMaterial.EnableKeyword("_EMISSION");
        ConfigureTransparent(hologramMaterial);

        Material emissionMaterial = GetOrCreateMaterial(EmissionMaterialPath, litShader);
        emissionMaterial.SetColor("_BaseColor", new Color(0.05f, 0.88f, 1f, 1f));
        emissionMaterial.SetColor("_EmissionColor", new Color(0f, 2.3f, 4.2f, 1f));
        emissionMaterial.SetFloat("_Metallic", 0.05f);
        emissionMaterial.SetFloat("_Smoothness", 0.75f);
        emissionMaterial.EnableKeyword("_EMISSION");
        emissionMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        EditorUtility.SetDirty(frameMaterial);
        EditorUtility.SetDirty(hologramMaterial);
        EditorUtility.SetDirty(emissionMaterial);

        GameObject root = new GameObject("PF_Aurora_HoloCoin");
        GameObject visualRootObject = new GameObject("VisualRoot");
        visualRootObject.transform.SetParent(root.transform, false);

        GameObject modelInstance = Object.Instantiate(model);
        modelInstance.name = "Aurora_HoloCoin_Model";
        Transform[] directChildren = new Transform[modelInstance.transform.childCount];
        for (int i = 0; i < directChildren.Length; i++)
        {
            directChildren[i] = modelInstance.transform.GetChild(i);
        }

        for (int i = 0; i < directChildren.Length; i++)
        {
            directChildren[i].SetParent(visualRootObject.transform, true);
        }
        Object.DestroyImmediate(modelInstance);

        List<Renderer> emissionRenderers = new List<Renderer>(3);
        Renderer[] renderers = visualRootObject.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer.name == "Coin_Frame" || renderer.name == "Coin_BackPlate")
            {
                renderer.sharedMaterial = frameMaterial;
            }
            else if (renderer.name == "Coin_HologramCore")
            {
                renderer.sharedMaterial = hologramMaterial;
                emissionRenderers.Add(renderer);
            }
            else if (renderer.name == "Coin_AuroraSymbol" || renderer.name == "Coin_EmissionDetails")
            {
                renderer.sharedMaterial = emissionMaterial;
                emissionRenderers.Add(renderer);
            }
        }

        SphereCollider trigger = root.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.285f;

        Rigidbody body = root.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.None;
        body.collisionDetectionMode = CollisionDetectionMode.Discrete;

        AuroraCoinVisualController visualController = root.AddComponent<AuroraCoinVisualController>();
        visualController.Configure(visualRootObject.transform, emissionRenderers.ToArray());
        root.AddComponent<AuroraCoinCollectible>();

        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.transform.localScale = Vector3.one;
        visualRootObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        visualRootObject.transform.localScale = Vector3.one;

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SetLabels(AssetDatabase.LoadMainAssetAtPath(ModelPath), new[] { "Aurora", "Collectible", "Gameplay" });
        AssetDatabase.SetLabels(AssetDatabase.LoadMainAssetAtPath(PrefabPath), new[] { "Aurora", "Collectible", "Prefab" });
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        ValidateAsset();
    }

    [MenuItem("Tools/Projeto Aurora/Collectibles/Validate Aurora HoloCoin")]
    public static void ValidateAsset()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[AuroraCoin] Prefab not found at " + PrefabPath);
            return;
        }

        List<string> errors = new List<string>();
        if (prefab.transform.localPosition != Vector3.zero || prefab.transform.localRotation != Quaternion.identity || prefab.transform.localScale != Vector3.one)
        {
            errors.Add("root transforms are not reset");
        }
        if (prefab.GetComponent<AuroraCoinVisualController>() == null)
        {
            errors.Add("AuroraCoinVisualController is missing");
        }
        if (prefab.GetComponent<AuroraCoinCollectible>() == null)
        {
            errors.Add("AuroraCoinCollectible is missing");
        }
        SphereCollider trigger = prefab.GetComponent<SphereCollider>();
        if (trigger == null || !trigger.isTrigger)
        {
            errors.Add("trigger SphereCollider is missing or disabled");
        }
        Rigidbody body = prefab.GetComponent<Rigidbody>();
        if (body == null || !body.isKinematic || body.useGravity)
        {
            errors.Add("kinematic Rigidbody configuration is invalid");
        }

        MeshFilter[] filters = prefab.GetComponentsInChildren<MeshFilter>(true);
        int vertices = 0;
        int triangles = 0;
        for (int i = 0; i < filters.Length; i++)
        {
            Mesh mesh = filters[i].sharedMesh;
            if (mesh == null)
            {
                continue;
            }
            vertices += mesh.vertexCount;
            triangles += mesh.triangles.Length / 3;
        }

        GameObject preview = Object.Instantiate(prefab);
        Renderer[] renderers = preview.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(Vector3.zero, Vector3.zero);
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        if (bounds.size.y < 0.40f || bounds.size.z > 0.20f)
        {
            errors.Add("model orientation is invalid: expected vertical Y and shallow Z, found " + bounds.size.ToString("F3"));
        }

        bool collectCompleted = false;
        AuroraCoinVisualController previewController = preview.GetComponent<AuroraCoinVisualController>();
        if (previewController != null)
        {
            previewController.OnCollectAnimationCompleted.AddListener(() => collectCompleted = true);
            previewController.PlayCollectAnimation();
            MethodInfo updateCollection = typeof(AuroraCoinVisualController).GetMethod(
                "UpdateCollection",
                BindingFlags.Instance | BindingFlags.NonPublic);
            updateCollection?.Invoke(previewController, new object[] { 1f });
        }
        if (preview.activeSelf || !collectCompleted)
        {
            errors.Add("collection animation did not invoke completion and deactivate the pooled object");
        }
        Object.DestroyImmediate(preview);

        if (filters.Length != 5)
        {
            errors.Add("expected 5 mesh objects, found " + filters.Length);
        }
        if (triangles <= 0 || triangles > 6000)
        {
            errors.Add("triangle count is outside the 1-6000 validation range: " + triangles);
        }

        if (errors.Count > 0)
        {
            Debug.LogError("[AuroraCoin] VALIDATION FAILED: " + string.Join("; ", errors));
            return;
        }

        Debug.Log(
            "[AuroraCoin] VALIDATION OK | meshes=" + filters.Length +
            " vertices=" + vertices +
            " triangles=" + triangles +
            " bounds=" + bounds.size.ToString("F3") +
            " materials=3 trigger=SphereCollider rigidbody=kinematic collectAnimation=pass");
    }

    [MenuItem("Tools/Projeto Aurora/Collectibles/Preview Play Collect Animation")]
    public static void PlayCollectionPreview()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[AuroraCoin] Enter Play Mode before running the collection preview.");
            return;
        }

        GameObject preview = GameObject.Find("AuroraCoin_TestPreview");
        AuroraCoinVisualController controller = preview != null
            ? preview.GetComponent<AuroraCoinVisualController>()
            : null;
        if (controller == null)
        {
            Debug.LogError("[AuroraCoin] AuroraCoin_TestPreview was not found in the active scene.");
            return;
        }

        controller.PlayCollectAnimation();
        Debug.Log("[AuroraCoin] Collection preview started.");
    }

    private static void ConfigureModelImporter()
    {
        ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null)
        {
            return;
        }

        bool changed = false;
        changed |= SetIfDifferent(importer.globalScale, 1f, value => importer.globalScale = value);
        changed |= SetIfDifferent(importer.importAnimation, false, value => importer.importAnimation = value);
        changed |= SetIfDifferent(importer.importCameras, false, value => importer.importCameras = value);
        changed |= SetIfDifferent(importer.importLights, false, value => importer.importLights = value);
        changed |= SetIfDifferent(importer.isReadable, false, value => importer.isReadable = value);
        changed |= SetIfDifferent(importer.materialImportMode, ModelImporterMaterialImportMode.None, value => importer.materialImportMode = value);
        changed |= SetIfDifferent(importer.meshCompression, ModelImporterMeshCompression.Off, value => importer.meshCompression = value);

        if (changed)
        {
            importer.SaveAndReimport();
        }
    }

    private static Material GetOrCreateMaterial(string path, Shader shader)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
        }
        return material;
    }

    private static void ConfigureTransparent(Material material)
    {
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)RenderQueue.Transparent;
    }

    private static bool SetIfDifferent<T>(T current, T expected, System.Action<T> setter)
    {
        if (EqualityComparer<T>.Default.Equals(current, expected))
        {
            return false;
        }
        setter(expected);
        return true;
    }
}
