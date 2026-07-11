#if UNITY_EDITOR
using System.Reflection;
using UnityEngine;

/// Piloto automático de QA (temporário): corre numa pista fixa, aperta E sozinho e
/// loga cada dano com posição + colliders próximos em playtest_lane.log.
public class AutoPilotRunner : MonoBehaviour
{
    public int lane = 1;                    // 0=esq, 1=centro, 2=dir
    public float testSpeed = 26f;
    private const string PATH = @"C:\ProjetoAurora-Game\playtest_lane.log";

    private float t0;
    private int lastLives = -1;
    private float lastBeat = -999f;
    private bool forced;

    private void Start()
    {
        t0 = Time.realtimeSinceStartup;
        W("=== RUN pista " + lane + " (x=" + ((lane - 1) * 3) + ") ===");
    }

    private void W(string s) { System.IO.File.AppendAllText(PATH, s + "\n"); }

    private void Update()
    {
        float el = Time.realtimeSinceStartup - t0;
        GameManager gm = GameManager.Instance;
        GameObject player = GameObject.Find("Dr. Elias - Player");
        if (gm == null || player == null) return;

        // forca Playing continuamente: triggers de tutorial no percurso re-setam o estado
        if (gm.State != GameState.Playing && gm.State != GameState.GameOver && el > 3f)
        {
            MethodInfo m = typeof(GameManager).GetMethod("SetState", BindingFlags.NonPublic | BindingFlags.Instance);
            m.Invoke(gm, new object[] { GameState.Playing });
            if (!forced) { forced = true; W("forcado Playing em t=" + el.ToString("F0") + "s"); }
        }
        if (gm.State != GameState.Playing) return;

        PlayerRunner runner = player.GetComponent<PlayerRunner>();
        runner.maximumSpeed = testSpeed;
        typeof(PlayerRunner).GetField("currentLane", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(runner, lane);

        // boost de avanco para cobrir o mapa inteiro no teste (soma ao autorun)
        CharacterController cc = player.GetComponent<CharacterController>();
        cc.Move(Vector3.forward * 18f * Time.deltaTime);

        float z = player.transform.position.z;

        foreach (InteractableObject io in FindObjectsByType<InteractableObject>(FindObjectsSortMode.None))
        {
            float dz = io.transform.position.z - z;
            if (dz < -2f || dz > 7f) continue;
            if (io.CanInteract(player)) { io.Interact(player); W("E z=" + z.ToString("F0") + " -> " + io.name); }
        }

        PlayerHealth ph = player.GetComponent<PlayerHealth>();
        int lives = ph.Lives;
        if (lastLives < 0) lastLives = lives;
        if (lives < lastLives)
        {
            string near = "";
            foreach (Collider h in Physics.OverlapBox(player.transform.position + Vector3.up, new Vector3(0.6f, 1f, 0.9f)))
                if (h.isTrigger) near += h.name + "@z" + h.transform.position.z.ToString("F0") + " ";
            W("DANO z=" + z.ToString("F1") + " x=" + player.transform.position.x.ToString("F1") + " vidas " + lastLives + "->" + lives + " perto=[" + near + "]");
            // auto-cura: nunca morre, mapeia o percurso inteiro registrando todos os hits
            while (ph.TryRestoreSegment()) { }
            lastLives = ph.Lives;
        }

        if (z - lastBeat >= 200f) { lastBeat = z; W("... z=" + z.ToString("F0") + " vidas=" + lives + " t=" + el.ToString("F0") + "s"); }

        if (z > 2560f || lives <= 0 || el > 240f)
        {
            W("FIM z=" + z.ToString("F0") + " vidas=" + lives + " t=" + el.ToString("F0") + "s");
            enabled = false;
            UnityEditor.EditorApplication.ExitPlaymode();
        }
    }
}
#endif
