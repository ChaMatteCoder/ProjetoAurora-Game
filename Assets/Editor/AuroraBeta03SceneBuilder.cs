using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class AuroraBeta03SceneBuilder
{
    private const string MainMenuScenePath =
        "Assets/_ProjectAurora/Scenes/MainMenu.unity";
    private const string IntegratedScenePath =
        "Assets/_ProjectAurora/Scenes/Beta03_Principal.unity";
    private const float IntegratedTerminalOffset = 2593f;
    private const string GeneratedRoot =
        "Assets/_ProjectAurora/Art/Generated/Environment/Fase05";
    private const string MaterialRoot = GeneratedRoot + "/Materials";
    private const string VolumePath = GeneratedRoot + "/Fase05_Volume.asset";
    private const string Box01Path =
        "Assets/_ProjectAurora/Art/Generated/Obstacles/Aurora_Box_01/modelo.glb";
    private const string Box02Path =
        "Assets/_ProjectAurora/Art/Generated/Obstacles/Aurora_Box_02/modelo.glb";
    private const string DoorPath =
        "Assets/_ProjectAurora/Art/Generated/Obstacles/Aurora_Door_01/modelo.glb";
    private const string Laser01Path =
        "Assets/_ProjectAurora/Art/Generated/Obstacles/Aurora_Lazer_01/modelo.glb";
    private const string Laser02Path =
        "Assets/_ProjectAurora/Art/Generated/Obstacles/Aurora_Lazer_02/modelo.glb";

    private static readonly float[] Lanes = { -3f, 0f, 3f };
    private static readonly string ProjectRoot =
        Path.GetDirectoryName(Application.dataPath);
    private static readonly string RequestPath =
        Path.Combine(ProjectRoot, "Temp", "AuroraBeta03Build.request");
    private static readonly string ResultPath =
        Path.Combine(ProjectRoot, "Temp", "AuroraBeta03Build.result");
    private static readonly string PreviewRoot =
        Path.Combine(ProjectRoot, "Temp", "AuroraBeta03");

    private sealed class Palette
    {
        public Material White;
        public Material Dark;
        public Material Floor;
        public Material FloorInset;
        public Material Cyan;
        public Material Red;
        public Material Glass;
        public Material Screen;
        public Material Hazard;
    }

    static AuroraBeta03SceneBuilder()
    {
        if (File.Exists(RequestPath))
        {
            EditorApplication.delayCall += RunRequestedBuild;
        }
    }

    [MenuItem("Tools/Projeto Aurora/Beta 0.3/Build Scene Passes")]
    public static void BuildScenePasses()
    {
        RunRequestedBuild();
    }

    private static void RunRequestedBuild()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += RunRequestedBuild;
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            WriteResult("FAILED: Unity is in Play Mode.");
            return;
        }

        if (File.Exists(RequestPath))
        {
            File.Delete(RequestPath);
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid() && activeScene.isDirty)
        {
            WriteResult(
                "FAILED: active scene has unsaved changes: " + activeScene.path);
            Debug.LogError(
                "PROJETO:AURORA - Build cancelado porque a cena ativa possui alteracoes nao salvas.");
            return;
        }

        try
        {
            string sourcePath = FindSourceScene();
            string firstPassPath = BuildFirstPass(sourcePath);
            string terminalPath = BuildTerminalScene(sourcePath);
            string integratedPath = BuildIntegratedBeta(sourcePath);

            ValidateFirstPass(firstPassPath);
            ValidateTerminal(terminalPath);
            ValidateIntegratedBeta(integratedPath);
            ConfigureBuildScenes(
                integratedPath,
                firstPassPath,
                terminalPath);
            EditorSceneManager.OpenScene(integratedPath, OpenSceneMode.Single);

            string result =
                "SUCCESS" + Environment.NewLine +
                "BETA=" + integratedPath + Environment.NewLine +
                "FIRST_PASS=" + firstPassPath + Environment.NewLine +
                "TERMINAL=" + terminalPath + Environment.NewLine +
                "PREVIEW_FIRST=" +
                Path.Combine(PreviewRoot, "Fase01_FirstPass.png") +
                Environment.NewLine +
                "PREVIEW_TERMINAL=" +
                Path.Combine(PreviewRoot, "Fase05_TerminalCentral.png");
            WriteResult(result);
            Debug.Log("PROJETO:AURORA - Cenas Beta 0.3 geradas e validadas.");
        }
        catch (Exception exception)
        {
            WriteResult("FAILED: " + exception);
            Debug.LogException(exception);
        }
    }

    private static string BuildFirstPass(string sourcePath)
    {
        string directory = Path.GetDirectoryName(sourcePath).Replace('\\', '/');
        string outputPath =
            directory + "/Fase01_LaboratorioLimpo_FirstPass.unity";

        Scene source = EditorSceneManager.OpenScene(sourcePath, OpenSceneMode.Single);
        if (!EditorSceneManager.SaveScene(source, outputPath, true))
        {
            throw new InvalidOperationException(
                "Nao foi possivel copiar a cena da Fase 01.");
        }

        Scene scene = EditorSceneManager.OpenScene(outputPath, OpenSceneMode.Single);
        CullFirstPassEnvironment();
        RebuildFirstPassObstacles();
        ConfigureFirstPassGameplay();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, outputPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        CapturePreview(
            Path.Combine(PreviewRoot, "Fase01_FirstPass.png"),
            new Vector3(0f, 3.8f, 72f),
            new Vector3(0f, 1.8f, 175f),
            62f);
        return outputPath;
    }

    private static void CullFirstPassEnvironment()
    {
        GameObject environment =
            FindSceneObject("Fase 01 - Aurora Research Corridor");
        if (environment == null)
        {
            throw new InvalidOperationException(
                "Ambiente detalhado da Fase 01 nao encontrado.");
        }

        foreach (Transform sector in environment.transform
                     .Cast<Transform>()
                     .ToArray())
        {
            if (!sector.name.StartsWith(
                    "Setor A",
                    StringComparison.OrdinalIgnoreCase))
            {
                Object.DestroyImmediate(sector.gameObject);
            }
        }

        DestroySceneObject("Fase01 - Detailed Obstacles");
        DestroySceneObject("Environment - 2700m");
    }

    private static void RebuildFirstPassObstacles()
    {
        GameObject gameplay = FindSceneObject("Gameplay Objects");
        if (gameplay == null)
        {
            throw new InvalidOperationException(
                "Gameplay Objects nao encontrado na Fase 01.");
        }

        HashSet<GameObject> oldHazards = new HashSet<GameObject>();
        foreach (Obstacle obstacle in gameplay.GetComponentsInChildren<Obstacle>(true))
        {
            if (obstacle.transform.position.z >= 60f)
            {
                oldHazards.Add(obstacle.gameObject);
            }
        }
        foreach (LaserHazard laser in gameplay.GetComponentsInChildren<LaserHazard>(true))
        {
            if (laser.transform.position.z >= 60f)
            {
                oldHazards.Add(laser.gameObject);
            }
        }
        foreach (GameObject oldHazard in oldHazards)
        {
            Object.DestroyImmediate(oldHazard);
        }

        foreach (Transform child in gameplay.transform.Cast<Transform>().ToArray())
        {
            if (child.name == "Fase01 - Progressive Obstacles" ||
                child.position.z > 440f ||
                child.name.Contains("Terminal Central") ||
                child.name == "Terminal Core" ||
                child.name == "Aurora Core")
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        BuildCuratedFirstSectorObstacles(gameplay, true);
        UpdateAuthoredObstacleCount(gameplay);
    }

    private static void BuildCuratedFirstSectorObstacles(
        GameObject gameplay,
        bool addPrototypeEnd)
    {
        Material cyan = LoadMaterial(
            "Assets/_ProjectAurora/Art/Generated/Environment/Fase01/Materials/" +
            "M_F01_CyanEmission.mat");
        Material red = LoadMaterial(
            "Assets/_ProjectAurora/Art/Generated/Environment/Fase01/Materials/" +
            "M_F01_RedEmission.mat");
        Material dark = LoadMaterial(
            "Assets/_ProjectAurora/Art/Generated/Environment/Fase01/Materials/" +
            "M_F01_DarkMetal.mat");

        GameObject root = new GameObject("Fase01 - Curated Obstacle Pass");
        root.transform.SetParent(gameplay.transform, false);

        CreateObstacle(root.transform, 0, 90f, false, Box02Path);
        CreateObstacle(root.transform, 2, 125f, true, Box01Path);
        CreateLaser(root.transform, 1, 160f, Laser02Path, cyan);
        CreateObstacle(root.transform, 0, 205f, true, Box01Path);
        CreateObstacle(root.transform, 1, 205f, false, Box02Path);
        CreateObstacle(root.transform, 0, 250f, false, Box02Path);
        CreateLaser(root.transform, 2, 250f, Laser01Path, red);
        CreateObstacle(root.transform, 1, 300f, true, Box01Path);
        CreateObstacle(root.transform, 2, 300f, false, Box02Path);
        CreateLaser(root.transform, 0, 350f, Laser02Path, cyan);
        CreateObstacle(root.transform, 2, 350f, true, Box01Path);
        CreateObstacle(root.transform, 1, 400f, false, Box02Path);

        if (!addPrototypeEnd)
        {
            return;
        }

        InstantiateVisual(
            DoorPath,
            root.transform,
            new Vector3(0f, 0f, 438f),
            new Vector3(10f, 5.2f, 1f),
            Quaternion.Euler(0f, 90f, 0f),
            "Fase01 End Gate");
        Box(
            "End Gate Backing",
            root.transform,
            new Vector3(0f, 2.6f, 440f),
            new Vector3(10f, 5.2f, 0.45f),
            Quaternion.identity,
            dark,
            true);

        GameObject endTrigger = new GameObject("First Pass End Trigger");
        endTrigger.transform.SetParent(root.transform, false);
        endTrigger.transform.position = new Vector3(0f, 0f, 424f);
        BoxCollider trigger = endTrigger.AddComponent<BoxCollider>();
        trigger.center = new Vector3(0f, 1.5f, 0f);
        trigger.size = new Vector3(9f, 3f, 3f);
        trigger.isTrigger = true;
        endTrigger.AddComponent<PrototypeSliceEndTrigger>();
    }

    private static void UpdateAuthoredObstacleCount(GameObject gameplay)
    {
        ObstacleSpawner spawner = gameplay.GetComponent<ObstacleSpawner>();
        if (spawner != null)
        {
            spawner.authoredObstacleCount =
                gameplay.GetComponentsInChildren<Obstacle>(true).Length +
                gameplay.GetComponentsInChildren<LaserHazard>(true).Length;
            EditorUtility.SetDirty(spawner);
        }
    }

    private static void ConfigureFirstPassGameplay()
    {
        GameManager manager = Object.FindAnyObjectByType<GameManager>();
        PlayerRunner player = Object.FindAnyObjectByType<PlayerRunner>();
        Camera camera = Camera.main;
        if (manager == null || player == null || camera == null)
        {
            throw new InvalidOperationException(
                "Sistemas principais ausentes na copia da Fase 01.");
        }

        manager.finishDistance = 425f;
        manager.terminalSequencePreview = false;
        player.transform.position = Vector3.zero;
        player.speedRampDistance = 425f;
        camera.transform.position = player.transform.position +
                                    new Vector3(0f, 4.6f, -9.5f);
        camera.transform.LookAt(player.transform.position +
                                new Vector3(0f, 1.3f, 8f));
        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(player);
        EditorUtility.SetDirty(camera);
    }

    private static string BuildTerminalScene(string sourcePath)
    {
        EnsureFolders();
        Palette palette = CreateTerminalPalette();
        string terminalDirectory =
            FindSceneSubfolder("FASE 05 - Terminal Central");
        string outputPath =
            terminalDirectory + "/Fase05_TerminalCentral_Beta03.unity";

        Scene source = EditorSceneManager.OpenScene(sourcePath, OpenSceneMode.Single);
        if (!EditorSceneManager.SaveScene(source, outputPath, true))
        {
            throw new InvalidOperationException(
                "Nao foi possivel copiar a base para a Fase 05.");
        }

        Scene scene = EditorSceneManager.OpenScene(outputPath, OpenSceneMode.Single);
        StripGameplayEnvironment();
        TerminalFinalePresentation presentation =
            BuildTerminalEnvironment(palette);
        ConfigureTerminalGameplay(presentation);
        ConfigureTerminalLighting();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, outputPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        CapturePreview(
            Path.Combine(PreviewRoot, "Fase05_TerminalCentral.png"),
            new Vector3(0f, 4.2f, 64f),
            new Vector3(0f, 4.8f, 103f),
            66f);
        return outputPath;
    }

    private static string BuildIntegratedBeta(string sourcePath)
    {
        EnsureFolders();
        Palette palette = CreateTerminalPalette();

        Scene source = EditorSceneManager.OpenScene(sourcePath, OpenSceneMode.Single);
        if (!EditorSceneManager.SaveScene(source, IntegratedScenePath, true))
        {
            throw new InvalidOperationException(
                "Nao foi possivel criar a cena principal da Beta 0.3.");
        }

        Scene scene =
            EditorSceneManager.OpenScene(IntegratedScenePath, OpenSceneMode.Single);
        RefreshIntegratedFirstSectorObstacles();
        TerminalFinalePresentation presentation =
            ReplaceIntegratedTerminal(palette);
        ConfigureIntegratedGameplay(presentation);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, IntegratedScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        CapturePreview(
            Path.Combine(PreviewRoot, "Beta03_Principal_Terminal.png"),
            new Vector3(0f, 5.2f, 2638f),
            new Vector3(0f, 4.8f, 2696f),
            64f);
        return IntegratedScenePath;
    }

    private static void RefreshIntegratedFirstSectorObstacles()
    {
        GameObject gameplay = FindSceneObject("Gameplay Objects");
        if (gameplay == null)
        {
            throw new InvalidOperationException(
                "Gameplay Objects nao encontrado para a Beta 0.3.");
        }

        DestroySceneObject("Fase01 - Curated Obstacle Pass");
        HashSet<GameObject> oldHazards = new HashSet<GameObject>();
        foreach (Obstacle obstacle in Object.FindObjectsByType<Obstacle>(
                     FindObjectsInactive.Include))
        {
            float z = obstacle.transform.position.z;
            if (z >= 60f && z <= 430f)
            {
                oldHazards.Add(obstacle.gameObject);
            }
        }
        foreach (LaserHazard laser in Object.FindObjectsByType<LaserHazard>(
                     FindObjectsInactive.Include))
        {
            float z = laser.transform.position.z;
            if (z >= 60f && z <= 430f)
            {
                oldHazards.Add(laser.gameObject);
            }
        }
        foreach (GameObject oldHazard in oldHazards)
        {
            Object.DestroyImmediate(oldHazard);
        }

        BuildCuratedFirstSectorObstacles(gameplay, false);
        UpdateAuthoredObstacleCount(gameplay);
    }

    private static TerminalFinalePresentation ReplaceIntegratedTerminal(
        Palette palette)
    {
        DestroySceneObject("Terminal Central");
        DestroySceneObject("Terminal Central Access");
        DestroySceneObject("Terminal Core");
        DestroySceneObject("Aurora Core");

        HashSet<GameObject> finalHazards = new HashSet<GameObject>();
        foreach (Obstacle obstacle in Object.FindObjectsByType<Obstacle>(
                     FindObjectsInactive.Include))
        {
            if (obstacle.transform.position.z >= 2550f)
            {
                finalHazards.Add(obstacle.gameObject);
            }
        }
        foreach (LaserHazard laser in Object.FindObjectsByType<LaserHazard>(
                     FindObjectsInactive.Include))
        {
            if (laser.transform.position.z >= 2550f)
            {
                finalHazards.Add(laser.gameObject);
            }
        }
        foreach (GameObject finalHazard in finalHazards)
        {
            Object.DestroyImmediate(finalHazard);
        }

        TerminalFinalePresentation presentation =
            BuildTerminalEnvironment(palette);
        GameObject terminalRoot = FindSceneObject("Fase05 - Terminal Central");
        BuildIntegratedTerminalLeadIn(terminalRoot.transform, palette);
        BuildIntegratedTerminalLighting(terminalRoot.transform);
        terminalRoot.transform.position =
            new Vector3(0f, 0f, IntegratedTerminalOffset);

        GameObject gameplay = FindSceneObject("Gameplay Objects");
        if (gameplay != null)
        {
            UpdateAuthoredObstacleCount(gameplay);
        }
        return presentation;
    }

    private static void BuildIntegratedTerminalLeadIn(
        Transform parent,
        Palette palette)
    {
        const float start = -343f;
        const float end = -16f;
        float center = (start + end) * 0.5f;
        float length = end - start;

        GameObject leadIn = new GameObject("Terminal Lead-In - Sector 05");
        leadIn.transform.SetParent(parent, false);
        Box("Structural Floor", leadIn.transform, new Vector3(0f, -0.3f, center),
            new Vector3(14f, 0.6f, length), Quaternion.identity,
            palette.Dark, true);
        Box("Track Surface", leadIn.transform, new Vector3(0f, 0.02f, center),
            new Vector3(9.5f, 0.06f, length), Quaternion.identity,
            palette.Floor, false);
        for (int lane = 0; lane < 3; lane++)
        {
            Box(
                "Lane " + (lane + 1),
                leadIn.transform,
                new Vector3(Lanes[lane], 0.065f, center),
                new Vector3(2.72f, 0.035f, length),
                Quaternion.identity,
                palette.FloorInset,
                false);
        }

        Box("Lane Divider L", leadIn.transform, new Vector3(-1.5f, 0.1f, center),
            new Vector3(0.06f, 0.03f, length), Quaternion.identity,
            palette.Cyan, false);
        Box("Lane Divider R", leadIn.transform, new Vector3(1.5f, 0.1f, center),
            new Vector3(0.06f, 0.03f, length), Quaternion.identity,
            palette.Cyan, false);
        Box("Wall L", leadIn.transform, new Vector3(-7.2f, 3.8f, center),
            new Vector3(0.5f, 7.6f, length), Quaternion.identity,
            palette.Dark, true);
        Box("Wall R", leadIn.transform, new Vector3(7.2f, 3.8f, center),
            new Vector3(0.5f, 7.6f, length), Quaternion.identity,
            palette.Dark, true);
        Box("Ceiling", leadIn.transform, new Vector3(0f, 7.55f, center),
            new Vector3(14.5f, 0.35f, length), Quaternion.identity,
            palette.Dark, false);

        for (float z = start + 14f; z <= end - 8f; z += 28f)
        {
            CreateTerminalArch(leadIn.transform, z, 5.7f, 7.1f, palette);
        }
    }

    private static void BuildIntegratedTerminalLighting(Transform parent)
    {
        GameObject lighting = new GameObject("Fase05 - Integrated Lighting");
        lighting.transform.SetParent(parent, false);

        for (float z = -320f; z <= 108f; z += 28f)
        {
            GameObject lightObject =
                new GameObject("Terminal Route Light");
            lightObject.transform.SetParent(lighting.transform, false);
            lightObject.transform.position = new Vector3(
                z % 56f < 1f ? -3.5f : 3.5f,
                z < 60f ? 6.6f : 8.6f,
                z);
            Light sceneLight = lightObject.AddComponent<Light>();
            sceneLight.type = LightType.Point;
            sceneLight.color = z % 56f < 1f
                ? new Color(0.05f, 0.7f, 1f)
                : new Color(1f, 0.025f, 0.035f);
            sceneLight.intensity = z < 60f ? 900f : 1900f;
            sceneLight.range = z < 60f ? 16f : 22f;
            sceneLight.shadows = LightShadows.None;
        }

        for (float z = 70f; z <= 106f; z += 18f)
        {
            GameObject chamberLightObject =
                new GameObject("Integrated Chamber Fill");
            chamberLightObject.transform.SetParent(lighting.transform, false);
            chamberLightObject.transform.position = new Vector3(0f, 7.4f, z);
            Light chamberLight = chamberLightObject.AddComponent<Light>();
            chamberLight.type = LightType.Point;
            chamberLight.color = new Color(0.62f, 0.8f, 1f);
            chamberLight.intensity = 2200f;
            chamberLight.range = 24f;
            chamberLight.shadows = LightShadows.None;
        }
    }

    private static void ConfigureIntegratedGameplay(
        TerminalFinalePresentation presentation)
    {
        PlayerRunner player = Object.FindAnyObjectByType<PlayerRunner>();
        GameManager manager = Object.FindAnyObjectByType<GameManager>();
        if (player == null || manager == null)
        {
            throw new InvalidOperationException(
                "Player ou GameManager ausente na Beta 0.3 principal.");
        }

        manager.terminalSequencePreview = false;
        manager.finishDistance = 2700f;

        FinalCutsceneController finalController =
            manager.GetComponent<FinalCutsceneController>();
        if (finalController == null)
        {
            finalController =
                manager.gameObject.AddComponent<FinalCutsceneController>();
        }
        finalController.dialogue = manager.dialogue;
        finalController.player = player;
        finalController.celestIAHud = manager.sectors == null
            ? Object.FindAnyObjectByType<CelestIAHudController>()
            : manager.sectors.celestIAHud;
        finalController.presentation = presentation;
        manager.finalCutscene = finalController;

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(finalController);
    }

    private static void StripGameplayEnvironment()
    {
        string[] names =
        {
            "Fase 01 - Aurora Research Corridor",
            "Environment - 2700m",
            "Gameplay Objects",
            "Fase01 - Detailed Obstacles",
            "Fase01 - Lighting"
        };
        foreach (string name in names)
        {
            DestroySceneObject(name);
        }
    }

    private static TerminalFinalePresentation BuildTerminalEnvironment(
        Palette palette)
    {
        GameObject root = new GameObject("Fase05 - Terminal Central");
        BuildApproach(root.transform, palette);
        BuildTerminalChamber(root.transform, palette);
        BuildTerminalSetDressing(root.transform, palette);

        GameObject staging = new GameObject("CUTSCENE STAGING - Beta 0.3");
        staging.transform.SetParent(root.transform, false);
        Transform panelFocus = Marker(
            "Focus - Main Panel",
            staging.transform,
            new Vector3(0f, 1.7f, 85.4f));
        Transform coreFocus = Marker(
            "Focus - Corrupted Core",
            staging.transform,
            new Vector3(0f, 5.4f, 103f));
        Transform panelShot = Marker(
            "Shot 01 - Panel Close",
            staging.transform,
            new Vector3(4.8f, 2.8f, 77f));
        Transform coreShot = Marker(
            "Shot 02 - Core Reveal",
            staging.transform,
            new Vector3(-6.8f, 5.6f, 86f));
        Marker(
            "Player Mark - Interaction",
            staging.transform,
            new Vector3(0f, 0f, 79f));

        GameObject corruptionLayer = BuildCorruptionLayer(
            root.transform,
            palette,
            out Light[] corruptedLights);
        corruptionLayer.SetActive(false);

        TerminalFinalePresentation presentation =
            staging.AddComponent<TerminalFinalePresentation>();
        presentation.panelShot = panelShot;
        presentation.panelFocus = panelFocus;
        presentation.coreShot = coreShot;
        presentation.coreFocus = coreFocus;
        presentation.corruptionLayer = corruptionLayer;
        presentation.corruptedLights = corruptedLights;
        return presentation;
    }

    private static void BuildApproach(Transform parent, Palette palette)
    {
        GameObject approach = new GameObject("Approach Corridor - Three Lanes");
        approach.transform.SetParent(parent, false);

        Box("Structural Floor", approach.transform, new Vector3(0f, -0.3f, 27f),
            new Vector3(14f, 0.6f, 86f), Quaternion.identity, palette.Dark, true);
        Box("Track Surface", approach.transform, new Vector3(0f, 0.02f, 27f),
            new Vector3(9.5f, 0.06f, 86f), Quaternion.identity, palette.Floor, false);
        for (int lane = 0; lane < 3; lane++)
        {
            Box(
                "Lane " + (lane + 1),
                approach.transform,
                new Vector3(Lanes[lane], 0.065f, 27f),
                new Vector3(2.72f, 0.035f, 86f),
                Quaternion.identity,
                palette.FloorInset,
                false);
        }

        Box("Lane Divider L", approach.transform, new Vector3(-1.5f, 0.1f, 27f),
            new Vector3(0.06f, 0.03f, 86f), Quaternion.identity, palette.Cyan, false);
        Box("Lane Divider R", approach.transform, new Vector3(1.5f, 0.1f, 27f),
            new Vector3(0.06f, 0.03f, 86f), Quaternion.identity, palette.Cyan, false);
        Box("Edge Glow L", approach.transform, new Vector3(-4.75f, 0.11f, 27f),
            new Vector3(0.11f, 0.04f, 86f), Quaternion.identity, palette.Red, false);
        Box("Edge Glow R", approach.transform, new Vector3(4.75f, 0.11f, 27f),
            new Vector3(0.11f, 0.04f, 86f), Quaternion.identity, palette.Red, false);
        Box("Wall L", approach.transform, new Vector3(-7.2f, 3.8f, 27f),
            new Vector3(0.5f, 7.6f, 86f), Quaternion.identity, palette.Dark, true);
        Box("Wall R", approach.transform, new Vector3(7.2f, 3.8f, 27f),
            new Vector3(0.5f, 7.6f, 86f), Quaternion.identity, palette.Dark, true);
        Box("Ceiling", approach.transform, new Vector3(0f, 7.55f, 27f),
            new Vector3(14.5f, 0.35f, 86f), Quaternion.identity, palette.Dark, false);

        for (float z = -10f; z <= 60f; z += 14f)
        {
            CreateTerminalArch(approach.transform, z, 5.7f, 7.1f, palette);
        }
        for (float z = -3f; z < 60f; z += 14f)
        {
            Box("Ceiling Cyan", approach.transform, new Vector3(-2.2f, 7.3f, z),
                new Vector3(2.9f, 0.08f, 0.35f), Quaternion.identity, palette.Cyan, false);
            Box("Ceiling Red", approach.transform, new Vector3(2.2f, 7.3f, z),
                new Vector3(2.9f, 0.08f, 0.35f), Quaternion.identity, palette.Red, false);
        }

        InstantiateVisual(
            DoorPath,
            approach.transform,
            new Vector3(0f, 0f, 59f),
            new Vector3(10.5f, 5.5f, 1.2f),
            Quaternion.Euler(0f, 90f, 0f),
            "Terminal Entry Gate");
    }

    private static void BuildTerminalChamber(Transform parent, Palette palette)
    {
        GameObject chamber = new GameObject("Central Chamber");
        chamber.transform.SetParent(parent, false);

        Box("Chamber Floor", chamber.transform, new Vector3(0f, -0.35f, 88f),
            new Vector3(28f, 0.7f, 58f), Quaternion.identity, palette.Dark, true);
        Box("Chamber Track", chamber.transform, new Vector3(0f, 0.02f, 84f),
            new Vector3(10f, 0.07f, 50f), Quaternion.identity, palette.Floor, false);
        Box("Side Deck L", chamber.transform, new Vector3(-9.5f, 0f, 88f),
            new Vector3(8.5f, 0.15f, 58f), Quaternion.identity, palette.FloorInset, false);
        Box("Side Deck R", chamber.transform, new Vector3(9.5f, 0f, 88f),
            new Vector3(8.5f, 0.15f, 58f), Quaternion.identity, palette.FloorInset, false);
        Box("Chamber Wall L", chamber.transform, new Vector3(-14f, 5.5f, 88f),
            new Vector3(0.55f, 11f, 58f), Quaternion.identity, palette.Dark, true);
        Box("Chamber Wall R", chamber.transform, new Vector3(14f, 5.5f, 88f),
            new Vector3(0.55f, 11f, 58f), Quaternion.identity, palette.Dark, true);
        Box("Chamber Ceiling", chamber.transform, new Vector3(0f, 11f, 88f),
            new Vector3(28f, 0.45f, 58f), Quaternion.identity, palette.Dark, false);
        Box("Back Wall", chamber.transform, new Vector3(0f, 5.5f, 117f),
            new Vector3(28f, 11f, 0.55f), Quaternion.identity, palette.Dark, true);

        for (int side = -1; side <= 1; side += 2)
        {
            Box("Wall Panel", chamber.transform, new Vector3(side * 13.65f, 5.5f, 88f),
                new Vector3(0.12f, 8.5f, 48f), Quaternion.identity, palette.White, false);
            Box("Wall Data Strip", chamber.transform, new Vector3(side * 13.5f, 5.5f, 88f),
                new Vector3(0.08f, 5.8f, 34f), Quaternion.identity,
                side < 0 ? palette.Cyan : palette.Red, false);
        }
        for (float z = 66f; z <= 108f; z += 14f)
        {
            CreateTerminalArch(chamber.transform, z, 11.5f, 10.4f, palette);
        }

        Box("Dais Step 01", chamber.transform, new Vector3(0f, 0.2f, 94f),
            new Vector3(10f, 0.4f, 8f), Quaternion.identity, palette.Dark, true);
        Box("Dais Step 02", chamber.transform, new Vector3(0f, 0.5f, 98f),
            new Vector3(8f, 0.6f, 6f), Quaternion.identity, palette.White, true);
        Box("Core Pedestal", chamber.transform, new Vector3(0f, 1.3f, 103f),
            new Vector3(6f, 2.2f, 4.5f), Quaternion.identity, palette.Dark, true);
        Cylinder("Core Base", chamber.transform, new Vector3(0f, 2.2f, 103f),
            new Vector3(3.2f, 0.65f, 3.2f), Quaternion.identity, palette.White);
        Sphere("Aurora Core", chamber.transform, new Vector3(0f, 5.4f, 103f),
            new Vector3(2.7f, 4.8f, 2.7f), Quaternion.identity, palette.Cyan);
        Box("Core Glass", chamber.transform, new Vector3(0f, 5.4f, 103f),
            new Vector3(4.4f, 8f, 4.4f), Quaternion.identity, palette.Glass, false);
        CreateCoreRing(chamber.transform, new Vector3(0f, 3.2f, 103f), 4.3f, 14, palette.White);
        CreateCoreRing(chamber.transform, new Vector3(0f, 5.5f, 103f), 4.8f, 16, palette.Cyan);
        CreateCoreRing(chamber.transform, new Vector3(0f, 7.8f, 103f), 4.3f, 14, palette.White);

        CreateMainPanel(chamber.transform, palette);
        CreateWorldText(
            "Terminal Sign",
            chamber.transform,
            "TERMINAL CENTRAL // 05",
            new Vector3(0f, 9.2f, 116.65f),
            Quaternion.Euler(0f, 180f, 0f),
            0.12f,
            52,
            new Color(0.5f, 0.95f, 1f));
    }

    private static void CreateMainPanel(Transform parent, Palette palette)
    {
        GameObject interaction = new GameObject("Terminal Central Access");
        interaction.transform.SetParent(parent, false);
        interaction.transform.position = new Vector3(0f, 0f, 82f);

        BoxCollider trigger = interaction.AddComponent<BoxCollider>();
        trigger.center = new Vector3(0f, 1.5f, 0f);
        trigger.size = new Vector3(5f, 3f, 5f);
        trigger.isTrigger = true;

        InteractableObject interactable =
            interaction.AddComponent<InteractableObject>();
        interactable.action = InteractableAction.FinalTerminal;
        interactable.prompt = "Pressione E para acessar o Terminal Central";
        interactable.message = string.Empty;

        Box("Console Base", interaction.transform, new Vector3(0f, 0.75f, 85.2f),
            new Vector3(3.8f, 1.5f, 2.2f), Quaternion.identity, palette.Dark, true);
        Box("Console Shoulder", interaction.transform, new Vector3(0f, 1.65f, 85.05f),
            new Vector3(3.2f, 0.45f, 1.5f), Quaternion.Euler(18f, 0f, 0f),
            palette.White, false);
        Box("Main Screen", interaction.transform, new Vector3(0f, 1.78f, 84.62f),
            new Vector3(2.5f, 0.12f, 0.95f), Quaternion.Euler(18f, 0f, 0f),
            palette.Screen, false);
        Box("Panel Red Fault", interaction.transform, new Vector3(0f, 1.86f, 84.48f),
            new Vector3(1.3f, 0.04f, 0.18f), Quaternion.Euler(18f, 0f, 0f),
            palette.Red, false);
    }

    private static void BuildTerminalSetDressing(
        Transform parent,
        Palette palette)
    {
        GameObject dressing = new GameObject("Terminal Set Dressing");
        dressing.transform.SetParent(parent, false);

        InstantiateVisual(
            Box01Path,
            dressing.transform,
            new Vector3(-8.5f, 0f, 72f),
            new Vector3(3f, 3.1f, 2.4f),
            Quaternion.Euler(0f, 90f, 0f),
            "Containment Cargo L");
        InstantiateVisual(
            Box02Path,
            dressing.transform,
            new Vector3(8.3f, 0f, 78f),
            new Vector3(4f, 1.4f, 2.2f),
            Quaternion.Euler(0f, 90f, 0f),
            "Containment Cargo R");
        InstantiateVisual(
            Laser01Path,
            dressing.transform,
            new Vector3(-9.5f, 0f, 91f),
            new Vector3(5f, 3.4f, 1.4f),
            Quaternion.Euler(0f, 90f, 0f),
            "Corrupted Laser Bank L");
        InstantiateVisual(
            Laser02Path,
            dressing.transform,
            new Vector3(9.5f, 0f, 96f),
            new Vector3(5f, 3.4f, 1.4f),
            Quaternion.Euler(0f, 90f, 0f),
            "Corrupted Laser Bank R");

        for (int side = -1; side <= 1; side += 2)
        {
            for (float z = 70f; z <= 108f; z += 12f)
            {
                Box("Server Plinth", dressing.transform,
                    new Vector3(side * 11.2f, 1.2f, z),
                    new Vector3(2.2f, 2.4f, 3.8f),
                    Quaternion.identity, palette.Dark, true);
                Box("Server Face", dressing.transform,
                    new Vector3(side * 10.05f, 1.3f, z),
                    new Vector3(0.08f, 1.6f, 2.6f),
                    Quaternion.identity,
                    (side < 0 && ((int)z / 12) % 2 == 0)
                        ? palette.Red
                        : palette.Screen,
                    false);
            }
        }
    }

    private static GameObject BuildCorruptionLayer(
        Transform parent,
        Palette palette,
        out Light[] corruptedLights)
    {
        GameObject layer = new GameObject("Cutscene Corruption Layer");
        layer.transform.SetParent(parent, false);
        List<Light> lights = new List<Light>();
        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.PI * 2f / 12f;
            Vector3 position = new Vector3(
                Mathf.Cos(angle) * 3.5f,
                5.4f + Mathf.Sin(angle * 2f) * 1.8f,
                103f + Mathf.Sin(angle) * 3.5f);
            Box(
                "Corruption Shard",
                layer.transform,
                position,
                new Vector3(0.18f, 1.5f, 0.18f),
                Quaternion.Euler(i * 19f, i * 31f, i * 13f),
                palette.Red,
                false);
        }

        GameObject lightObject = new GameObject("Corrupted Core Light");
        lightObject.transform.SetParent(layer.transform, false);
        lightObject.transform.position = new Vector3(0f, 5.4f, 100f);
        Light coreLight = lightObject.AddComponent<Light>();
        coreLight.type = LightType.Point;
        coreLight.color = new Color(1f, 0.02f, 0.03f);
        coreLight.intensity = 2800f;
        coreLight.range = 20f;
        coreLight.shadows = LightShadows.Soft;
        lights.Add(coreLight);
        corruptedLights = lights.ToArray();
        return layer;
    }

    private static void ConfigureTerminalGameplay(
        TerminalFinalePresentation presentation)
    {
        PlayerRunner player = Object.FindAnyObjectByType<PlayerRunner>();
        GameManager manager = Object.FindAnyObjectByType<GameManager>();
        Camera camera = Camera.main;
        if (player == null || manager == null || camera == null)
        {
            throw new InvalidOperationException(
                "Player, GameManager ou camera ausente na Fase 05.");
        }

        player.transform.position = Vector3.zero;
        player.initialSpeed = 5.5f;
        player.maximumSpeed = 7f;
        player.speedRampDistance = 90f;
        manager.terminalSequencePreview = true;
        manager.previewAutoRun = true;
        manager.previewSectorName = "TERMINAL CENTRAL // 05";
        manager.previewObjective =
            "Terminal Central alcancado. Aproxime-se do painel principal.";
        manager.finishDistance = 90f;

        FinalCutsceneController finalController =
            manager.GetComponent<FinalCutsceneController>();
        if (finalController == null)
        {
            finalController =
                manager.gameObject.AddComponent<FinalCutsceneController>();
        }
        finalController.dialogue = manager.dialogue;
        finalController.player = player;
        finalController.celestIAHud = manager.sectors == null
            ? Object.FindAnyObjectByType<CelestIAHudController>()
            : manager.sectors.celestIAHud;
        finalController.presentation = presentation;
        manager.finalCutscene = finalController;

        CameraFollow follow = camera.GetComponent<CameraFollow>();
        if (follow == null)
        {
            follow = camera.gameObject.AddComponent<CameraFollow>();
        }
        follow.target = player.transform;
        follow.offset = new Vector3(0f, 4.8f, -9.5f);
        follow.lookOffset = new Vector3(0f, 1.4f, 9f);
        follow.positionSmooth = 8f;
        follow.rotationSmooth = 7f;
        camera.transform.position = player.transform.position + follow.offset;
        camera.transform.LookAt(player.transform.position + follow.lookOffset);
        camera.fieldOfView = 60f;
        camera.farClipPlane = 180f;

        EditorUtility.SetDirty(player);
        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(finalController);
        EditorUtility.SetDirty(camera);
        EditorUtility.SetDirty(follow);
    }

    private static void ConfigureTerminalLighting()
    {
        GameObject lighting = new GameObject("Fase05 - Corrupted Lighting");
        GameObject fillObject = new GameObject("Cold Directional Fill");
        fillObject.transform.SetParent(lighting.transform, false);
        fillObject.transform.rotation = Quaternion.Euler(42f, -32f, 0f);
        Light fill = fillObject.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.25f, 0.48f, 0.7f);
        fill.intensity = 0.42f;
        fill.shadows = LightShadows.Hard;

        for (float z = 0f; z <= 108f; z += 14f)
        {
            GameObject lightObject =
                new GameObject("Corrupted Corridor Light");
            lightObject.transform.SetParent(lighting.transform, false);
            lightObject.transform.position = new Vector3(
                z % 28f < 1f ? -3.5f : 3.5f,
                z < 60f ? 6.6f : 9f,
                z);
            Light sceneLight = lightObject.AddComponent<Light>();
            sceneLight.type = LightType.Point;
            sceneLight.color = z % 28f < 1f
                ? new Color(0.05f, 0.7f, 1f)
                : new Color(1f, 0.025f, 0.035f);
            sceneLight.intensity = z < 60f ? 1100f : 2200f;
            sceneLight.range = z < 60f ? 14f : 20f;
            sceneLight.shadows = LightShadows.None;
        }

        for (float z = 70f; z <= 106f; z += 18f)
        {
            GameObject chamberLightObject =
                new GameObject("Terminal Chamber Fill");
            chamberLightObject.transform.SetParent(lighting.transform, false);
            chamberLightObject.transform.position = new Vector3(0f, 7.4f, z);
            Light chamberLight = chamberLightObject.AddComponent<Light>();
            chamberLight.type = LightType.Point;
            chamberLight.color = new Color(0.62f, 0.8f, 1f);
            chamberLight.intensity = 2400f;
            chamberLight.range = 24f;
            chamberLight.shadows = LightShadows.None;
        }

        GameObject volumeObject = new GameObject("Fase05 Global Volume");
        volumeObject.transform.SetParent(lighting.transform, false);
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 20f;
        volume.sharedProfile = CreateVolumeProfile();

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.17f, 0.24f, 0.31f);
        RenderSettings.ambientEquatorColor = new Color(0.16f, 0.08f, 0.11f);
        RenderSettings.ambientGroundColor = new Color(0.045f, 0.055f, 0.075f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.035f, 0.055f, 0.08f);
        RenderSettings.fogStartDistance = 58f;
        RenderSettings.fogEndDistance = 145f;

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.allowHDR = true;
            camera.backgroundColor = new Color(0.005f, 0.01f, 0.02f);
            UniversalAdditionalCameraData data =
                camera.GetComponent<UniversalAdditionalCameraData>();
            if (data == null)
            {
                data =
                    camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            data.renderPostProcessing = true;
            data.antialiasing =
                AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.dithering = true;
        }
    }

    private static void CreateTerminalArch(
        Transform parent,
        float z,
        float halfWidth,
        float height,
        Palette palette)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            Box("Arch Pillar", parent, new Vector3(side * halfWidth, height * 0.5f, z),
                new Vector3(0.65f, height, 0.8f), Quaternion.identity,
                palette.White, false);
            Box("Arch Inner Glow", parent,
                new Vector3(side * (halfWidth - 0.38f), height * 0.5f, z - 0.02f),
                new Vector3(0.09f, height * 0.68f, 0.84f), Quaternion.identity,
                side < 0 ? palette.Cyan : palette.Red, false);
        }
        Box("Arch Header", parent, new Vector3(0f, height, z),
            new Vector3(halfWidth * 2f, 0.65f, 0.85f), Quaternion.identity,
            palette.Dark, false);
        Box("Arch Header Glow", parent, new Vector3(0f, height - 0.36f, z - 0.02f),
            new Vector3(halfWidth * 1.45f, 0.1f, 0.9f), Quaternion.identity,
            palette.Screen, false);
    }

    private static void CreateCoreRing(
        Transform parent,
        Vector3 center,
        float radius,
        int segments,
        Material material)
    {
        float circumference = 2f * Mathf.PI * radius;
        float segmentLength = circumference / segments * 0.72f;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * 360f / segments;
            float radians = angle * Mathf.Deg2Rad;
            Vector3 position = center + new Vector3(
                Mathf.Cos(radians) * radius,
                0f,
                Mathf.Sin(radians) * radius);
            Box(
                "Core Ring Segment",
                parent,
                position,
                new Vector3(segmentLength, 0.22f, 0.28f),
                Quaternion.Euler(0f, -angle, 0f),
                material,
                false);
        }
    }

    private static void CreateObstacle(
        Transform parent,
        int lane,
        float z,
        bool tall,
        string prefabPath)
    {
        Vector3 size = tall
            ? new Vector3(2.35f, 2.65f, 1.1f)
            : new Vector3(2.35f, 0.85f, 1.1f);
        GameObject root = new GameObject(
            tall ? "Tall Containment Obstacle" : "Low Cargo Obstacle");
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(Lanes[lane], 0f, z);

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, size.y * 0.5f, 0f);
        collider.size = size;
        collider.isTrigger = true;
        root.AddComponent<Obstacle>();
        InstantiateVisual(
            prefabPath,
            root.transform,
            Vector3.zero,
            size,
            Quaternion.Euler(0f, 90f, 0f),
            "Obstacle Visual",
            true);
    }

    private static void CreateLaser(
        Transform parent,
        int lane,
        float z,
        string prefabPath,
        Material beamMaterial)
    {
        GameObject root = new GameObject("Laser Obstacle");
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(Lanes[lane], 0f, z);

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0.9f, 0f);
        collider.size = new Vector3(2.55f, 0.2f, 0.35f);
        collider.isTrigger = true;
        GameObject beam = Box(
            "Laser Damage Beam",
            root.transform,
            new Vector3(Lanes[lane], 0.9f, z - 0.02f),
            new Vector3(2.55f, 0.13f, 0.14f),
            Quaternion.identity,
            beamMaterial,
            false);
        LaserHazard hazard = root.AddComponent<LaserHazard>();
        hazard.visual = beam;
        hazard.damageCollider = collider;
        hazard.activeColor = beamMaterial.color;
        InstantiateVisual(
            prefabPath,
            root.transform,
            Vector3.zero,
            new Vector3(2.9f, 2.8f, 1.2f),
            Quaternion.Euler(0f, 90f, 0f),
            "Laser Unit Visual",
            true);
    }

    private static GameObject InstantiateVisual(
        string path,
        Transform parent,
        Vector3 position,
        Vector3 targetSize,
        Quaternion rotation,
        string name,
        bool localPosition = false)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            throw new InvalidOperationException(
                "Modelo de obstaculo nao encontrado: " + path);
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
        {
            throw new InvalidOperationException(
                "Falha ao instanciar modelo: " + path);
        }

        instance.name = name;
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

        Bounds bounds = CalculateBounds(prefab);
        instance.transform.localScale = new Vector3(
            targetSize.z / Mathf.Max(0.001f, bounds.size.x),
            targetSize.y / Mathf.Max(0.001f, bounds.size.y),
            targetSize.x / Mathf.Max(0.001f, bounds.size.z));
        SetStaticRecursive(instance);
        return instance;
    }

    private static Bounds CalculateBounds(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
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

    private static Palette CreateTerminalPalette()
    {
        return new Palette
        {
            White = MakeLitMaterial("M_F05_WhitePanel",
                new Color(0.78f, 0.83f, 0.88f), 0.38f, 0.72f),
            Dark = MakeLitMaterial("M_F05_DarkMetal",
                new Color(0.035f, 0.052f, 0.08f), 0.88f, 0.58f),
            Floor = MakeLitMaterial("M_F05_Floor",
                new Color(0.18f, 0.23f, 0.3f), 0.72f, 0.84f),
            FloorInset = MakeLitMaterial("M_F05_FloorInset",
                new Color(0.23f, 0.3f, 0.36f), 0.58f, 0.78f),
            Cyan = MakeEmissionMaterial("M_F05_CyanEmission",
                new Color(0.03f, 0.72f, 1f), 5.2f),
            Red = MakeEmissionMaterial("M_F05_RedEmission",
                new Color(1f, 0.012f, 0.025f), 6.2f),
            Glass = MakeGlassMaterial("M_F05_Glass",
                new Color(0.05f, 0.42f, 0.58f, 0.16f)),
            Screen = MakeEmissionMaterial("M_F05_Screen",
                new Color(0.02f, 0.4f, 0.65f), 4.4f),
            Hazard = MakeLitMaterial("M_F05_Hazard",
                new Color(0.78f, 0.45f, 0.025f), 0.25f, 0.45f)
        };
    }

    private static Material MakeLitMaterial(
        string name,
        Color color,
        float metallic,
        float smoothness)
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
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material MakeEmissionMaterial(
        string name,
        Color color,
        float intensity)
    {
        Material material = MakeLitMaterial(
            name, color * 0.35f, 0.15f, 0.82f);
        material.EnableKeyword("_EMISSION");
        material.SetColor(
            "_EmissionColor",
            new Color(
                color.r * intensity,
                color.g * intensity,
                color.b * intensity,
                1f));
        material.globalIlluminationFlags =
            MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material MakeGlassMaterial(string name, Color color)
    {
        Material material = MakeLitMaterial(name, color, 0.08f, 0.96f);
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

    private static VolumeProfile CreateVolumeProfile()
    {
        VolumeProfile existing =
            AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath);
        if (existing != null)
        {
            AssetDatabase.DeleteAsset(VolumePath);
        }
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, VolumePath);

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.5f);
        bloom.intensity.Override(1.05f);
        bloom.scatter.Override(0.72f);
        bloom.highQualityFiltering.Override(true);
        ColorAdjustments color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(0.45f);
        color.contrast.Override(18f);
        color.saturation.Override(-8f);
        color.colorFilter.Override(new Color(0.9f, 0.94f, 1f));
        Vignette vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.27f);
        vignette.smoothness.Override(0.58f);
        vignette.rounded.Override(true);
        Tonemapping tonemapping = profile.Add<Tonemapping>(true);
        tonemapping.mode.Override(TonemappingMode.ACES);
        ChromaticAberration chromatic =
            profile.Add<ChromaticAberration>(true);
        chromatic.intensity.Override(0.12f);
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static GameObject Box(
        string name,
        Transform parent,
        Vector3 position,
        Vector3 scale,
        Quaternion rotation,
        Material material,
        bool keepCollider)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.position = position;
        box.transform.rotation = rotation;
        box.transform.localScale = scale;
        box.GetComponent<Renderer>().sharedMaterial = material;
        if (!keepCollider)
        {
            Object.DestroyImmediate(box.GetComponent<Collider>());
        }
        SetStatic(box);
        return box;
    }

    private static GameObject Cylinder(
        string name,
        Transform parent,
        Vector3 position,
        Vector3 scale,
        Quaternion rotation,
        Material material)
    {
        GameObject cylinder =
            GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        cylinder.transform.SetParent(parent, false);
        cylinder.transform.position = position;
        cylinder.transform.rotation = rotation;
        cylinder.transform.localScale = scale;
        cylinder.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(cylinder.GetComponent<Collider>());
        SetStatic(cylinder);
        return cylinder;
    }

    private static GameObject Sphere(
        string name,
        Transform parent,
        Vector3 position,
        Vector3 scale,
        Quaternion rotation,
        Material material)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.SetParent(parent, false);
        sphere.transform.position = position;
        sphere.transform.rotation = rotation;
        sphere.transform.localScale = scale;
        sphere.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(sphere.GetComponent<Collider>());
        SetStatic(sphere);
        return sphere;
    }

    private static Transform Marker(
        string name,
        Transform parent,
        Vector3 position)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(parent, false);
        marker.transform.position = position;
        return marker.transform;
    }

    private static void CreateWorldText(
        string name,
        Transform parent,
        string text,
        Vector3 position,
        Quaternion rotation,
        float characterSize,
        int fontSize,
        Color color)
    {
        GameObject textObject = new GameObject(name, typeof(TextMesh));
        textObject.transform.SetParent(parent, false);
        textObject.transform.position = position;
        textObject.transform.rotation = rotation;
        TextMesh textMesh = textObject.GetComponent<TextMesh>();
        textMesh.text = text;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = characterSize;
        textMesh.fontSize = fontSize;
        textMesh.color = color;
        SetStatic(textObject);
    }

    private static void CapturePreview(
        string path,
        Vector3 position,
        Vector3 lookAt,
        float fieldOfView)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            GameObject cameraObject = new GameObject("Preview Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = position;
            camera.transform.LookAt(lookAt);
            camera.fieldOfView = fieldOfView;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 240f;
            camera.allowHDR = true;
            camera.backgroundColor = new Color(0.005f, 0.01f, 0.02f);
            UniversalAdditionalCameraData data =
                cameraObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing =
                AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            RenderTexture texture =
                new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = texture;
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = texture;
            Texture2D image =
                new Texture2D(960, 540, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, 960f, 540f), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(cameraObject);
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "PROJETO:AURORA - Preview nao gerado: " + exception.Message);
        }
    }

    private static void ValidateFirstPass(string scenePath)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject root = FindSceneObject("Fase01 - Curated Obstacle Pass");
        if (root == null ||
            root.GetComponentsInChildren<Obstacle>(true).Length < 8 ||
            root.GetComponentsInChildren<LaserHazard>(true).Length < 3 ||
            Object.FindAnyObjectByType<PlayerRunner>() == null ||
            Object.FindAnyObjectByType<GameManager>() == null)
        {
            throw new InvalidOperationException(
                "Validacao da primeira passagem da Fase 01 falhou.");
        }
    }

    private static void ValidateTerminal(string scenePath)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameManager manager = Object.FindAnyObjectByType<GameManager>();
        TerminalFinalePresentation presentation =
            Object.FindAnyObjectByType<TerminalFinalePresentation>();
        InteractableObject terminal =
            Object.FindObjectsByType<InteractableObject>(
                    FindObjectsInactive.Include)
                .FirstOrDefault(
                    item => item.action == InteractableAction.FinalTerminal);

        if (manager == null ||
            !manager.terminalSequencePreview ||
            presentation == null ||
            terminal == null ||
            Object.FindAnyObjectByType<PlayerRunner>() == null ||
            FindSceneObject("Fase05 - Terminal Central") == null)
        {
            throw new InvalidOperationException(
                "Validacao da cena Terminal Central falhou.");
        }
    }

    private static void ValidateIntegratedBeta(string scenePath)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameManager manager = Object.FindAnyObjectByType<GameManager>();
        PlayerRunner player = Object.FindAnyObjectByType<PlayerRunner>();
        TerminalFinalePresentation presentation =
            Object.FindAnyObjectByType<TerminalFinalePresentation>();
        InteractableObject terminal =
            Object.FindObjectsByType<InteractableObject>(
                    FindObjectsInactive.Include)
                .FirstOrDefault(
                    item => item.action == InteractableAction.FinalTerminal);
        GameObject firstPass = FindSceneObject("Fase01 - Curated Obstacle Pass");
        GameObject terminalRoot = FindSceneObject("Fase05 - Terminal Central");

        if (manager == null ||
            manager.terminalSequencePreview ||
            manager.finalCutscene == null ||
            manager.finalCutscene.presentation != presentation ||
            player == null ||
            presentation == null ||
            terminal == null ||
            firstPass == null ||
            firstPass.GetComponentsInChildren<Obstacle>(true).Length < 8 ||
            firstPass.GetComponentsInChildren<LaserHazard>(true).Length < 3 ||
            terminalRoot == null ||
            Mathf.Abs(terminal.transform.position.z - 2675f) > 0.1f)
        {
            throw new InvalidOperationException(
                "Validacao da cena principal da Beta 0.3 falhou.");
        }
    }

    private static void ConfigureBuildScenes(
        string integratedPath,
        string firstPassPath,
        string terminalPath)
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainMenuScenePath, true),
            new EditorBuildSettingsScene(integratedPath, true),
            new EditorBuildSettingsScene(firstPassPath, true),
            new EditorBuildSettingsScene(terminalPath, true)
        };
    }

    private static string FindSourceScene()
    {
        string path = AssetDatabase.FindAssets(
                "Fase01_SetorA_LaboratorioLimpo t:Scene")
            .Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault();
        if (string.IsNullOrEmpty(path))
        {
            throw new InvalidOperationException(
                "Cena base da Fase 01 nao encontrada.");
        }
        return path;
    }

    private static string FindSceneSubfolder(string suffix)
    {
        string folder = AssetDatabase
            .GetSubFolders("Assets/_ProjectAurora/Scenes")
            .FirstOrDefault(
                path => path.EndsWith(
                    suffix,
                    StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(folder))
        {
            throw new InvalidOperationException(
                "Pasta de cena nao encontrada: " + suffix);
        }
        return folder;
    }

    private static GameObject FindSceneObject(string name)
    {
        return Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include)
            .Select(item => item.gameObject)
            .FirstOrDefault(item => item.name == name);
    }

    private static void DestroySceneObject(string name)
    {
        GameObject target = FindSceneObject(name);
        if (target != null)
        {
            Object.DestroyImmediate(target);
        }
    }

    private static Material LoadMaterial(string path)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            throw new InvalidOperationException(
                "Material nao encontrado: " + path);
        }
        return material;
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

    private static void SetStatic(GameObject target)
    {
        GameObjectUtility.SetStaticEditorFlags(
            target,
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

    private static void WriteResult(string contents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
        File.WriteAllText(ResultPath, contents);
    }
}
