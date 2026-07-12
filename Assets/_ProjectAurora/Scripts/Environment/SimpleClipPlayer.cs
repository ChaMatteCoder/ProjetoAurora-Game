using UnityEngine;

/// Toca um AnimationClip em loop por amostragem manual (SampleAnimation).
/// Usado na cena do Painel Principal (2 braços + tubo): o FBX multi-rig não
/// inicializava de forma confiável nem via Animator+controller nem via
/// Playables — a amostragem direta por caminho funciona sempre (validado).
/// Custo: uma avaliação de clip por frame — desprezível para peças de vitrine.
public class SimpleClipPlayer : MonoBehaviour
{
    public AnimationClip clip;
    private float t;

    private void Update()
    {
        if (clip == null) return;
        t += Time.deltaTime;
        clip.SampleAnimation(gameObject, t % clip.length);
    }
}
