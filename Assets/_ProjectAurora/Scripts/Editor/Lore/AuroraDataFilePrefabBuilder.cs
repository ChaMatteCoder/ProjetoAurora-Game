#if UNITY_EDITOR
using ProjectAurora.Lore;
using UnityEditor;
using UnityEngine;

namespace ProjectAurora.Editor.Lore
{
    public static class AuroraDataFilePrefabBuilder
    {
        public const string PrefabPath =
            "Assets/_ProjectAurora/Prefabs/Collectibles/PF_Aurora_DataFile.prefab";
        private const string MaterialFolder = "Assets/_ProjectAurora/Art/Lore/Materials";
        private const string DarkMaterialPath = MaterialFolder + "/MAT_DataFile_Dark.mat";
        private const string CyanMaterialPath = MaterialFolder + "/MAT_DataFile_Cyan.mat";
        private const string WhiteMaterialPath = MaterialFolder + "/MAT_DataFile_Core.mat";

        [MenuItem("Tools/Projeto Aurora/Lore/Rebuild DataFile Prefab")]
        public static GameObject RebuildDataFilePrefab()
        {
            EnsureFolder("Assets/_ProjectAurora/Prefabs/Collectibles");
            EnsureFolder(MaterialFolder);

            Material dark = EnsureMaterial(DarkMaterialPath,
                new Color(0.006f, 0.018f, 0.03f, 1f), Color.black);
            Material cyan = EnsureMaterial(CyanMaterialPath,
                new Color(0.015f, 0.24f, 0.29f, 1f), new Color(0.05f, 3.2f, 4.2f, 1f));
            Material core = EnsureMaterial(WhiteMaterialPath,
                new Color(0.72f, 0.94f, 1f, 1f), new Color(0.25f, 1.6f, 2.1f, 1f));
            AuroraLoreCatalog catalog = AssetDatabase.LoadAssetAtPath<AuroraLoreCatalog>(
                AuroraLoreCatalogBuilder.CatalogPath);
            if (catalog == null) catalog = AuroraLoreCatalogBuilder.RebuildLoreCatalog();

            GameObject root = new GameObject("PF_Aurora_DataFile");
            try
            {
                BoxCollider trigger = root.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.size = new Vector3(1.5f, 1.15f, 1.25f);
                Rigidbody body = root.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;

                GameObject visualRoot = new GameObject("VisualRoot");
                visualRoot.transform.SetParent(root.transform, false);
                visualRoot.transform.localPosition = new Vector3(0f, 0.58f, 0f);
                visualRoot.transform.localRotation = Quaternion.Euler(12f, 0f, -5f);
                visualRoot.AddComponent<AuroraDataFileVisualController>();

                CreatePart(visualRoot.transform, "ArmoredBody", PrimitiveType.Cube,
                    Vector3.zero, new Vector3(1.12f, 0.16f, 0.72f), dark);
                CreatePart(visualRoot.transform, "HoloScreen", PrimitiveType.Cube,
                    new Vector3(0f, 0.105f, 0f), new Vector3(0.78f, 0.045f, 0.44f), cyan);
                CreatePart(visualRoot.transform, "DataCore", PrimitiveType.Cylinder,
                    new Vector3(0f, 0.145f, 0f), new Vector3(0.15f, 0.028f, 0.15f), core,
                    new Vector3(0f, 0f, 0f));

                CreatePart(visualRoot.transform, "FrameTop", PrimitiveType.Cube,
                    new Vector3(0f, 0.14f, 0.3f), new Vector3(1.02f, 0.08f, 0.08f), cyan);
                CreatePart(visualRoot.transform, "FrameBottom", PrimitiveType.Cube,
                    new Vector3(0f, 0.14f, -0.3f), new Vector3(1.02f, 0.08f, 0.08f), cyan);
                CreatePart(visualRoot.transform, "FrameLeft", PrimitiveType.Cube,
                    new Vector3(-0.47f, 0.14f, 0f), new Vector3(0.08f, 0.08f, 0.54f), cyan);
                CreatePart(visualRoot.transform, "FrameRight", PrimitiveType.Cube,
                    new Vector3(0.47f, 0.14f, 0f), new Vector3(0.08f, 0.08f, 0.54f), cyan);

                CreatePart(visualRoot.transform, "PortLeft", PrimitiveType.Cylinder,
                    new Vector3(-0.59f, 0f, 0f), new Vector3(0.14f, 0.06f, 0.14f), core,
                    new Vector3(0f, 0f, 90f));
                CreatePart(visualRoot.transform, "PortRight", PrimitiveType.Cylinder,
                    new Vector3(0.59f, 0f, 0f), new Vector3(0.14f, 0.06f, 0.14f), core,
                    new Vector3(0f, 0f, 90f));

                AuroraDataFileCollectible collectible = root.AddComponent<AuroraDataFileCollectible>();
                collectible.ConfigureForEditor(catalog, "LORE_001", visualRoot, trigger);

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[AuroraDataFile] Prefab tecnológico reconstruído em " + PrefabPath +
                          " com exemplo configurado para LORE_001.");
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreatePart(
            Transform parent,
            string name,
            PrimitiveType primitive,
            Vector3 position,
            Vector3 scale,
            Material material,
            Vector3? rotation = null)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localRotation = Quaternion.Euler(rotation ?? Vector3.zero);
            part.transform.localScale = scale;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
            Renderer renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return part;
        }

        private static Material EnsureMaterial(string path, Color baseColor, Color emission)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                material = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = baseColor;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseColor);
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", emission);
            if (emission.maxColorComponent > 0f) material.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            string name = path.Substring(separator + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
