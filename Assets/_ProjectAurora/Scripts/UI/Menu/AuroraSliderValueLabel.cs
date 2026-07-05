using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAurora.UI.Menu
{
    /// Mostra o valor de um slider como porcentagem ao lado dele (feedback imediato
    /// nas Configuracoes — Round 10b).
    public class AuroraSliderValueLabel : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Text label;

        private void OnEnable()
        {
            if (slider == null || label == null)
            {
                return;
            }

            slider.onValueChanged.AddListener(UpdateLabel);
            UpdateLabel(slider.value);
        }

        private void OnDisable()
        {
            if (slider != null)
            {
                slider.onValueChanged.RemoveListener(UpdateLabel);
            }
        }

        private void UpdateLabel(float value)
        {
            if (label != null)
            {
                label.text = Mathf.RoundToInt(value * 100f) + "%";
            }
        }
    }
}
