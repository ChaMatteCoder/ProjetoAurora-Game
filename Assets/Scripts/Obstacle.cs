using UnityEngine;

public class Obstacle : MonoBehaviour
{
    // Distância máxima (m) entre o centro do collider e o renderer visível mais
    // próximo. Acima disso o dano é considerado "fantasma" e o collider é
    // desligado. Folga alta o bastante para não afetar obstáculos com pequeno
    // offset de posição, mas pega collider solto a metros do modelo.
    private const float MaxVisualDistance = 4f;

    private void Awake()
    {
        // Regra do projeto: só se leva dano de obstáculo VISÍVEL e próximo.
        // Cobre os dois casos de "dano fantasma":
        //   - wrapper sem nenhum visual (modelo deletado na edição);
        //   - collider largado longe do visual (o modelo foi movido).
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Collider col in GetComponents<Collider>())
        {
            if (col.enabled && NearestVisibleDistance(col, renderers) > MaxVisualDistance)
            {
                col.enabled = false;
            }
        }
    }

    private static float NearestVisibleDistance(Collider col, Renderer[] renderers)
    {
        Vector3 c = col.bounds.center;
        float best = float.MaxValue;
        foreach (Renderer r in renderers)
        {
            if (r != null && r.enabled && r.gameObject.activeInHierarchy)
            {
                float d = Vector3.Distance(c, r.bounds.center);
                if (d < best)
                {
                    best = d;
                }
            }
        }
        return best; // float.MaxValue quando não há renderer visível -> desliga
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage();
        }
    }
}
