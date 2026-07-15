using UnityEngine;

/// Luz que começa APAGADA e acende suavemente quando o Dr. Elias chega perto
/// (em Z), permanecendo acesa depois. Usada no corredor do Núcleo/Terminal
/// Central para o efeito das luzes acendendo conforme o player avança.
[RequireComponent(typeof(Light))]
public class ProgressiveLight : MonoBehaviour
{
    [Tooltip("Distância em Z (m) do player para começar a acender.")]
    public float activateDistance = 16f;
    [Tooltip("Tempo aproximado (s) para acender por completo.")]
    public float fadeInSeconds = 0.6f;

    private Light lit;
    private float targetIntensity;
    private bool triggered;
    private Transform player;

    private void Awake()
    {
        lit = GetComponent<Light>();
        targetIntensity = lit.intensity;
        lit.intensity = 0f; // começa apagada
    }

    private void Update()
    {
        if (player == null)
        {
            var go = GameObject.Find("Dr. Elias - Player");
            if (go == null) return;
            player = go.transform;
        }

        if (!triggered)
        {
            if (Mathf.Abs(player.position.z - transform.position.z) <= activateDistance)
            {
                triggered = true;
            }
            else
            {
                return;
            }
        }

        float rate = targetIntensity / Mathf.Max(0.05f, fadeInSeconds);
        lit.intensity = Mathf.MoveTowards(lit.intensity, targetIntensity, rate * Time.deltaTime);
        if (lit.intensity >= targetIntensity - 0.01f)
        {
            lit.intensity = targetIntensity;
            enabled = false; // aceso; não precisa mais atualizar
        }
    }
}
