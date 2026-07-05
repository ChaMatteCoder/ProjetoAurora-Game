using UnityEngine;

/// Aplica as configuracoes persistidas no inicio da cena (Menu e Gameplay),
/// sem tocar em codigo de gameplay.
public class AuroraSettingsApplier : MonoBehaviour
{
    private void Start()
    {
        AuroraSettingsService.ApplyAll();
    }
}
