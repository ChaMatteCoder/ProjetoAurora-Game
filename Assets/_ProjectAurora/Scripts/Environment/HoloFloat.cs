using UnityEngine;

/// Flutuação de holograma (DataFile): giro contínuo + bob senoidal em Y.
/// Substitui o AC_DataFile_Float do FBX — as curvas de posição do clip são
/// multiplicadas pelo globalScale do importador (100, FBX em cm), o que
/// transformava o bob de ±5cm em ±5m e afundava o card no chão em play mode.
/// Procedural = imune à escala de importação.
public class HoloFloat : MonoBehaviour
{
    public float spinDegPerSec = 55f;
    public float bobAmplitude = 0.12f;
    public float bobHz = 0.55f;

    private Vector3 basePos;
    private float phase;

    private void Awake()
    {
        basePos = transform.localPosition;
        // dessincroniza instâncias vizinhas
        phase = Mathf.Repeat(transform.position.z * 0.61f, 2f * Mathf.PI);
    }

    private void Update()
    {
        float y = bobAmplitude * Mathf.Sin(phase + Time.time * bobHz * 2f * Mathf.PI);
        transform.localPosition = basePos + Vector3.up * y;
        transform.Rotate(0f, spinDegPerSec * Time.deltaTime, 0f, Space.World);
    }
}
