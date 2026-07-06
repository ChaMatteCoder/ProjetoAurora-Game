using UnityEngine;

/// Zona de sprint final (Round 15): ao entrar (corredor antes do Núcleo), aumenta a
/// velocidade do Dr. Elias com transição suave, dando urgência ao clímax.
[RequireComponent(typeof(BoxCollider))]
public class FinalSprintZone : MonoBehaviour
{
    [Tooltip("Multiplicador de velocidade alvo (ex.: 1.2 = +20%).")]
    public float sprintMultiplier = 1.2f;
    public bool once = true;

    private bool triggered;

    private void Reset()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(16f, 6f, 3f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (once && triggered)
        {
            return;
        }

        PlayerRunner runner = other.GetComponentInParent<PlayerRunner>();
        if (runner == null)
        {
            return;
        }

        triggered = true;
        runner.SetFinalSprint(Mathf.Max(1f, sprintMultiplier));
    }
}
