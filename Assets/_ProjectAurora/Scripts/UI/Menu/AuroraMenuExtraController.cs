using UnityEngine;
using UnityEngine.UI;

namespace ProjectAurora.UI.Menu
{
    /// Painel EXTRA: hub com SKIN e LORE (placeholders estruturados para futuras features).
    public class AuroraMenuExtraController : MonoBehaviour
    {
        [SerializeField] private GameObject hubPanel;
        [SerializeField] private GameObject skinPanel;
        [SerializeField] private GameObject lorePanel;
        [SerializeField] private Button skinButton;
        [SerializeField] private Button loreButton;
        [SerializeField] private Button skinBackButton;
        [SerializeField] private Button loreBackButton;

        private void Awake()
        {
            if (skinButton != null) skinButton.onClick.AddListener(() => Show(skinPanel));
            if (loreButton != null) loreButton.onClick.AddListener(() => Show(lorePanel));
            if (skinBackButton != null) skinBackButton.onClick.AddListener(ShowHub);
            if (loreBackButton != null) loreBackButton.onClick.AddListener(ShowHub);
        }

        private void OnEnable()
        {
            ShowHub();
        }

        public bool IsInSubpanel =>
            (skinPanel != null && skinPanel.activeSelf) || (lorePanel != null && lorePanel.activeSelf);

        /// Volta do subpainel para o hub. Retorna true se consumiu a acao.
        public bool BackToHub()
        {
            if (!IsInSubpanel)
            {
                return false;
            }

            ShowHub();
            return true;
        }

        private void ShowHub()
        {
            if (hubPanel != null) hubPanel.SetActive(true);
            if (skinPanel != null) skinPanel.SetActive(false);
            if (lorePanel != null) lorePanel.SetActive(false);
        }

        private void Show(GameObject panel)
        {
            if (hubPanel != null) hubPanel.SetActive(false);
            if (skinPanel != null) skinPanel.SetActive(panel == skinPanel);
            if (lorePanel != null) lorePanel.SetActive(panel == lorePanel);
        }
    }
}
