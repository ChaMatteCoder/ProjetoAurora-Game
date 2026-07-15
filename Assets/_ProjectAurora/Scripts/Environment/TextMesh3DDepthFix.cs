using UnityEngine;

/// Corrige o TextMesh legado (placas de nome de setor, marcas de parede) que
/// desenhava POR CIMA das paredes: troca o shader "GUI/Text Shader" (ZTest
/// Always) pelo "Aurora/TextMesh3DDepth" (ZTest LEqual), fazendo o texto ser
/// ocluído pela geometria. Roda no editor e no jogo ([ExecuteAlways]).
///
/// O material do TextMesh de fonte dinâmica é compartilhado e mantém o atlas de
/// glifos vivo — por isso só trocamos o SHADER do material existente (sem criar
/// instância nem novo material), preservando a fonte.
[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
public class TextMesh3DDepthFix : MonoBehaviour
{
    private const string DepthShader = "Aurora/TextMesh3DDepth";

    private void OnEnable()
    {
        Apply();
    }

    private void Apply()
    {
        var mr = GetComponent<MeshRenderer>();
        if (mr == null) return;

        Shader depth = Shader.Find(DepthShader);
        if (depth == null) return;

        Material mat = mr.sharedMaterial;
        if (mat != null && mat.shader != depth)
        {
            mat.shader = depth;
        }
    }
}
