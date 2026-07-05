using UnityEngine;

/// Gatilho de aproximacao das portas de transicao de setor (Round 11).
/// Abre a porta automaticamente com antecedencia quando o Dr. Elias se aproxima.
[RequireComponent(typeof(BoxCollider))]
public class AuroraSectorDoorTrigger : MonoBehaviour
{
    public AuroraDoorController targetDoor;
    [Tooltip("Distancia (m) antes da porta em que o trigger fica posicionado.")]
    public float openDistance = 30f;
    public string sectorFrom;
    public string sectorTo;
    public bool openOnce = true;
    [Tooltip("Opcional: fecha a porta depois que o player passa (nao usado nas transicoes padrao).")]
    public bool autoCloseAfterPlayer;
    public float autoCloseDelay = 3f;

    private bool triggered;

    private void Reset()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(14f, 6f, 3f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((openOnce && triggered) || targetDoor == null)
        {
            return;
        }

        if (other.GetComponentInParent<PlayerRunner>() == null)
        {
            return;
        }

        triggered = true;
        targetDoor.OnApproachOpen();

        if (autoCloseAfterPlayer)
        {
            Invoke(nameof(CloseDoor), Mathf.Max(1f, autoCloseDelay));
        }
    }

    private void CloseDoor()
    {
        targetDoor?.Close();
    }
}
