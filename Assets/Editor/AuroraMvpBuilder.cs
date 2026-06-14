using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public static class AuroraMvpBuilder
{
    private static readonly Color Cyan = new Color(0.05f, 0.85f, 1f);
    private static readonly Color Dark = new Color(0.025f, 0.04f, 0.065f);

    [MenuItem("Tools/Projeto Aurora/Build Beta 0.2")]
    public static void BuildBeta()
    {
        EnsureFolders();
        ConfigureVideoImport();
        Material[] materials = CreateMaterials();
        CreateMainMenuScene();
        CreateGameScene(materials);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true)
        };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        Debug.Log("PROJETO:AURORA Beta 0.2 criada com sucesso.");
    }

    private static void ConfigureVideoImport()
    {
        string[] paths =
        {
            "Assets/Videos/CelestIA/Celestia01.mp4",
            "Assets/Videos/CelestIA/Celestia02.mp4",
            "Assets/Videos/CelestIA/Celestia03.mp4"
        };

        foreach (string path in paths)
        {
            VideoClipImporter importer = AssetImporter.GetAtPath(path) as VideoClipImporter;
            if (importer == null)
            {
                Debug.LogWarning($"PROJETO:AURORA - Vídeo não encontrado: {path}");
                continue;
            }

            VideoImporterTargetSettings settings = importer.defaultTargetSettings;
            settings.enableTranscoding = false;
            importer.defaultTargetSettings = settings;
            importer.importAudio = false;
            importer.SaveAndReimport();
        }
    }

    [MenuItem("Tools/Projeto Aurora/Build MVP")]
    public static void BuildLegacyEntry() => BuildBeta();

    private static void EnsureFolders()
    {
        string[] folders =
        {
            "Assets/Scenes", "Assets/Scripts", "Assets/Editor", "Assets/Audio",
            "Assets/Materials", "Assets/Videos", "Assets/Videos/CelestIA", "Assets/RenderTextures"
        };
        foreach (string folder in folders)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
                AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
            }
        }
    }

    private static Material[] CreateMaterials()
    {
        return new[]
        {
            MakeMaterial("Player", new Color(0.02f, 0.85f, 1f)),
            MakeMaterial("Floor", new Color(0.07f, 0.09f, 0.12f)),
            MakeMaterial("Obstacle", new Color(0.9f, 0.04f, 0.05f)),
            MakeMaterial("SectorA", new Color(0.12f, 0.45f, 0.52f)),
            MakeMaterial("Containment", new Color(0.55f, 0.45f, 0.08f)),
            MakeMaterial("MachineRoom", new Color(0.45f, 0.2f, 0.05f)),
            MakeMaterial("RedCorridor", new Color(0.48f, 0.025f, 0.035f)),
            MakeMaterial("TechnicalBridge", new Color(0.04f, 0.12f, 0.28f)),
            MakeMaterial("CentralTerminal", new Color(0.02f, 0.38f, 0.42f)),
            MakeMaterial("Laser", new Color(1f, 0.01f, 0.01f))
        };
    }

    private static Material MakeMaterial(string name, Color color)
    {
        string path = $"Assets/Materials/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = shader;
        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void CreateMainMenuScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MainMenu";

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Dark;
        camera.cullingMask = 0;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        CreateEventSystem();
        Canvas canvas = CreateCanvas("Main Menu Canvas");
        Image background = CreatePanel(canvas.transform, "Background", Dark, Vector2.zero, Vector2.one);
        MainMenuController controller = background.gameObject.AddComponent<MainMenuController>();

        CreateText(background.transform, "Title", "PROJETO:AURORA\nFALHA DE CONTENÇÃO", 42, TextAnchor.MiddleCenter,
            new Vector2(0.1f, 0.73f), new Vector2(0.9f, 0.94f), Cyan);
        CreateText(background.transform, "Subtitle", "BETA 0.2 MECÂNICA", 20, TextAnchor.MiddleCenter,
            new Vector2(0.2f, 0.66f), new Vector2(0.8f, 0.73f), Color.white);

        controller.playButton = CreateButton(background.transform, "Play", "JOGAR", 0.54f);
        controller.controlsButton = CreateButton(background.transform, "Controls", "CONTROLES", 0.44f);
        controller.settingsButton = CreateButton(background.transform, "Settings", "CONFIGURAÇÕES", 0.34f);
        controller.extraButton = CreateButton(background.transform, "Extra", "EXTRA", 0.24f);
        controller.creditsButton = CreateButton(background.transform, "Credits", "CRÉDITOS", 0.14f);
        controller.quitButton = CreateButton(background.transform, "Quit", "SAIR", 0.04f);

        controller.controlsPanel = CreateMenuPanel(background.transform, "Controls Panel",
            "CONTROLES\n\nA / ← : mover para a esquerda\nD / → : mover para a direita\nEspaço : pular\nE : interagir\nEsc : pausar",
            out controller.controlsCloseButton);
        controller.settingsPanel = CreateMenuPanel(background.transform, "Settings Panel",
            "CONFIGURAÇÕES\n\nOpções de áudio e acessibilidade em desenvolvimento.",
            out controller.settingsCloseButton);
        controller.extraPanel = CreateMenuPanel(background.transform, "Extra Panel",
            "EXTRA\n\nConteúdo de lore em desenvolvimento.",
            out controller.extraCloseButton);
        controller.creditsPanel = CreateMenuPanel(background.transform, "Credits Panel",
            "CRÉDITOS\n\nConceito e desenvolvimento: Projeto Aurora\nBeta mecânica poligonal",
            out controller.creditsCloseButton);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
    }

    private static GameObject CreateMenuPanel(Transform parent, string name, string text, out Button close)
    {
        Image panel = CreatePanel(parent, name, new Color(0.02f, 0.07f, 0.11f, 0.99f),
            new Vector2(0.22f, 0.15f), new Vector2(0.78f, 0.85f));
        CreateText(panel.transform, "Content", text, 24, TextAnchor.MiddleCenter,
            new Vector2(0.08f, 0.2f), new Vector2(0.92f, 0.92f), Color.white);
        close = CreateButton(panel.transform, "Close", "VOLTAR",
            new Vector2(0.32f, 0.06f), new Vector2(0.68f, 0.17f));
        panel.gameObject.SetActive(false);
        return panel.gameObject;
    }

    private static void CreateGameScene(Material[] materials)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Game";
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.34f, 0.34f, 0.4f);

        GameObject environment = new GameObject("Environment - 2700m");
        string[] sectorNames =
        {
            "Setor A - Laboratório", "Corredor de Contenção", "Sala de Máquinas",
            "Corredor Vermelho", "Ponte Técnica", "Terminal Central"
        };
        for (int i = 0; i < 6; i++)
        {
            CreateSector(environment.transform, sectorNames[i], i * 450f + 225f, materials[i + 3], materials[1]);
        }

        GameObject player = CreatePlayer(materials[0], materials[8]);
        CreateCamera(player.transform);

        GameObject gameplay = new GameObject("Gameplay Objects");
        int obstacleCount = CreateObstacles(gameplay.transform, materials[2], materials[8], materials[9]);
        gameplay.AddComponent<ObstacleSpawner>().authoredObstacleCount = obstacleCount;

        CreateEventSystem();
        UIManager ui = CreateGameplayUi(out CelestIAHudController hud);

        GameObject systems = new GameObject("Game Systems");
        CelestIAController celestia = systems.AddComponent<CelestIAController>();
        celestia.ui = ui;
        SectorManager sectors = systems.AddComponent<SectorManager>();
        sectors.ui = ui;
        sectors.celestIAHud = hud;
        TutorialManager tutorial = systems.AddComponent<TutorialManager>();
        tutorial.player = player.GetComponent<PlayerRunner>();
        tutorial.celestIA = celestia;

        CreateTutorialPanel(gameplay.transform, tutorial, materials[8], materials[2], out GameObject tutorialPanel);
        tutorial.tutorialPanel = tutorialPanel;

        GameManager manager = systems.AddComponent<GameManager>();
        manager.player = player.GetComponent<PlayerRunner>();
        manager.health = player.GetComponent<PlayerHealth>();
        manager.ui = ui;
        manager.sectors = sectors;
        manager.celestIA = celestia;
        manager.tutorial = tutorial;

        CreateFinalTerminal(gameplay.transform, materials[8]);
        CreateAudioManager();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Game.unity");
    }

    private static GameObject CreatePlayer(Material playerMaterial, Material visorMaterial)
    {
        GameObject player = new GameObject("Dr. Elias - Player");
        CharacterController character = player.AddComponent<CharacterController>();
        character.height = 2f;
        character.radius = 0.48f;
        character.center = new Vector3(0f, 1f, 0f);
        PlayerRunner runner = player.AddComponent<PlayerRunner>();
        runner.speedRampDistance = 2700f;
        player.AddComponent<PlayerInteraction>();
        PlayerHealth health = player.AddComponent<PlayerHealth>();

        GameObject body = CreatePrimitive(PrimitiveType.Capsule, "Player Body", player.transform, new Vector3(0f, 1f, 0f),
            Vector3.one, playerMaterial, false);
        GameObject visor = CreatePrimitive(PrimitiveType.Cube, "Visor", body.transform, new Vector3(0f, 0.25f, 0.43f),
            new Vector3(0.55f, 0.18f, 0.08f), visorMaterial, false);
        health.renderers = new[] { body.GetComponent<Renderer>(), visor.GetComponent<Renderer>() };
        return player;
    }

    private static void CreateCamera(Transform player)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 60f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 3200f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Dark;
        cameraObject.AddComponent<AudioListener>();
        CameraFollow follow = cameraObject.AddComponent<CameraFollow>();
        follow.target = player;
        cameraObject.transform.position = player.position + follow.offset;
        cameraObject.transform.LookAt(player.position + Vector3.up);
    }

    private static void CreateSector(Transform parent, string name, float centerZ, Material sector, Material floor)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = new Vector3(0f, 0f, centerZ);
        CreatePrimitive(PrimitiveType.Cube, "Floor", root.transform, new Vector3(0f, -0.25f, 0f),
            new Vector3(12f, 0.5f, 450f), floor, true);
        CreatePrimitive(PrimitiveType.Cube, "Left Wall", root.transform, new Vector3(-6f, 2f, 0f),
            new Vector3(0.35f, 4f, 450f), sector, true);
        CreatePrimitive(PrimitiveType.Cube, "Right Wall", root.transform, new Vector3(6f, 2f, 0f),
            new Vector3(0.35f, 4f, 450f), sector, true);

        for (int z = -210; z <= 210; z += 45)
        {
            CreatePrimitive(PrimitiveType.Cube, $"Arch {z} Left", root.transform, new Vector3(-4.7f, 3.4f, z),
                new Vector3(0.25f, 6.5f, 0.4f), sector, false);
            CreatePrimitive(PrimitiveType.Cube, $"Arch {z} Right", root.transform, new Vector3(4.7f, 3.4f, z),
                new Vector3(0.25f, 6.5f, 0.4f), sector, false);
            CreatePrimitive(PrimitiveType.Cube, $"Arch {z} Top", root.transform, new Vector3(0f, 6.5f, z),
                new Vector3(9.6f, 0.25f, 0.4f), sector, false);
        }
    }

    private static int CreateObstacles(Transform parent, Material obstacle, Material accent, Material laserMaterial)
    {
        int count = 0;
        for (float z = 90f; z < 2620f; z += 58f)
        {
            int lane = count % 3;
            float x = (lane - 1) * 3f;
            if (count % 7 == 3)
            {
                CreateLaser(parent, new Vector3(x, 0.55f, z), new Vector3(2.4f, 0.16f, 0.18f), laserMaterial);
            }
            else if (count % 9 == 5)
            {
                CreateRobot(parent, new Vector3(x, 0f, z), obstacle, accent);
            }
            else
            {
                bool low = count % 3 == 0;
                Vector3 scale = low ? new Vector3(2.2f, 0.7f, 0.8f) : new Vector3(2.2f, 2.6f, 0.8f);
                GameObject block = CreatePrimitive(PrimitiveType.Cube, low ? "Low Barrier" : "Containment Barrier",
                    parent, new Vector3(x, scale.y * 0.5f, z), scale, obstacle, true);
                block.GetComponent<Collider>().isTrigger = true;
                block.AddComponent<Obstacle>();
            }
            count++;
        }

        LaserHazard controlled = CreateLaser(parent, new Vector3(0f, 1.1f, 760f),
            new Vector3(8.5f, 0.18f, 0.2f), laserMaterial);
        CreateInteractionPanel(parent, new Vector3(-3f, 1f, 735f), "Painel de lasers",
            InteractableAction.DisableLaser, accent, null, controlled, null, "Pressione E para desativar lasers");

        GameObject door = CreatePrimitive(PrimitiveType.Cube, "Containment Door", parent, new Vector3(0f, 2.2f, 520f),
            new Vector3(9f, 4.4f, 0.5f), obstacle, false);
        CreateInteractionPanel(parent, new Vector3(3f, 1f, 505f), "Painel de porta",
            InteractableAction.OpenDoor, accent, door, null, null, "Pressione E para abrir a porta");
        return count + 2;
    }

    private static LaserHazard CreateLaser(Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject laser = CreatePrimitive(PrimitiveType.Cube, "Laser Hazard", parent, position, scale, material, true);
        laser.GetComponent<Collider>().isTrigger = true;
        LaserHazard hazard = laser.AddComponent<LaserHazard>();
        hazard.visual = laser;
        hazard.damageCollider = laser.GetComponent<Collider>();
        return hazard;
    }

    private static void CreateRobot(Transform parent, Vector3 position, Material bodyMaterial, Material eyeMaterial)
    {
        GameObject robot = new GameObject("Security Robot");
        robot.transform.SetParent(parent);
        robot.transform.position = position;
        BoxCollider collider = robot.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 1.25f, 0f);
        collider.size = new Vector3(1.8f, 2.5f, 1.1f);
        collider.isTrigger = true;
        robot.AddComponent<Obstacle>();
        CreatePrimitive(PrimitiveType.Cube, "Body", robot.transform, new Vector3(0f, 1.25f, 0f),
            new Vector3(1.5f, 1.4f, 0.9f), bodyMaterial, false);
        CreatePrimitive(PrimitiveType.Sphere, "Head", robot.transform, new Vector3(0f, 2.2f, 0f),
            new Vector3(0.85f, 0.65f, 0.75f), bodyMaterial, false);
        CreatePrimitive(PrimitiveType.Cube, "Red Eye", robot.transform, new Vector3(0f, 2.25f, -0.38f),
            new Vector3(0.45f, 0.14f, 0.08f), eyeMaterial, false);
    }

    private static void CreateTutorialPanel(Transform parent, TutorialManager tutorial, Material accent,
        Material doorMaterial, out GameObject panel)
    {
        GameObject door = CreatePrimitive(PrimitiveType.Cube, "Tutorial Door", parent, new Vector3(0f, 2f, 8f),
            new Vector3(9f, 4f, 0.5f), doorMaterial, false);
        panel = CreateInteractionPanel(parent, new Vector3(0f, 1f, 2f), "Tutorial Panel",
            InteractableAction.TutorialPanel, accent, door, null, tutorial, "Pressione E");
        panel.SetActive(false);
    }

    private static GameObject CreateInteractionPanel(Transform parent, Vector3 position, string name,
        InteractableAction action, Material material, GameObject target, LaserHazard laser, TutorialManager tutorial,
        string prompt)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = position;
        BoxCollider trigger = root.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(4f, 3f, 5f);
        InteractableObject interactable = root.AddComponent<InteractableObject>();
        interactable.action = action;
        interactable.prompt = prompt;
        interactable.targetObject = target;
        interactable.targetLaser = laser;
        interactable.tutorial = tutorial;
        interactable.message = action == InteractableAction.DisableLaser
            ? "CELESTIA: Rede de lasers desativada."
            : "CELESTIA: Acesso liberado.";
        CreatePrimitive(PrimitiveType.Cube, "Console", root.transform, Vector3.zero,
            new Vector3(0.9f, 1.4f, 0.25f), material, false);
        return root;
    }

    private static void CreateFinalTerminal(Transform parent, Material material)
    {
        CreatePrimitive(PrimitiveType.Cylinder, "Terminal Core", parent, new Vector3(0f, 2f, 2685f),
            new Vector3(2f, 2f, 2f), material, false);
        CreatePrimitive(PrimitiveType.Sphere, "Aurora Core", parent, new Vector3(0f, 4.5f, 2685f),
            new Vector3(2f, 2f, 2f), material, false);
        GameObject terminal = CreateInteractionPanel(parent, new Vector3(0f, 1f, 2675f), "Terminal Central Access",
            InteractableAction.FinalTerminal, material, null, null, null,
            "Pressione E para acessar o Terminal Central");
        terminal.GetComponent<InteractableObject>().message = string.Empty;
    }

    private static void CreateAudioManager()
    {
        GameObject audio = new GameObject("Music Manager");
        AudioSource source = audio.AddComponent<AudioSource>();
        source.spatialBlend = 0f;
        AudioManager manager = audio.AddComponent<AudioManager>();
        manager.gameplayMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Falha de Contenção.mp3");
    }

    private static UIManager CreateGameplayUi(out CelestIAHudController celestIAHud)
    {
        Canvas canvas = CreateCanvas("HUD Canvas");
        UIManager ui = canvas.gameObject.AddComponent<UIManager>();
        Image top = CreatePanel(canvas.transform, "HUD Top", new Color(0.01f, 0.025f, 0.045f, 0.9f),
            new Vector2(0f, 0.86f), Vector2.one);
        ui.sectorText = CreateText(top.transform, "Sector", "SETOR:", 20, TextAnchor.MiddleLeft,
            new Vector2(0.025f, 0.52f), new Vector2(0.62f, 0.95f), Cyan);
        ui.distanceText = CreateText(top.transform, "Distance", "DISTÂNCIA: 0m / 2700m", 18, TextAnchor.MiddleLeft,
            new Vector2(0.025f, 0.08f), new Vector2(0.62f, 0.5f), Color.white);
        ui.livesText = CreateText(top.transform, "Lives", "VIDAS: 3", 22, TextAnchor.MiddleRight,
            new Vector2(0.72f, 0.18f), new Vector2(0.97f, 0.85f), Color.white);

        Image message = CreatePanel(canvas.transform, "CelestIA Panel", new Color(0.01f, 0.04f, 0.07f, 0.92f),
            new Vector2(0.16f, 0.04f), new Vector2(0.92f, 0.15f));
        ui.celestiaText = CreateText(message.transform, "CelestIA", "CELESTIA:", 19, TextAnchor.MiddleCenter,
            new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f), Cyan);
        celestIAHud = CreateCelestIACard(canvas.transform);

        Image prompt = CreatePanel(canvas.transform, "Interaction Prompt", new Color(0.02f, 0.12f, 0.16f, 0.96f),
            new Vector2(0.36f, 0.19f), new Vector2(0.64f, 0.27f));
        ui.interactionPrompt = prompt.gameObject;
        ui.interactionText = CreateText(prompt.transform, "Prompt Text", "Pressione E", 22, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, Color.white);
        prompt.gameObject.SetActive(false);

        Image sectorCard = CreatePanel(canvas.transform, "Sector Card", new Color(0.01f, 0.08f, 0.12f, 0.94f),
            new Vector2(0.27f, 0.68f), new Vector2(0.73f, 0.78f));
        sectorCard.gameObject.AddComponent<CanvasGroup>();
        ui.sectorCard = sectorCard.gameObject;
        ui.sectorCardText = CreateText(sectorCard.transform, "Sector Card Text", string.Empty, 26,
            TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Cyan);
        sectorCard.gameObject.SetActive(false);

        ui.pausePanel = CreateOverlay(canvas.transform, "Pause Panel", "PAUSADO", out ui.resumeButton,
            out ui.pauseRestartButton, out ui.pauseMenuButton, "CONTINUAR", "REINICIAR", "VOLTAR AO MENU");
        ui.gameOverPanel = CreateOverlay(canvas.transform, "Game Over Panel", "FALHA DE CONTENÇÃO",
            out ui.gameOverRestartButton, out ui.gameOverMenuButton, out Button gameIgnored,
            "REINICIAR", "VOLTAR AO MENU", string.Empty);
        gameIgnored.gameObject.SetActive(false);
        ui.finalPanel = CreateOverlay(canvas.transform, "Final Panel", "PROTOCOLO AURORA CONTINUA",
            out ui.finalRestartButton, out ui.finalMenuButton, out Button finalIgnored,
            "JOGAR NOVAMENTE", "VOLTAR AO MENU", string.Empty);
        finalIgnored.gameObject.SetActive(false);

        Image intro = CreatePanel(canvas.transform, "Intro Panel", Color.black, Vector2.zero, Vector2.one);
        ui.introPanel = intro.gameObject;
        ui.introText = CreateText(intro.transform, "Intro Text", string.Empty, 28, TextAnchor.MiddleCenter,
            new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.65f), Cyan);
        return ui;
    }

    private static CelestIAHudController CreateCelestIACard(Transform parent)
    {
        Image frame = CreatePanel(parent, "CelestIA Video Card", new Color(0.02f, 0.3f, 0.38f, 1f),
            new Vector2(0.025f, 0.025f), new Vector2(0.145f, 0.19f));
        RectMask2D mask = frame.gameObject.AddComponent<RectMask2D>();
        GameObject rawObject = new GameObject("Video", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        rawObject.transform.SetParent(frame.transform, false);
        RectTransform rect = rawObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(-0.35f, 0f);
        rect.anchorMax = new Vector2(1.35f, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        string rtPath = "Assets/RenderTextures/CelestIAHud.renderTexture";
        RenderTexture texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
        if (texture == null)
        {
            texture = new RenderTexture(512, 512, 0) { name = "CelestIAHud" };
            AssetDatabase.CreateAsset(texture, rtPath);
        }
        rawObject.GetComponent<RawImage>().texture = texture;

        VideoPlayer video = frame.gameObject.AddComponent<VideoPlayer>();
        video.playOnAwake = false;
        video.isLooping = true;
        video.renderMode = VideoRenderMode.RenderTexture;
        video.targetTexture = texture;
        video.audioOutputMode = VideoAudioOutputMode.None;

        CelestIAHudController controller = frame.gameObject.AddComponent<CelestIAHudController>();
        controller.videoPlayer = video;
        controller.display = rawObject.GetComponent<RawImage>();
        controller.normalClip = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Videos/CelestIA/Celestia01.mp4");
        controller.transitionClip = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Videos/CelestIA/Celestia02.mp4");
        controller.corruptedClip = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Videos/CelestIA/Celestia03.mp4");
        return controller;
    }

    private static GameObject CreateOverlay(Transform parent, string name, string title, out Button first,
        out Button second, out Button third, string firstLabel, string secondLabel, string thirdLabel)
    {
        Image panel = CreatePanel(parent, name, new Color(0.01f, 0.02f, 0.04f, 0.97f), Vector2.zero, Vector2.one);
        CreateText(panel.transform, "Title", title, 38, TextAnchor.MiddleCenter,
            new Vector2(0.1f, 0.62f), new Vector2(0.9f, 0.82f), Cyan);
        first = CreateButton(panel.transform, "First", firstLabel, new Vector2(0.36f, 0.45f), new Vector2(0.64f, 0.54f));
        second = CreateButton(panel.transform, "Second", secondLabel, new Vector2(0.36f, 0.33f), new Vector2(0.64f, 0.42f));
        third = CreateButton(panel.transform, "Third", thirdLabel, new Vector2(0.36f, 0.21f), new Vector2(0.64f, 0.3f));
        panel.gameObject.SetActive(false);
        return panel.gameObject;
    }

    private static Canvas CreateCanvas(string name)
    {
        GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static void CreateEventSystem()
    {
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private static Image CreatePanel(Transform parent, string name, Color color, Vector2 min, Vector2 max)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text CreateText(Transform parent, string name, string value, int size, TextAnchor anchor,
        Vector2 min, Vector2 max, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Text text = go.GetComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.alignment = anchor;
        text.color = color;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 12;
        text.resizeTextMaxSize = size;
        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label, float bottom)
    {
        return CreateButton(parent, name, label, new Vector2(0.38f, bottom), new Vector2(0.62f, bottom + 0.075f));
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 min, Vector2 max)
    {
        Image image = CreatePanel(parent, name, new Color(0.03f, 0.3f, 0.42f, 0.95f), min, max);
        Button button = image.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = Cyan;
        colors.pressedColor = new Color(0.02f, 0.65f, 0.8f);
        button.colors = colors;
        CreateText(image.transform, "Label", label, 22, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Color.white);
        return button;
    }

    private static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 localPosition,
        Vector3 scale, Material material, bool keepCollider)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        if (!keepCollider)
        {
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }
        return go;
    }
}
