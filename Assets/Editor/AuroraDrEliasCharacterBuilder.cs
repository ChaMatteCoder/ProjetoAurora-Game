using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AuroraDrEliasCharacterBuilder
{
    private const string CharacterRoot =
        "Assets/_ProjectAurora/Characters/DrElias";
    private const string ModelPath =
        CharacterRoot + "/Animation/scientist+character+3d+model/" +
        "tripo_convert_1c0ed329-50ef-45bd-8891-8f1d62783e9c.fbx";
    private const string RunningPath = CharacterRoot + "/Model/Running.fbx";
    private const string JumpPath = CharacterRoot + "/Model/Jump.fbx";
    private const string IdlePath =
        CharacterRoot + "/Model/Nervously Look Around.fbx";
    private const string ControllerPath =
        CharacterRoot + "/Animation/DrElias_RunJump.controller";
    private const string VisualPrefabPath =
        CharacterRoot + "/Prefabs/DrElias_AnimatedVisual.prefab";
    private const string GameplayScenePath =
        "Assets/_ProjectAurora/Scenes/FASE 01 - Laboratório Limpo A/" +
        "Fase01_SetorA_LaboratorioLimpo.unity";
    private const string MainMenuScenePath =
        "Assets/_ProjectAurora/Scenes/MainMenu.unity";

    [MenuItem("Tools/Projeto Aurora/Dr. Elias/Configurar Modelo e Animacoes")]
    public static void Build()
    {
        EnsureFolder(CharacterRoot + "/Animation");
        EnsureFolder(CharacterRoot + "/Prefabs");

        ConfigureModelImporter();
        Avatar avatar = FindAvatar(ModelPath);
        if (avatar == null || !avatar.isValid || !avatar.isHuman)
        {
            throw new System.InvalidOperationException(
                "O Avatar Humanoid do Dr. Elias nao foi gerado corretamente.");
        }

        ConfigureAnimationImporter(RunningPath, "Running", true);
        ConfigureAnimationImporter(JumpPath, "Jump", false);
        ConfigureAnimationImporter(IdlePath, "Idle Nervous", true);

        AnimationClip running = FindClip(RunningPath, "Running");
        AnimationClip jump = FindClip(JumpPath, "Jump");
        AnimationClip idle = FindClip(IdlePath, "Idle Nervous");
        AnimatorController controller = CreateController(idle, running, jump);
        CreateVisualPrefab(controller);
        ApplyToGameplayScene();
        ValidateConfiguration(controller);
        ConfigureBuildScenes();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("PROJETO:AURORA - Dr. Elias e animacoes configurados.");
    }

    private static void ConfigureModelImporter()
    {
        ModelImporter importer = GetImporter(ModelPath);
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.optimizeGameObjects = false;
        importer.SaveAndReimport();
    }

    private static void ConfigureAnimationImporter(
        string path,
        string clipName,
        bool loop)
    {
        ModelImporter importer = GetImporter(path);
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.sourceAvatar = null;
        importer.importAnimation = true;
        importer.importCameras = false;
        importer.importLights = false;
        importer.optimizeGameObjects = false;

        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        if (clips == null || clips.Length == 0)
        {
            clips = importer.clipAnimations;
        }

        foreach (ModelImporterClipAnimation clip in clips)
        {
            clip.name = clipName;
            clip.loopTime = loop;
            clip.loopPose = loop;
            clip.keepOriginalOrientation = true;
            clip.keepOriginalPositionY = true;
            clip.keepOriginalPositionXZ = true;
            clip.lockRootRotation = true;
            clip.lockRootHeightY = true;
            clip.lockRootPositionXZ = true;
        }

        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    private static AnimatorController CreateController(
        AnimationClip idle,
        AnimationClip running,
        AnimationClip jump)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
        }

        AnimatorController controller =
            AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsJumping", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idleState = stateMachine.AddState("Idle Nervous");
        idleState.motion = idle;
        idleState.speed = 1f;
        stateMachine.defaultState = idleState;

        AnimatorState runningState = stateMachine.AddState("Running");
        runningState.motion = running;
        runningState.speed = 1f;

        AnimatorState jumpState = stateMachine.AddState("Jump");
        jumpState.motion = jump;
        jumpState.speed = 1f;

        AnimatorStateTransition toRunning = idleState.AddTransition(runningState);
        toRunning.hasExitTime = false;
        toRunning.duration = 0.18f;
        toRunning.AddCondition(AnimatorConditionMode.If, 0f, "IsRunning");

        AnimatorStateTransition toIdle = runningState.AddTransition(idleState);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.2f;
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsRunning");

        AnimatorStateTransition toJump = stateMachine.AddAnyStateTransition(jumpState);
        toJump.hasExitTime = false;
        toJump.duration = 0.08f;
        toJump.canTransitionToSelf = false;
        toJump.AddCondition(AnimatorConditionMode.If, 0f, "Jump");

        AnimatorStateTransition jumpToRun = jumpState.AddTransition(runningState);
        jumpToRun.hasExitTime = false;
        jumpToRun.duration = 0.08f;
        jumpToRun.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsJumping");
        jumpToRun.AddCondition(AnimatorConditionMode.If, 0f, "IsRunning");

        AnimatorStateTransition jumpToIdle = jumpState.AddTransition(idleState);
        jumpToIdle.hasExitTime = false;
        jumpToIdle.duration = 0.08f;
        jumpToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsJumping");
        jumpToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsRunning");

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void CreateVisualPrefab(AnimatorController controller)
    {
        GameObject wrapper = new GameObject("DrElias Animated Visual");
        try
        {
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            GameObject model = Object.Instantiate(modelPrefab);
            model.name = "DrElias Model";
            model.transform.SetParent(wrapper.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one * 1.95f;

            Animator animator = model.GetComponent<Animator>();
            if (animator == null)
            {
                animator = model.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            if (AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefabPath) != null)
            {
                AssetDatabase.DeleteAsset(VisualPrefabPath);
            }
            PrefabUtility.SaveAsPrefabAsset(wrapper, VisualPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(wrapper);
        }
    }

    private static void ApplyToGameplayScene()
    {
        var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
        GameObject player = GameObject.Find("Dr. Elias - Player");
        if (player == null)
        {
            throw new System.InvalidOperationException(
                "Player Dr. Elias nao encontrado na cena da Fase 01.");
        }

        for (int i = player.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = player.transform.GetChild(i);
            bool isKnownVisual =
                child.name == "Player Body" ||
                child.name == "DrElias Visual" ||
                child.name == "DrElias Animated Visual" ||
                child.name == "DrElias_AnimatedVisual";
            bool containsCharacterVisual =
                child.GetComponentInChildren<Animator>(true) != null ||
                child.GetComponentInChildren<Renderer>(true) != null;
            if (isKnownVisual || containsCharacterVisual)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        GameObject visualPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefabPath);
        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(
            visualPrefab,
            scene);
        visual.name = "DrElias Visual";
        visual.transform.SetParent(player.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        Animator animator = visual.GetComponentInChildren<Animator>(true);
        PlayerRunner runner = player.GetComponent<PlayerRunner>();
        DrEliasAnimationController driver =
            player.GetComponent<DrEliasAnimationController>();
        if (driver == null)
        {
            driver = player.AddComponent<DrEliasAnimationController>();
        }
        driver.runner = runner;
        driver.animator = animator;
        driver.referenceRunSpeed = runner.initialSpeed;

        CharacterController character = player.GetComponent<CharacterController>();
        character.height = 2.05f;
        character.radius = 0.38f;
        character.center = new Vector3(0f, 1.025f, 0f);

        PlayerHealth health = player.GetComponent<PlayerHealth>();
        health.renderers = visual.GetComponentsInChildren<Renderer>(true);

        EditorUtility.SetDirty(player);
        EditorUtility.SetDirty(driver);
        EditorUtility.SetDirty(health);
        EditorUtility.SetDirty(character);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, GameplayScenePath);
    }

    private static void ConfigureBuildScenes()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainMenuScenePath, true),
            new EditorBuildSettingsScene(GameplayScenePath, true)
        };

        SceneAsset menu =
            AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
        if (menu != null)
        {
            EditorSceneManager.playModeStartScene = menu;
        }
    }

    private static void ValidateConfiguration(AnimatorController controller)
    {
        var scene = EditorSceneManager.GetActiveScene();
        Transform[] sceneTransforms = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .ToArray();
        GameObject[] players = sceneTransforms
            .Where(transform => transform.name == "Dr. Elias - Player")
            .Select(transform => transform.gameObject)
            .ToArray();

        if (players.Length != 1)
        {
            throw new System.InvalidOperationException(
                "A cena precisa conter exatamente um Dr. Elias - Player.");
        }

        GameObject player = players[0];
        int visualChildren = Enumerable.Range(0, player.transform.childCount)
            .Select(index => player.transform.GetChild(index))
            .Count(child =>
                child.GetComponentInChildren<Animator>(true) != null ||
                child.GetComponentInChildren<Renderer>(true) != null);
        Animator[] animators = player.GetComponentsInChildren<Animator>(true);

        if (visualChildren != 1 || animators.Length != 1)
        {
            throw new System.InvalidOperationException(
                "Dr. Elias ainda possui um visual duplicado na cena.");
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        if (stateMachine.defaultState == null ||
            stateMachine.defaultState.name != "Idle Nervous")
        {
            throw new System.InvalidOperationException(
                "Idle Nervous precisa ser o estado inicial do Animator.");
        }

        bool hasRunningParameter = controller.parameters.Any(parameter =>
            parameter.name == "IsRunning" &&
            parameter.type == AnimatorControllerParameterType.Bool);
        bool hasJumpParameter = controller.parameters.Any(parameter =>
            parameter.name == "Jump" &&
            parameter.type == AnimatorControllerParameterType.Trigger);
        bool hasJumpingParameter = controller.parameters.Any(parameter =>
            parameter.name == "IsJumping" &&
            parameter.type == AnimatorControllerParameterType.Bool);
        if (!hasRunningParameter || !hasJumpParameter || !hasJumpingParameter)
        {
            throw new System.InvalidOperationException(
                "Parametros IsRunning, IsJumping e Jump nao foram configurados.");
        }

        Animator animator = animators[0];
        if (animator.runtimeAnimatorController != controller)
        {
            throw new System.InvalidOperationException(
                "O Animator do Dr. Elias nao usa o controller atualizado.");
        }

        DrEliasAnimationController driver =
            player.GetComponent<DrEliasAnimationController>();
        if (driver == null || driver.animator != animator)
        {
            throw new System.InvalidOperationException(
                "O controlador de animacao nao referencia o Animator principal.");
        }

        Debug.Log(
            "PROJETO:AURORA - Validacao concluida: um Dr. Elias e Idle Nervous inicial.");
    }

    private static Avatar FindAvatar(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Avatar>()
            .FirstOrDefault();
    }

    private static AnimationClip FindClip(string path, string name)
    {
        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<AnimationClip>()
            .FirstOrDefault(item =>
                !item.name.StartsWith("__preview__", System.StringComparison.Ordinal));
        if (clip == null)
        {
            throw new System.InvalidOperationException(
                "Clip " + name + " nao encontrado em " + path);
        }
        return clip;
    }

    private static ModelImporter GetImporter(string path)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
        {
            throw new FileNotFoundException("FBX nao encontrado.", path);
        }
        return importer;
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
}
