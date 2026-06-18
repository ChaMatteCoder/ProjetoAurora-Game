using TMPro;
using UnityEngine;

public class InteractionPromptController : MonoBehaviour
{
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private string defaultPrompt = "PRESSIONE E";

    private void Awake()
    {
        SetVisible(false, string.Empty);
    }

    public void SetVisible(bool visible, string message)
    {
        if (promptRoot != null)
        {
            promptRoot.SetActive(visible);
        }

        if (promptText != null)
        {
            promptText.text = string.IsNullOrWhiteSpace(message) ? defaultPrompt : message;
        }
    }
}
