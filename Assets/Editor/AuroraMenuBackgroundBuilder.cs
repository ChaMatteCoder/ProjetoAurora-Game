using System.Collections.Generic;
using ProjectAurora.UI.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// Troca o fundo do menu principal: remove o loop de video (Dr.Elias_Loop) e monta
/// o slideshow de imagens com Ken Burns + crossfade (Round 19).
///
/// Segue a convencao do projeto para material substituido: o antigo nao e destruido,
/// vira "Legacy_..._Disabled" desativado (mesmo padrao de Legacy_MenuVisuals e
/// Legacy_SectorE_Ceiling_Disabled), mantendo o caminho de volta se preciso.
public static class AuroraMenuBackgroundBuilder
{
    private const string MenuRootPath = "Canvas_MainMenu/MenuRoot_16x9";
    private const string VideoBackgroundName = "Video_Background";
    private const string BackgroundName = "Menu_Background";
    private const string SlidesFolder = "Assets/_ProjectAurora/Art/Menu/Background";

    [MenuItem("Aurora/Menu/Trocar fundo de video por slideshow", priority = 30)]
    public static void Build()
    {
        GameObject menuRoot = GameObject.Find(MenuRootPath);
        if (menuRoot == null)
        {
            EditorUtility.DisplayDialog("Aurora",
                "MenuRoot_16x9 nao encontrado. Abra a cena MainMenu antes de rodar.", "OK");
            return;
        }

        Texture[] slides = LoadSlides();
        if (slides.Length == 0)
        {
            EditorUtility.DisplayDialog("Aurora",
                "Nenhuma imagem MenuBackdrop_*.png encontrada em " + SlidesFolder, "OK");
            return;
        }

        // 1) aposenta o fundo de video (desativa + renomeia, nao destroi)
        Transform oldVideo = menuRoot.transform.Find(VideoBackgroundName);
        int backgroundSiblingIndex = 0;
        if (oldVideo != null)
        {
            backgroundSiblingIndex = oldVideo.GetSiblingIndex();
            Undo.RecordObject(oldVideo.gameObject, "Aposentar fundo de video");
            oldVideo.name = "Legacy_VideoBackground_Disabled";
            oldVideo.gameObject.SetActive(false);
            EditorUtility.SetDirty(oldVideo.gameObject);
        }

        // 2) recria o container do slideshow do zero
        Transform existing = menuRoot.transform.Find(BackgroundName);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        GameObject background = new GameObject(BackgroundName,
            typeof(RectTransform), typeof(RectMask2D), typeof(AuroraMenuSlideshow));
        Undo.RegisterCreatedObjectUndo(background, "Criar fundo do menu");
        background.transform.SetParent(menuRoot.transform, false);
        Stretch(background.GetComponent<RectTransform>());
        // fundo fica no mesmo lugar que o video ocupava (atras de todo o resto)
        background.transform.SetSiblingIndex(backgroundSiblingIndex);

        RawImage slideA = CreateSlideLayer(background.transform, "Slide_A");
        RawImage slideB = CreateSlideLayer(background.transform, "Slide_B");

        AuroraMenuSlideshow slideshow = background.GetComponent<AuroraMenuSlideshow>();
        slideshow.ConfigureFromEditor(slides, slideA, slideB);
        EditorUtility.SetDirty(slideshow);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[Aurora/Menu] Slideshow montado com {slides.Length} imagens. " +
                  "Fundo de video aposentado como Legacy_VideoBackground_Disabled.");
    }

    private static RawImage CreateSlideLayer(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        Stretch(rt);

        RawImage img = go.GetComponent<RawImage>();
        img.color = new Color(1f, 1f, 1f, 0f); // comeca invisivel; o script controla o alpha
        img.raycastTarget = false;             // nunca rouba clique dos botoes do menu
        return img;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.anchoredPosition = Vector2.zero;
    }

    private static Texture[] LoadSlides()
    {
        var found = new List<Texture>();
        // ordem estavel por nome: MenuBackdrop_01, _02, ...
        string[] guids = AssetDatabase.FindAssets("MenuBackdrop t:Texture", new[] { SlidesFolder });
        var paths = new List<string>();
        foreach (string guid in guids)
        {
            paths.Add(AssetDatabase.GUIDToAssetPath(guid));
        }
        paths.Sort(string.CompareOrdinal);

        foreach (string path in paths)
        {
            Texture tex = AssetDatabase.LoadAssetAtPath<Texture>(path);
            if (tex != null)
            {
                found.Add(tex);
            }
        }
        return found.ToArray();
    }
}
