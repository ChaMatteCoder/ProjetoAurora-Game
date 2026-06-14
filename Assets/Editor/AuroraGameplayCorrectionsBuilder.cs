using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class AuroraGameplayCorrectionsBuilder
{
    private const string ProgressiveRootName = "Fase01 - Progressive Obstacles";
    private const string MaterialRoot =
        "Assets/_ProjectAurora/Art/Generated/Environment/Fase01/Materials/";

    private static readonly float[] Lanes = { -3f, 0f, 3f };

    private sealed class Palette
    {
        public Material White;
        public Material Dark;
        public Material Cyan;
        public Material Hazard;
    }

    [MenuItem("Tools/Projeto Aurora/Fase 01/Aplicar Correcoes de Gameplay")]
    public static void Build()
    {
        AuroraDrEliasCharacterBuilder.Build();

        string scenePath = FindGameplayScenePath();
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject gameplay = GameObject.Find("Gameplay Objects");
        if (gameplay == null)
        {
            throw new InvalidOperationException(
                "Gameplay Objects nao foi encontrado na Fase 01.");
        }

        Transform oldRoot = gameplay.transform.Find(ProgressiveRootName);
        if (oldRoot != null)
        {
            Object.DestroyImmediate(oldRoot.gameObject);
        }

        Palette palette = LoadPalette();
        GameObject progressionRoot = new GameObject(ProgressiveRootName);
        progressionRoot.transform.SetParent(gameplay.transform, false);

        int earlyCount = BuildEarlyStage(progressionRoot.transform, palette);
        int middleCount = BuildMiddleStage(progressionRoot.transform, palette);
        int lateCount = BuildLateStage(progressionRoot.transform, palette);

        ObstacleSpawner spawner = gameplay.GetComponent<ObstacleSpawner>();
        if (spawner != null)
        {
            spawner.authoredObstacleCount =
                gameplay.GetComponentsInChildren<Obstacle>(true).Length +
                gameplay.GetComponentsInChildren<LaserHazard>(true).Length;
            EditorUtility.SetDirty(spawner);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "PROJETO:AURORA - Correcoes aplicadas. Obstaculos extras: " +
            $"inicio={earlyCount}, meio={middleCount}, final={lateCount}. " +
            "Todas as fileiras mantem ao menos uma pista livre.");
    }

    private static int BuildEarlyStage(Transform parent, Palette palette)
    {
        Transform stage = CreateStage(parent, "Dificuldade 1 - Leitura");
        int count = 0;
        for (int row = 14; row <= 20; row += 2)
        {
            int existingLane = row % 3;
            int lane = (existingLane + 1) % 3;
            float z = RowZ(row) + 29f;
            CreateLowBarrier(stage, lane, z, palette);
            count++;
        }

        return count;
    }

    private static int BuildMiddleStage(Transform parent, Palette palette)
    {
        Transform stage = CreateStage(parent, "Dificuldade 2 - Alternancia");
        int count = 0;
        for (int row = 21; row <= 31; row += 2)
        {
            int existingLane = row % 3;
            int addedLane = (existingLane + 1) % 3;
            CreateComplementaryBarrier(
                stage,
                addedLane,
                RowZ(row),
                row,
                palette);
            count++;
        }

        for (int row = 22; row <= 30; row += 4)
        {
            int existingLane = row % 3;
            int lane = (existingLane + 2) % 3;
            CreateLowBarrier(stage, lane, RowZ(row) + 29f, palette);
            count++;
        }

        return count;
    }

    private static int BuildLateStage(Transform parent, Palette palette)
    {
        Transform stage = CreateStage(parent, "Dificuldade 3 - Contencao");
        int count = 0;
        for (int row = 32; row <= 42; row++)
        {
            int existingLane = row % 3;
            int addedLane = (existingLane + 1) % 3;
            CreateComplementaryBarrier(
                stage,
                addedLane,
                RowZ(row),
                row,
                palette);
            count++;

            if (row < 42)
            {
                int openLane = (existingLane + 2) % 3;
                float z = RowZ(row) + 29f;
                if (row % 3 == 0)
                {
                    CreateLaser(stage, openLane, z, palette);
                }
                else
                {
                    CreateLowBarrier(stage, openLane, z, palette);
                }
                count++;
            }
        }

        return count;
    }

    private static void CreateComplementaryBarrier(
        Transform parent,
        int lane,
        float z,
        int row,
        Palette palette)
    {
        bool originalIsLow = row % 7 != 3 && row % 9 != 5 && row % 3 == 0;
        if (originalIsLow || row % 4 == 0)
        {
            CreateTallBarrier(parent, lane, z, palette);
        }
        else
        {
            CreateLowBarrier(parent, lane, z, palette);
        }
    }

    private static void CreateLowBarrier(
        Transform parent,
        int lane,
        float z,
        Palette palette)
    {
        Vector3 size = new Vector3(2.2f, 0.7f, 0.9f);
        GameObject root = CreateObstacleRoot(
            "Progressive Low Barrier",
            parent,
            new Vector3(Lanes[lane], size.y * 0.5f, z),
            size);
        root.AddComponent<Obstacle>();
        CreateCrateVisual(root.transform, size, palette);
    }

    private static void CreateTallBarrier(
        Transform parent,
        int lane,
        float z,
        Palette palette)
    {
        Vector3 size = new Vector3(2.2f, 2.55f, 0.9f);
        GameObject root = CreateObstacleRoot(
            "Progressive Containment Barrier",
            parent,
            new Vector3(Lanes[lane], size.y * 0.5f, z),
            size);
        root.AddComponent<Obstacle>();
        CreateCrateVisual(root.transform, size, palette);
    }

    private static void CreateLaser(
        Transform parent,
        int lane,
        float z,
        Palette palette)
    {
        GameObject root = new GameObject("Progressive Cyan Laser");
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(Lanes[lane], 0f, z);

        BoxCollider damageCollider = root.AddComponent<BoxCollider>();
        damageCollider.center = new Vector3(0f, 0.72f, 0f);
        damageCollider.size = new Vector3(2.35f, 0.18f, 0.22f);
        damageCollider.isTrigger = true;

        LaserHazard hazard = root.AddComponent<LaserHazard>();
        hazard.damageCollider = damageCollider;
        hazard.activeColor = new Color(0.04f, 0.82f, 1f);

        CreateBox(
            "Laser Beam",
            root.transform,
            new Vector3(0f, 0.72f, 0f),
            new Vector3(2.35f, 0.12f, 0.12f),
            palette.Cyan);
        CreateLaserPost(root.transform, -1.32f, palette);
        CreateLaserPost(root.transform, 1.32f, palette);
    }

    private static GameObject CreateObstacleRoot(
        string name,
        Transform parent,
        Vector3 position,
        Vector3 size)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = position;

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.size = size;
        collider.isTrigger = true;
        return root;
    }

    private static void CreateCrateVisual(
        Transform root,
        Vector3 size,
        Palette palette)
    {
        CreateBox("White Shell", root, Vector3.zero, size * 0.94f, palette.White);
        CreateBox(
            "Dark Base",
            root,
            new Vector3(0f, -size.y * 0.42f, 0f),
            new Vector3(size.x, size.y * 0.16f, size.z * 1.04f),
            palette.Dark);
        CreateBox(
            "Dark Left Brace",
            root,
            new Vector3(-size.x * 0.44f, 0f, 0f),
            new Vector3(size.x * 0.12f, size.y * 0.9f, size.z * 1.02f),
            palette.Dark);
        CreateBox(
            "Dark Right Brace",
            root,
            new Vector3(size.x * 0.44f, 0f, 0f),
            new Vector3(size.x * 0.12f, size.y * 0.9f, size.z * 1.02f),
            palette.Dark);
        CreateBox(
            "Cyan Status",
            root,
            new Vector3(0f, -size.y * 0.16f, -size.z * 0.52f),
            new Vector3(size.x * 0.58f, 0.07f, 0.04f),
            palette.Cyan);
        CreateBox(
            "Hazard Mark",
            root,
            new Vector3(0f, size.y * 0.24f, -size.z * 0.525f),
            new Vector3(size.x * 0.42f, Mathf.Min(0.25f, size.y * 0.28f), 0.035f),
            palette.Hazard);
    }

    private static void CreateLaserPost(
        Transform parent,
        float localX,
        Palette palette)
    {
        CreateBox(
            "Emitter Base",
            parent,
            new Vector3(localX, 0.15f, 0f),
            new Vector3(0.58f, 0.3f, 0.62f),
            palette.Dark);
        CreateBox(
            "Emitter Body",
            parent,
            new Vector3(localX, 0.75f, 0f),
            new Vector3(0.42f, 1.15f, 0.44f),
            palette.White);
        CreateBox(
            "Emitter Glow",
            parent,
            new Vector3(localX, 0.72f, -0.24f),
            new Vector3(0.18f, 0.28f, 0.06f),
            palette.Cyan);
    }

    private static GameObject CreateBox(
        string name,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPosition;
        box.transform.localRotation = Quaternion.identity;
        box.transform.localScale = localScale;

        Object.DestroyImmediate(box.GetComponent<Collider>());
        Renderer renderer = box.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        return box;
    }

    private static Transform CreateStage(Transform parent, string name)
    {
        GameObject stage = new GameObject(name);
        stage.transform.SetParent(parent, false);
        return stage.transform;
    }

    private static float RowZ(int row)
    {
        return 90f + row * 58f;
    }

    private static Palette LoadPalette()
    {
        Palette palette = new Palette
        {
            White = LoadMaterial("M_F01_WhitePanel.mat"),
            Dark = LoadMaterial("M_F01_DarkMetal.mat"),
            Cyan = LoadMaterial("M_F01_CyanEmission.mat"),
            Hazard = LoadMaterial("M_F01_Hazard.mat")
        };
        return palette;
    }

    private static Material LoadMaterial(string fileName)
    {
        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot + fileName);
        if (material == null)
        {
            throw new InvalidOperationException(
                "Material da Fase 01 nao encontrado: " + fileName);
        }

        return material;
    }

    private static string FindGameplayScenePath()
    {
        string path = AssetDatabase.FindAssets(
                "Fase01_SetorA_LaboratorioLimpo t:Scene")
            .Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault();
        if (string.IsNullOrEmpty(path))
        {
            throw new InvalidOperationException(
                "Cena Fase01_SetorA_LaboratorioLimpo nao encontrada.");
        }

        return path;
    }
}
