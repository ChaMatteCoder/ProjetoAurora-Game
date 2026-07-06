using UnityEngine;

/// Funil de aproximacao do Terminal Central (Round 14).
/// Ao entrar neste gatilho, o jogador e travado na lane central para nao correr por uma
/// lane lateral e sair do mapa sem acessar o terminal (o trigger de acesso e central).
[RequireComponent(typeof(BoxCollider))]
public class TerminalApproachFunnel : MonoBehaviour
{
    private bool triggered;

    private void Reset()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(16f, 6f, 3f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered)
        {
            return;
        }

        PlayerRunner runner = other.GetComponentInParent<PlayerRunner>();
        if (runner == null)
        {
            return;
        }

        triggered = true;
        runner.SetLaneLockedCenter(true);
    }
}
