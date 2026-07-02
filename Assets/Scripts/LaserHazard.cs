using UnityEngine;

public class LaserHazard : MonoBehaviour
{
    public bool isActive = true;
    public int damage = 1;
    public GameObject visual;
    public Collider damageCollider;
    public Color activeColor = new Color(1f, 0.02f, 0.02f);
    public Color inactiveColor = new Color(0.12f, 0.12f, 0.12f);

    public void Deactivate()
    {
        isActive = false;
        if (damageCollider != null)
        {
            damageCollider.enabled = false;
        }

        SetColor(inactiveColor);
    }

    public void Activate()
    {
        isActive = true;
        if (damageCollider != null)
        {
            damageCollider.enabled = true;
        }

        SetColor(activeColor);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isActive)
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            health?.TakeDamage();
        }
    }

    private void SetColor(Color color)
    {
        if (visual == null)
        {
            return;
        }

        // Cobre visuais compostos (varios feixes sob um pai) e materiais emissivos:
        // sem atualizar _EmissionColor, um laser desativado continuaria brilhando.
        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
        {
            renderer.material.color = color;
            if (renderer.material.HasProperty("_BaseColor"))
            {
                renderer.material.SetColor("_BaseColor", color);
            }
            if (renderer.material.IsKeywordEnabled("_EMISSION"))
            {
                renderer.material.SetColor("_EmissionColor", color * 2.5f);
            }
        }
    }
}
