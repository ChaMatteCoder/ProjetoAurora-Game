using UnityEngine;

public class LaserInteractable : InteractableBase
{
    [Header("Lasers")]
    [SerializeField] private GameObject[] lasersToDisable;
    [SerializeField] private Collider[] damageCollidersToDisable;
    [SerializeField] private Light[] laserLightsToDisable;
    [SerializeField] private Renderer[] renderersToDim;
    [SerializeField] private Color dimColor = new Color(0.08f, 0.1f, 0.12f, 1f);
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip deactivateSfx;
    [SerializeField] private string celestIAMessage = "CELESTIA: Emissores desativados.";

    protected override void HandleInteraction(GameObject interactor)
    {
        // VFX de desligamento ANTES de desativar (precisa da posicao dos feixes vivos).
        // Uma faisca por feixe, com teto de 3 para paineis que desligam muitos lasers.
        if (lasersToDisable != null)
        {
            int burstsSpawned = 0;
            for (int i = 0; i < lasersToDisable.Length && burstsSpawned < 3; i++)
            {
                if (lasersToDisable[i] != null && lasersToDisable[i].activeInHierarchy)
                {
                    ProjectAurora.VFX.AuroraVFXController.LaserShutdown(lasersToDisable[i].transform.position);
                    burstsSpawned++;
                }
            }
        }

        SetGameObjectsActive(lasersToDisable, false);
        SetCollidersEnabled(damageCollidersToDisable, false);
        SetLightsEnabled(laserLightsToDisable, false);
        DimRenderers();
        PlaySfx(audioSource, deactivateSfx);
        NotifyCelestIA(celestIAMessage, "CEL_049");
    }

    private static void SetGameObjectsActive(GameObject[] targets, bool active)
    {
        if (targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                targets[i].SetActive(active);
            }
        }
    }

    private static void SetCollidersEnabled(Collider[] targets, bool enabled)
    {
        if (targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                targets[i].enabled = enabled;
            }
        }
    }

    private static void SetLightsEnabled(Light[] targets, bool enabled)
    {
        if (targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                targets[i].enabled = enabled;
            }
        }
    }

    private void DimRenderers()
    {
        if (renderersToDim == null)
        {
            return;
        }

        for (int i = 0; i < renderersToDim.Length; i++)
        {
            Renderer target = renderersToDim[i];
            if (target == null)
            {
                continue;
            }

            Material material = target.material;
            material.color = dimColor;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", dimColor);
            }
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", Color.black);
            }
        }
    }
}
