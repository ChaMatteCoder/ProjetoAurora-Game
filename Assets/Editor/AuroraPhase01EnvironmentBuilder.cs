using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class AuroraPhase01EnvironmentBuilder
{
    private const string GameScenePath = "Assets/Scenes/Game.unity";
    private const string GeneratedRoot = "Assets/_ProjectAurora/Art/Generated/Environment/Fase01";
    private const string MaterialRoot = GeneratedRoot + "/Materials";
    private const string VolumePath = GeneratedRoot + "/Fase01_Volume.asset";

    private const string FloorTexturePath = GeneratedRoot + "/Fase01_FloorGrid.png";
    private const string WallTexturePath = GeneratedRoot + "/Fase01_WallPanel.png";
    private const string HazardTexturePath = GeneratedRoot + "/Fase01_Hazard.png";

    private const string Box01Path =
        "Assets/_ProjectAurora/Art/Generated/Obstacles/Aurora_Box_01/modelo.glb";
    private const string Box02Path =
        "Assets/_ProjectAurora/Art/Generated/Obstacles/Aurora_Box_02/modelo.glb";
    private const string DoorPath =
        "Assets/_ProjectAurora/Art/Generated/Obstacles/Aurora_Door_01/modelo.glb";
    private const string Laser02Path =
        "Assets/_ProjectAurora/Art/Generated/Obstacles/Aurora_Lazer_02/modelo.glb";

    private static readonly Color Cyan = new Color(0.04f, 0.82f, 1f);
    private static readonly Color White = new Color(0.72f, 0.79f, 0.85f);
    private static readonly Color Dark = new Color(0.018f, 0.028f, 0.043f);

    private sealed class Palette
    {
        public Material WhitePanel;
        public Material DarkMetal;
        public Material Floor;
        public Material FloorInset;
        public Material CyanEmission;
        public Material WhiteEmission;
        public Material RedEmission;
        public Material Glass;
        public Material Plant;
        public Material Soil;
        public Material Hazard;
        public Material Screen;
    }

    [MenuItem("Tools/Projeto Aurora/Fase 01/Rebuild Detailed Environment")]
    public static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != GameScenePath)
        {
            scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        }

        EnsureFolders();
        ConfigureTexture(FloorTexturePath);
        ConfigureTexture(WallTexturePath);
        ConfigureTexture(HazardTexturePath);

        Palette palette = CreatePalette();
        RebuildEnvironment(palette);
        RebuildObstacleVisuals(palette);
        ConfigureLightingAndCamera();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, GameScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("PROJETO:AURORA - Fase 01 detalhada reconstruida.");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/_ProjectAurora/Art/Generated/Environment");
        EnsureFolder(GeneratedRoot);
        EnsureFolder(MaterialRoot);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private static void ConfigureTexture(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = true;
        importer.textureType = TextureImporterType.Default;
        importer.spriteImportMode = SpriteImportMode.None;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }

    private static Palette CreatePalette()
    {
        Texture floorTexture = AssetDatabase.LoadAssetAtPath<Texture>(FloorTexturePath);
        Texture wallTexture = AssetDatabase.LoadAssetAtPath<Texture>(WallTexturePath);
        Texture hazardTexture = AssetDatabase.LoadAssetAtPath<Texture>(HazardTexturePath);

        Palette palette = new Palette
        {
            WhitePanel = MakeLitMaterial("M_F01_WhitePanel", White, 0.15f, 0.72f, wallTexture, new Vector2(2f, 6f)),
            DarkMetal = MakeLitMaterial("M_F01_DarkMetal", Dark, 0.82f, 0.6f, null, Vector2.one),
            Floor = MakeLitMaterial("M_F01_Floor", new Color(0.9f, 0.96f, 1f), 0.68f, 0.72f,
                floorTexture, new Vector2(6f, 90f)),
            FloorInset = MakeLitMaterial("M_F01_FloorInset", new Color(0.68f, 0.78f, 0.88f), 0.5f, 0.78f,
                floorTexture, new Vector2(3f, 70f)),
            CyanEmission = MakeEmissionMaterial("M_F01_CyanEmission", Cyan, 4.5f),
            WhiteEmission = MakeEmissionMaterial("M_F01_WhiteEmission", new Color(0.78f, 0.9f, 1f), 3.2f),
            RedEmission = MakeEmissionMaterial("M_F01_RedEmission", new Color(1f, 0.025f, 0.02f), 5f),
            Glass = MakeGlassMaterial("M_F01_Glass", new Color(0.12f, 0.62f, 0.76f, 0.13f)),
            Plant = MakeLitMaterial("M_F01_Plant", new Color(0.08f, 0.38f, 0.18f), 0.05f, 0.42f,
                null, Vector2.one),
            Soil = MakeLitMaterial("M_F01_Soil", new Color(0.055f, 0.04f, 0.03f), 0f, 0.18f,
                null, Vector2.one),
            Hazard = MakeLitMaterial("M_F01_Hazard", new Color(0.85f, 0.55f, 0.02f), 0.28f, 0.48f,
                hazardTexture, new Vector2(3f, 1f)),
            Screen = MakeEmissionMaterial("M_F01_Screen", new Color(0.02f, 0.42f, 0.65f), 3.8f)
        };
        return palette;
    }

    private static Material MakeLitMaterial(string name, Color color, float metallic, float smoothness,
        Texture texture, Vector2 textureScale)
    {
        string path = MaterialRoot + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        if (texture != null)
        {
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", textureScale);
        }
        else
        {
            material.SetTexture("_BaseMap", null);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material MakeEmissionMaterial(string name, Color color, float intensity)
    {
        Material material = MakeLitMaterial(name, color * 0.42f, 0.18f, 0.82f, null, Vector2.one);
        Color emission = new Color(color.r * intensity, color.g * intensity, color.b * intensity, 1f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", emission);
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material MakeGlassMaterial(string name, Color color)
    {
        Material material = MakeLitMaterial(name, color, 0.08f, 0.96f, null, Vector2.one);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void RebuildEnvironment(Palette palette)
    {
        DestroyByName("Environment - 2700m");
        DestroyByName("Fase 01 - Aurora Research Corridor");

        GameObject environment = new GameObject("Fase 01 - Aurora Research Corridor");
        string[] sectorNames =
        {
            "Setor A - Laboratorio",
            "Corredor de Contencao",
            "Sala de Maquinas",
            "Corredor Vermelho",
            "Ponte Tecnica",
            "Terminal Central"
        };

        for (int i = 0; i < sectorNames.Length; i++)
        {
            CreateSector(environment.transform, sectorNames[i], i, palette, i == 0);
        }
    }

    private static void CreateSector(Transform parent, string sectorName, int sectorIndex, Palette palette,
        bool detailed)
    {
        GameObject sector = new GameObject(sectorName);
        sector.transform.SetParent(parent);
        sector.transform.localPosition = new Vector3(0f, 0f, sectorIndex * 450f + 225f);

        CreateSectorShell(sector.transform, palette);
        if (detailed)
        {
            CreateStartVestibule(sector.transform, palette);
        }

        float archSpacing = detailed ? 15f : 30f;
        float firstArch = sectorIndex == 0 ? -225f : -225f + archSpacing;
        for (float z = firstArch; z <= 225f; z += archSpacing)
        {
            CreateArchFrame(sector.transform, z, palette);
        }

        float baySpacing = detailed ? 15f : 30f;
        int bayIndex = 0;
        for (float z = -225f + baySpacing * 0.5f; z < 225f; z += baySpacing)
        {
            CreateSideBay(sector.transform, z, baySpacing - 1.2f, palette);
            CreateCeilingLight(sector.transform, z, palette);

            if (detailed)
            {
                if (bayIndex % 2 == 0)
                {
                    CreateBotanicalPod(sector.transform, bayIndex % 4 == 0 ? -1 : 1, z, palette);
                }
                else
                {
                    CreatePlanter(sector.transform, bayIndex % 4 == 1 ? 1 : -1, z, palette);
                }

                if (bayIndex % 4 == 1)
                {
                    CreateConsole(sector.transform, -1, z - 2.8f, palette);
                }
                else if (bayIndex % 4 == 3)
                {
                    CreateConsole(sector.transform, 1, z + 2.8f, palette);
                }
            }
            else if (bayIndex % 3 == 1)
            {
                CreatePlanter(sector.transform, bayIndex % 2 == 0 ? -1 : 1, z, palette);
            }

            bayIndex++;
        }

        for (float z = -202.5f; z < 225f; z += 45f)
        {
            CreateFloorChevrons(sector.transform, z, palette);
        }

        CreateSectorSign(sector.transform, sectorName, -207f, palette);
        if (detailed)
        {
            CreateAuroraLogoWall(sector.transform, -1, -142f, palette);
            CreateAuroraLogoWall(sector.transform, 1, 38f, palette);
            CreateAuroraLogoWall(sector.transform, -1, 188f, palette);
        }
    }

    private static void CreateSectorShell(Transform parent, Palette palette)
    {
        Box("Structural Floor", parent, new Vector3(0f, -0.35f, 0f), new Vector3(14f, 0.7f, 450f),
            Quaternion.identity, palette.DarkMetal, true);
        Box("Track Surface", parent, new Vector3(0f, 0.015f, 0f), new Vector3(9.6f, 0.04f, 450f),
            Quaternion.identity, palette.Floor, false);
        Box("Left Catwalk", parent, new Vector3(-6.15f, 0.04f, 0f), new Vector3(2.5f, 0.08f, 450f),
            Quaternion.identity, palette.WhitePanel, false);
        Box("Right Catwalk", parent, new Vector3(6.15f, 0.04f, 0f), new Vector3(2.5f, 0.08f, 450f),
            Quaternion.identity, palette.WhitePanel, false);

        for (int i = -1; i <= 1; i++)
        {
            Box("Lane " + (i + 2), parent, new Vector3(i * 3f, 0.055f, 0f), new Vector3(2.78f, 0.035f, 450f),
                Quaternion.identity, palette.FloorInset, false);
        }

        Box("Lane Divider L", parent, new Vector3(-1.5f, 0.082f, 0f), new Vector3(0.045f, 0.018f, 450f),
            Quaternion.identity, palette.WhiteEmission, false);
        Box("Lane Divider R", parent, new Vector3(1.5f, 0.082f, 0f), new Vector3(0.045f, 0.018f, 450f),
            Quaternion.identity, palette.WhiteEmission, false);
        Box("Track Glow L", parent, new Vector3(-4.72f, 0.09f, 0f), new Vector3(0.09f, 0.04f, 450f),
            Quaternion.identity, palette.CyanEmission, false);
        Box("Track Glow R", parent, new Vector3(4.72f, 0.09f, 0f), new Vector3(0.09f, 0.04f, 450f),
            Quaternion.identity, palette.CyanEmission, false);

        Box("Left Lab Back Wall", parent, new Vector3(-10.4f, 3.4f, 0f), new Vector3(0.35f, 6.8f, 450f),
            Quaternion.identity, palette.WhitePanel, false);
        Box("Right Lab Back Wall", parent, new Vector3(10.4f, 3.4f, 0f), new Vector3(0.35f, 6.8f, 450f),
            Quaternion.identity, palette.WhitePanel, false);
        Box("Left Lab Floor", parent, new Vector3(-8.7f, -0.05f, 0f), new Vector3(3.2f, 0.16f, 450f),
            Quaternion.identity, palette.FloorInset, false);
        Box("Right Lab Floor", parent, new Vector3(8.7f, -0.05f, 0f), new Vector3(3.2f, 0.16f, 450f),
            Quaternion.identity, palette.FloorInset, false);

        Box("Center Ceiling", parent, new Vector3(0f, 7.55f, 0f), new Vector3(6.6f, 0.28f, 450f),
            Quaternion.identity, palette.WhitePanel, false);
        Box("Left Ceiling Shoulder", parent, new Vector3(-5.9f, 6.85f, 0f), new Vector3(5.8f, 0.24f, 450f),
            Quaternion.Euler(0f, 0f, -11f), palette.WhitePanel, false);
        Box("Right Ceiling Shoulder", parent, new Vector3(5.9f, 6.85f, 0f), new Vector3(5.8f, 0.24f, 450f),
            Quaternion.Euler(0f, 0f, 11f), palette.WhitePanel, false);

        Pipe("Left Main Duct", parent, new Vector3(-7.8f, 6.45f, 0f), 0.28f, 450f, palette.DarkMetal);
        Pipe("Right Main Duct", parent, new Vector3(7.8f, 6.45f, 0f), 0.28f, 450f, palette.DarkMetal);
        Pipe("Left Cyan Conduit", parent, new Vector3(-6.95f, 6.2f, 0f), 0.08f, 450f, palette.CyanEmission);
        Pipe("Right Cyan Conduit", parent, new Vector3(6.95f, 6.2f, 0f), 0.08f, 450f, palette.CyanEmission);
    }

    private static void CreateStartVestibule(Transform parent, Palette palette)
    {
        const float z = -240f;
        const float length = 30f;
        Box("Start Structural Floor", parent, new Vector3(0f, -0.35f, z), new Vector3(14f, 0.7f, length),
            Quaternion.identity, palette.DarkMetal, true);
        Box("Start Track Surface", parent, new Vector3(0f, 0.015f, z), new Vector3(9.6f, 0.04f, length),
            Quaternion.identity, palette.Floor, false);
        Box("Start Catwalk L", parent, new Vector3(-6.15f, 0.04f, z), new Vector3(2.5f, 0.08f, length),
            Quaternion.identity, palette.WhitePanel, false);
        Box("Start Catwalk R", parent, new Vector3(6.15f, 0.04f, z), new Vector3(2.5f, 0.08f, length),
            Quaternion.identity, palette.WhitePanel, false);
        Box("Start Lab Wall L", parent, new Vector3(-10.4f, 3.4f, z), new Vector3(0.35f, 6.8f, length),
            Quaternion.identity, palette.WhitePanel, false);
        Box("Start Lab Wall R", parent, new Vector3(10.4f, 3.4f, z), new Vector3(0.35f, 6.8f, length),
            Quaternion.identity, palette.WhitePanel, false);
        Box("Start Glass L", parent, new Vector3(-7.08f, 3.4f, z), new Vector3(0.055f, 4.75f, length - 1f),
            Quaternion.identity, palette.Glass, false, false);
        Box("Start Glass R", parent, new Vector3(7.08f, 3.4f, z), new Vector3(0.055f, 4.75f, length - 1f),
            Quaternion.identity, palette.Glass, false, false);
        Box("Start Ceiling", parent, new Vector3(0f, 7.55f, z), new Vector3(6.6f, 0.28f, length),
            Quaternion.identity, palette.WhitePanel, false);
        Box("Start Glow L", parent, new Vector3(-4.72f, 0.09f, z), new Vector3(0.09f, 0.04f, length),
            Quaternion.identity, palette.CyanEmission, false);
        Box("Start Glow R", parent, new Vector3(4.72f, 0.09f, z), new Vector3(0.09f, 0.04f, length),
            Quaternion.identity, palette.CyanEmission, false);
        CreateArchFrame(parent, z, palette);
        CreateFloorChevrons(parent, z, palette);
    }

    private static void CreateFloorChevrons(Transform parent, float z, Palette palette)
    {
        for (int i = 0; i < 3; i++)
        {
            float rowZ = z + i * 0.72f;
            Box("Floor Chevron L", parent, new Vector3(-0.28f, 0.11f, rowZ),
                new Vector3(0.18f, 0.025f, 0.92f), Quaternion.Euler(0f, -34f, 0f),
                palette.CyanEmission, false, false);
            Box("Floor Chevron R", parent, new Vector3(0.28f, 0.11f, rowZ),
                new Vector3(0.18f, 0.025f, 0.92f), Quaternion.Euler(0f, 34f, 0f),
                palette.CyanEmission, false, false);
        }
    }

    private static void CreateArchFrame(Transform parent, float z, Palette palette)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            Box("Arch Foot", parent, new Vector3(side * 5.55f, 0.42f, z), new Vector3(1.25f, 0.84f, 1f),
                Quaternion.identity, palette.DarkMetal, false);
            Box("Arch Pillar", parent, new Vector3(side * 5.62f, 3.35f, z), new Vector3(0.7f, 5.8f, 0.82f),
                Quaternion.identity, palette.WhitePanel, false);
            Box("Arch Inner Spine", parent, new Vector3(side * 5.18f, 3.45f, z), new Vector3(0.22f, 4.75f, 0.9f),
                Quaternion.identity, palette.DarkMetal, false);
            Box("Arch Vertical Glow", parent, new Vector3(side * 5.04f, 3.45f, z - 0.01f),
                new Vector3(0.07f, 3.55f, 0.93f), Quaternion.identity, palette.CyanEmission, false);
            Box("Arch Hazard Foot", parent, new Vector3(side * 5.53f, 0.6f, z - 0.48f),
                new Vector3(0.52f, 0.25f, 0.04f), Quaternion.identity, palette.Hazard, false);
        }

        Box("Arch Top", parent, new Vector3(0f, 7.05f, z), new Vector3(8.2f, 0.48f, 0.86f),
            Quaternion.identity, palette.DarkMetal, false);
        Box("Arch Top Panel", parent, new Vector3(0f, 7.34f, z), new Vector3(5.4f, 0.22f, 0.9f),
            Quaternion.identity, palette.WhitePanel, false);
        Box("Arch Top Glow", parent, new Vector3(0f, 6.78f, z - 0.02f), new Vector3(4.8f, 0.08f, 0.94f),
            Quaternion.identity, palette.WhiteEmission, false);
        Box("Arch Shoulder L", parent, new Vector3(-4.55f, 6.35f, z), new Vector3(2.35f, 0.42f, 0.84f),
            Quaternion.Euler(0f, 0f, -28f), palette.WhitePanel, false);
        Box("Arch Shoulder R", parent, new Vector3(4.55f, 6.35f, z), new Vector3(2.35f, 0.42f, 0.84f),
            Quaternion.Euler(0f, 0f, 28f), palette.WhitePanel, false);
    }

    private static void CreateSideBay(Transform parent, float z, float depth, Palette palette)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            Box("Lab Glass", parent, new Vector3(side * 7.08f, 3.4f, z), new Vector3(0.055f, 4.75f, depth),
                Quaternion.identity, palette.Glass, false, false);
            Box("Glass Lower Rail", parent, new Vector3(side * 7.04f, 0.95f, z), new Vector3(0.28f, 0.28f, depth),
                Quaternion.identity, palette.DarkMetal, false);
            Box("Glass Upper Rail", parent, new Vector3(side * 7.04f, 5.87f, z), new Vector3(0.28f, 0.28f, depth),
                Quaternion.identity, palette.DarkMetal, false);
            Box("Lab Base Cabinet", parent, new Vector3(side * 9.65f, 0.65f, z),
                new Vector3(1.05f, 1.3f, depth - 0.6f), Quaternion.identity, palette.WhitePanel, false);
            Box("Cabinet Toe", parent, new Vector3(side * 9.55f, 0.16f, z),
                new Vector3(1.25f, 0.22f, depth - 0.4f), Quaternion.identity, palette.DarkMetal, false);
        }
    }

    private static void CreateCeilingLight(Transform parent, float z, Palette palette)
    {
        Box("Ceiling Light L", parent, new Vector3(-2.35f, 7.32f, z), new Vector3(0.34f, 0.09f, 7.8f),
            Quaternion.identity, palette.WhiteEmission, false);
        Box("Ceiling Light R", parent, new Vector3(2.35f, 7.32f, z), new Vector3(0.34f, 0.09f, 7.8f),
            Quaternion.identity, palette.WhiteEmission, false);
        Box("Ceiling Light Frame L", parent, new Vector3(-2.35f, 7.39f, z), new Vector3(0.65f, 0.13f, 8.35f),
            Quaternion.identity, palette.DarkMetal, false);
        Box("Ceiling Light Frame R", parent, new Vector3(2.35f, 7.39f, z), new Vector3(0.65f, 0.13f, 8.35f),
            Quaternion.identity, palette.DarkMetal, false);
    }

    private static void CreateBotanicalPod(Transform parent, int side, float z, Palette palette)
    {
        GameObject pod = new GameObject("Botanical Research Pod");
        pod.transform.SetParent(parent);
        pod.transform.localPosition = new Vector3(side * 8.65f, 0f, z);

        Cylinder("Pod Base", pod.transform, new Vector3(0f, 0.65f, 0f), new Vector3(1.25f, 0.18f, 1.25f),
            Quaternion.identity, palette.DarkMetal);
        Cylinder("Pod Lower Ring", pod.transform, new Vector3(0f, 1.05f, 0f), new Vector3(1.12f, 0.12f, 1.12f),
            Quaternion.identity, palette.CyanEmission);
        Cylinder("Pod Glass", pod.transform, new Vector3(0f, 3.0f, 0f), new Vector3(1.02f, 1.9f, 1.02f),
            Quaternion.identity, palette.Glass, false);
        Cylinder("Pod Upper Ring", pod.transform, new Vector3(0f, 4.95f, 0f), new Vector3(1.14f, 0.14f, 1.14f),
            Quaternion.identity, palette.DarkMetal);
        Cylinder("Pod Crown", pod.transform, new Vector3(0f, 5.28f, 0f), new Vector3(1.28f, 0.2f, 1.28f),
            Quaternion.identity, palette.WhitePanel);
        Cylinder("Plant Stem", pod.transform, new Vector3(0f, 2.45f, 0f), new Vector3(0.1f, 1.2f, 0.1f),
            Quaternion.identity, palette.Plant);
        Sphere("Plant Leaf A", pod.transform, new Vector3(0.35f, 2.8f, 0f), new Vector3(0.68f, 0.16f, 0.35f),
            Quaternion.Euler(0f, 0f, 25f), palette.Plant);
        Sphere("Plant Leaf B", pod.transform, new Vector3(-0.34f, 2.3f, 0.18f), new Vector3(0.62f, 0.16f, 0.34f),
            Quaternion.Euler(12f, 35f, -28f), palette.Plant);
        Sphere("Plant Leaf C", pod.transform, new Vector3(0f, 3.35f, -0.24f), new Vector3(0.45f, 0.14f, 0.72f),
            Quaternion.Euler(-18f, 0f, 0f), palette.Plant);
        Sphere("Pod Core Light", pod.transform, new Vector3(0f, 5.0f, 0f), new Vector3(0.35f, 0.08f, 0.35f),
            Quaternion.identity, palette.CyanEmission);
    }

    private static void CreatePlanter(Transform parent, int side, float z, Palette palette)
    {
        GameObject planter = new GameObject("Laboratory Planter");
        planter.transform.SetParent(parent);
        planter.transform.localPosition = new Vector3(side * 8.45f, 0f, z);

        Box("Planter Body", planter.transform, new Vector3(0f, 0.62f, 0f), new Vector3(2.1f, 1.12f, 1.25f),
            Quaternion.identity, palette.WhitePanel, false);
        Box("Planter Base", planter.transform, new Vector3(0f, 0.15f, 0f), new Vector3(2.25f, 0.22f, 1.42f),
            Quaternion.identity, palette.DarkMetal, false);
        Box("Planter Soil", planter.transform, new Vector3(0f, 1.2f, 0f), new Vector3(1.85f, 0.12f, 1.02f),
            Quaternion.identity, palette.Soil, false);
        Box("Planter Glow", planter.transform, new Vector3(-side * 1.08f, 0.67f, 0f),
            new Vector3(0.055f, 0.62f, 0.82f), Quaternion.identity, palette.CyanEmission, false);

        for (int i = -2; i <= 2; i++)
        {
            float x = i * 0.32f;
            Cylinder("Plant Stem", planter.transform, new Vector3(x, 1.65f + (i % 2) * 0.12f, 0f),
                new Vector3(0.055f, 0.48f, 0.055f), Quaternion.Euler(0f, 0f, i * 7f), palette.Plant);
            Sphere("Plant Leaf", planter.transform, new Vector3(x + 0.12f, 1.95f, 0f),
                new Vector3(0.32f, 0.11f, 0.22f), Quaternion.Euler(0f, i * 23f, i * 9f), palette.Plant);
        }
    }

    private static void CreateConsole(Transform parent, int side, float z, Palette palette)
    {
        GameObject console = new GameObject("Research Console");
        console.transform.SetParent(parent);
        console.transform.localPosition = new Vector3(side * 6.1f, 0f, z);

        Box("Console Base", console.transform, new Vector3(0f, 0.55f, 0f), new Vector3(0.78f, 1.1f, 1.15f),
            Quaternion.identity, palette.WhitePanel, false);
        Box("Console Foot", console.transform, new Vector3(0f, 0.13f, 0f), new Vector3(0.95f, 0.22f, 1.35f),
            Quaternion.identity, palette.DarkMetal, false);
        Box("Console Neck", console.transform, new Vector3(-side * 0.12f, 1.18f, 0f),
            new Vector3(0.38f, 0.78f, 0.78f), Quaternion.Euler(0f, 0f, -side * 8f),
            palette.DarkMetal, false);
        Box("Console Screen", console.transform, new Vector3(-side * 0.38f, 1.62f, 0f),
            new Vector3(0.06f, 0.68f, 0.98f), Quaternion.Euler(0f, 0f, -side * 13f),
            palette.Screen, false);
    }

    private static void CreateSectorSign(Transform parent, string sectorName, float z, Palette palette)
    {
        Box("Sector Sign Frame", parent, new Vector3(0f, 5.85f, z + 0.12f), new Vector3(5.8f, 1.05f, 0.18f),
            Quaternion.identity, palette.DarkMetal, false);
        Box("Sector Sign Glow", parent, new Vector3(0f, 5.35f, z), new Vector3(4.4f, 0.055f, 0.06f),
            Quaternion.identity, palette.CyanEmission, false);
        CreateWorldText("Sector Label", parent, sectorName.ToUpperInvariant(), new Vector3(0f, 5.92f, z - 0.02f),
            Quaternion.Euler(0f, 180f, 0f), 0.055f, 48, new Color(0.72f, 0.94f, 1f));
    }

    private static void CreateAuroraLogoWall(Transform parent, int side, float z, Palette palette)
    {
        GameObject logo = new GameObject("Project Aurora Wall Mark");
        logo.transform.SetParent(parent);
        logo.transform.localPosition = new Vector3(side * 6.88f, 3.55f, z);
        logo.transform.localRotation = Quaternion.Euler(0f, side > 0 ? -90f : 90f, 0f);

        Box("Banner", logo.transform, Vector3.zero, new Vector3(2.75f, 3.5f, 0.12f),
            Quaternion.identity, palette.DarkMetal, false);
        Box("Aurora A Left", logo.transform, new Vector3(-0.36f, 0.55f, -0.08f),
            new Vector3(0.18f, 1.35f, 0.07f), Quaternion.Euler(0f, 0f, -24f), palette.CyanEmission, false);
        Box("Aurora A Right", logo.transform, new Vector3(0.36f, 0.55f, -0.08f),
            new Vector3(0.18f, 1.35f, 0.07f), Quaternion.Euler(0f, 0f, 24f), palette.CyanEmission, false);
        Box("Aurora A Bar", logo.transform, new Vector3(0f, 0.25f, -0.09f),
            new Vector3(0.72f, 0.15f, 0.07f), Quaternion.identity, palette.CyanEmission, false);
        CreateWorldText("Aurora Label", logo.transform, "PROJECT\nAURORA", new Vector3(0f, -0.82f, -0.1f),
            Quaternion.identity, 0.055f, 38, Color.white);
    }

    private static void RebuildObstacleVisuals(Palette palette)
    {
        DestroyByName("Fase01 - Detailed Obstacles");
        GameObject gameplay = GameObject.Find("Gameplay Objects");
        if (gameplay == null)
        {
            Debug.LogWarning("PROJETO:AURORA - Gameplay Objects nao encontrado.");
            return;
        }

        GameObject detailRoot = new GameObject("Fase01 - Detailed Obstacles");
        bool usedLowHero = false;
        bool usedTallHero = false;
        bool usedLaserHero = false;

        foreach (Transform obstacle in gameplay.transform)
        {
            if (obstacle.name == "Low Barrier")
            {
                DisableRenderer(obstacle.gameObject);
                if (!usedLowHero)
                {
                    InstantiateDetailedModel(Box02Path, detailRoot.transform, obstacle.position,
                        new Vector3(2.55f, 0.86f, 1.1f), Quaternion.Euler(0f, 90f, 0f));
                    usedLowHero = true;
                }
                else
                {
                    CreateCrateVisual(detailRoot.transform, obstacle.position, new Vector3(2.35f, 0.72f, 0.9f),
                        palette);
                }
            }
            else if (obstacle.name == "Containment Barrier")
            {
                DisableRenderer(obstacle.gameObject);
                if (!usedTallHero)
                {
                    InstantiateDetailedModel(Box01Path, detailRoot.transform, obstacle.position,
                        new Vector3(2.5f, 2.55f, 1.05f), Quaternion.Euler(0f, 90f, 0f));
                    usedTallHero = true;
                }
                else
                {
                    CreateCrateVisual(detailRoot.transform, obstacle.position, new Vector3(2.35f, 2.55f, 0.9f),
                        palette);
                }
            }
            else if (obstacle.name == "Laser Hazard")
            {
                Renderer renderer = obstacle.GetComponent<Renderer>();
                bool fullWidth = obstacle.localScale.x > 5f;
                if (!usedLaserHero && !fullWidth)
                {
                    if (renderer != null)
                    {
                        renderer.enabled = false;
                    }

                    InstantiateDetailedModel(Laser02Path, detailRoot.transform,
                        new Vector3(obstacle.position.x, 1.35f, obstacle.position.z),
                        new Vector3(2.75f, 2.7f, 1.0f), Quaternion.Euler(0f, 90f, 0f));
                    usedLaserHero = true;
                }
                else
                {
                    if (renderer != null)
                    {
                        renderer.enabled = true;
                        renderer.sharedMaterial = palette.RedEmission;
                    }

                    float halfWidth = obstacle.localScale.x * 0.5f + 0.2f;
                    CreateLaserPost(detailRoot.transform,
                        new Vector3(obstacle.position.x - halfWidth, 1.25f, obstacle.position.z), palette);
                    CreateLaserPost(detailRoot.transform,
                        new Vector3(obstacle.position.x + halfWidth, 1.25f, obstacle.position.z), palette);
                }
            }
            else if (obstacle.name == "Security Robot")
            {
                foreach (Renderer renderer in obstacle.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.sharedMaterial = renderer.gameObject.name == "Red Eye"
                        ? palette.RedEmission
                        : renderer.gameObject.name == "Body"
                            ? palette.WhitePanel
                            : palette.DarkMetal;
                }
            }
        }

        ApplyHeroDoor(GameObject.Find("Gameplay Objects/Tutorial Door"), palette);
        ApplyProceduralDoor(GameObject.Find("Gameplay Objects/Containment Door"), palette);

        foreach (InteractableObject interactable in gameplay.GetComponentsInChildren<InteractableObject>(true))
        {
            Renderer renderer = interactable.GetComponentInChildren<Renderer>(true);
            if (renderer != null)
            {
                renderer.sharedMaterial = palette.Screen;
            }
        }
    }

    private static void CreateCrateVisual(Transform parent, Vector3 center, Vector3 size, Palette palette)
    {
        GameObject crate = new GameObject("Aurora Cargo Visual");
        crate.transform.SetParent(parent);
        crate.transform.position = center;

        Box("Crate Core", crate.transform, Vector3.zero, size, Quaternion.identity, palette.WhitePanel, false);
        Box("Crate Base", crate.transform, new Vector3(0f, -size.y * 0.42f, 0f),
            new Vector3(size.x * 1.04f, size.y * 0.18f, size.z * 1.08f), Quaternion.identity,
            palette.DarkMetal, false);
        Box("Crate Side L", crate.transform, new Vector3(-size.x * 0.46f, 0f, 0f),
            new Vector3(size.x * 0.12f, size.y * 0.92f, size.z * 1.04f), Quaternion.identity,
            palette.DarkMetal, false);
        Box("Crate Side R", crate.transform, new Vector3(size.x * 0.46f, 0f, 0f),
            new Vector3(size.x * 0.12f, size.y * 0.92f, size.z * 1.04f), Quaternion.identity,
            palette.DarkMetal, false);
        Box("Crate Front Glow", crate.transform, new Vector3(0f, -size.y * 0.18f, -size.z * 0.53f),
            new Vector3(size.x * 0.58f, 0.08f, 0.04f), Quaternion.identity, palette.CyanEmission, false);
        Box("Crate Hazard L", crate.transform, new Vector3(-size.x * 0.34f, size.y * 0.22f, -size.z * 0.53f),
            new Vector3(size.x * 0.16f, size.y * 0.22f, 0.045f), Quaternion.identity, palette.Hazard, false);
        Box("Crate Hazard R", crate.transform, new Vector3(size.x * 0.34f, size.y * 0.22f, -size.z * 0.53f),
            new Vector3(size.x * 0.16f, size.y * 0.22f, 0.045f), Quaternion.identity, palette.Hazard, false);
    }

    private static void CreateLaserPost(Transform parent, Vector3 center, Palette palette)
    {
        GameObject post = new GameObject("Laser Emitter Post");
        post.transform.SetParent(parent);
        post.transform.position = center;

        Box("Emitter Base", post.transform, new Vector3(0f, -1.02f, 0f), new Vector3(0.72f, 0.3f, 0.78f),
            Quaternion.identity, palette.DarkMetal, false);
        Box("Emitter Body", post.transform, Vector3.zero, new Vector3(0.58f, 2.15f, 0.62f),
            Quaternion.identity, palette.WhitePanel, false);
        Box("Emitter Spine", post.transform, new Vector3(0f, 0f, -0.33f), new Vector3(0.28f, 1.55f, 0.08f),
            Quaternion.identity, palette.DarkMetal, false);
        Sphere("Emitter Lens", post.transform, new Vector3(0f, 0.35f, -0.4f), new Vector3(0.34f, 0.34f, 0.18f),
            Quaternion.identity, palette.RedEmission);
        Box("Emitter Status", post.transform, new Vector3(0f, -0.42f, -0.39f), new Vector3(0.12f, 0.48f, 0.08f),
            Quaternion.identity, palette.RedEmission, false);
    }

    private static void ApplyHeroDoor(GameObject door, Palette palette)
    {
        if (door == null)
        {
            return;
        }

        DisableRenderer(door);
        door.transform.localScale = Vector3.one;
        InstantiateDetailedModel(DoorPath, door.transform, Vector3.zero, new Vector3(9f, 4.2f, 0.72f),
            Quaternion.Euler(0f, 90f, 0f), true);
    }

    private static void ApplyProceduralDoor(GameObject door, Palette palette)
    {
        if (door == null)
        {
            return;
        }

        DisableRenderer(door);
        door.transform.localScale = Vector3.one;
        for (int side = -1; side <= 1; side += 2)
        {
            Box("Door Pillar", door.transform, new Vector3(side * 4.15f, 0f, 0f),
                new Vector3(0.72f, 4.4f, 0.8f), Quaternion.identity, palette.WhitePanel, false);
            Box("Door Inner", door.transform, new Vector3(side * 3.72f, 0f, -0.05f),
                new Vector3(0.22f, 3.6f, 0.86f), Quaternion.identity, palette.DarkMetal, false);
            Box("Door Glow", door.transform, new Vector3(side * 3.58f, 0f, -0.48f),
                new Vector3(0.07f, 2.55f, 0.06f), Quaternion.identity, palette.CyanEmission, false);
        }

        Box("Door Header", door.transform, new Vector3(0f, 1.8f, 0f), new Vector3(7.75f, 0.82f, 0.82f),
            Quaternion.identity, palette.DarkMetal, false);
        Box("Door Header Panel", door.transform, new Vector3(0f, 2.1f, -0.03f), new Vector3(5.2f, 0.35f, 0.86f),
            Quaternion.identity, palette.WhitePanel, false);
        Box("Door Header Glow", door.transform, new Vector3(0f, 1.37f, -0.47f), new Vector3(4.4f, 0.09f, 0.06f),
            Quaternion.identity, palette.CyanEmission, false);
    }

    private static GameObject InstantiateDetailedModel(string path, Transform parent, Vector3 position,
        Vector3 targetSize, Quaternion rotation, bool localPosition = false)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning("PROJETO:AURORA - Modelo nao encontrado: " + path);
            return null;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
        {
            return null;
        }

        instance.name = Path.GetFileName(Path.GetDirectoryName(path)) + " Visual";
        instance.transform.SetParent(parent, false);
        if (localPosition)
        {
            instance.transform.localPosition = position;
            instance.transform.localRotation = rotation;
        }
        else
        {
            instance.transform.position = position;
            instance.transform.rotation = rotation;
        }

        Bounds sourceBounds = CalculatePrefabBounds(prefab);
        instance.transform.localScale = new Vector3(
            targetSize.z / Mathf.Max(0.001f, sourceBounds.size.x),
            targetSize.y / Mathf.Max(0.001f, sourceBounds.size.y),
            targetSize.x / Mathf.Max(0.001f, sourceBounds.size.z));
        SetStaticRecursive(instance);
        return instance;
    }

    private static Bounds CalculatePrefabBounds(GameObject prefab)
    {
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Bounds(Vector3.zero, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private static void ConfigureLightingAndCamera()
    {
        DestroyByName("Fase01 - Lighting");
        GameObject lighting = new GameObject("Fase01 - Lighting");

        GameObject sunObject = new GameObject("Aurora Directional Fill");
        sunObject.transform.SetParent(lighting.transform);
        sunObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(0.62f, 0.76f, 0.92f);
        sun.intensity = 0.72f;
        sun.shadows = LightShadows.Hard;

        for (float z = 22.5f; z < 450f; z += 45f)
        {
            GameObject lightObject = new GameObject("Laboratory Ceiling Light");
            lightObject.transform.SetParent(lighting.transform);
            lightObject.transform.position = new Vector3(0f, 6.65f, z);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = z % 90f < 1f ? new Color(0.42f, 0.86f, 1f) : new Color(0.82f, 0.9f, 1f);
            light.intensity = 850f;
            light.range = 19f;
            light.shadows = LightShadows.None;
        }

        VolumeProfile profile = CreateVolumeProfile();
        GameObject volumeObject = new GameObject("Fase01 Global Volume");
        volumeObject.transform.SetParent(lighting.transform);
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.sharedProfile = profile;

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.25f, 0.32f, 0.4f);
        RenderSettings.ambientEquatorColor = new Color(0.13f, 0.18f, 0.23f);
        RenderSettings.ambientGroundColor = new Color(0.07f, 0.09f, 0.12f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.055f, 0.11f, 0.16f);
        RenderSettings.fogStartDistance = 105f;
        RenderSettings.fogEndDistance = 285f;

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.fieldOfView = 58f;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 420f;
            camera.allowHDR = true;
            camera.backgroundColor = new Color(0.012f, 0.025f, 0.042f);
            UniversalAdditionalCameraData data = camera.GetComponent<UniversalAdditionalCameraData>();
            if (data == null)
            {
                data = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }

            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.dithering = true;

            CameraFollow follow = camera.GetComponent<CameraFollow>();
            if (follow != null)
            {
                follow.offset = new Vector3(0f, 4.6f, -9.5f);
                follow.positionSmooth = 9f;
                follow.rotationSmooth = 8f;
            }
        }

        int quality = Mathf.Min(3, QualitySettings.names.Length - 1);
        if (quality >= 0)
        {
            QualitySettings.SetQualityLevel(quality, true);
        }
        QualitySettings.shadowDistance = 70f;
    }

    private static VolumeProfile CreateVolumeProfile()
    {
        if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath) != null)
        {
            AssetDatabase.DeleteAsset(VolumePath);
        }

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, VolumePath);

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.62f);
        bloom.intensity.Override(0.72f);
        bloom.scatter.Override(0.68f);
        bloom.highQualityFiltering.Override(true);

        ColorAdjustments color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(0.32f);
        color.contrast.Override(12f);
        color.saturation.Override(-6f);
        color.colorFilter.Override(new Color(0.9f, 0.96f, 1f));

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.2f);
        vignette.smoothness.Override(0.52f);
        vignette.rounded.Override(true);

        Tonemapping tonemapping = profile.Add<Tonemapping>(true);
        tonemapping.mode.Override(TonemappingMode.ACES);

        WhiteBalance whiteBalance = profile.Add<WhiteBalance>(true);
        whiteBalance.temperature.Override(-7f);
        whiteBalance.tint.Override(-2f);

        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static GameObject Box(string name, Transform parent, Vector3 localPosition, Vector3 scale,
        Quaternion localRotation, Material material, bool keepCollider, bool castShadows = true)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        go.transform.localScale = scale;
        Renderer renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        renderer.receiveShadows = castShadows;
        if (!keepCollider)
        {
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        SetStatic(go);
        return go;
    }

    private static GameObject Cylinder(string name, Transform parent, Vector3 localPosition, Vector3 scale,
        Quaternion localRotation, Material material, bool castShadows = true)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        go.transform.localScale = scale;
        Renderer renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        renderer.receiveShadows = castShadows;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        SetStatic(go);
        return go;
    }

    private static GameObject Sphere(string name, Transform parent, Vector3 localPosition, Vector3 scale,
        Quaternion localRotation, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        SetStatic(go);
        return go;
    }

    private static void Pipe(string name, Transform parent, Vector3 localPosition, float radius, float length,
        Material material)
    {
        Cylinder(name, parent, localPosition, new Vector3(radius, length * 0.5f, radius),
            Quaternion.Euler(90f, 0f, 0f), material);
    }

    private static void CreateWorldText(string name, Transform parent, string text, Vector3 localPosition,
        Quaternion localRotation, float characterSize, int fontSize, Color color)
    {
        GameObject go = new GameObject(name, typeof(TextMesh));
        go.transform.SetParent(parent);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        TextMesh textMesh = go.GetComponent<TextMesh>();
        textMesh.text = text;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = characterSize;
        textMesh.fontSize = fontSize;
        textMesh.color = color;
        SetStatic(go);
    }

    private static void DisableRenderer(GameObject target)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }
    }

    private static void DestroyByName(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }
    }

    private static void SetStatic(GameObject go)
    {
        GameObjectUtility.SetStaticEditorFlags(go,
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic);
    }

    private static void SetStaticRecursive(GameObject root)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            SetStatic(child.gameObject);
        }
    }
}
