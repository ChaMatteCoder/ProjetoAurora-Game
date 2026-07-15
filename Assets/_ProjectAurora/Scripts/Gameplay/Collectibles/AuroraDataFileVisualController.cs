using UnityEngine;

[DisallowMultipleComponent]
public sealed class AuroraDataFileVisualController : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 34f;
    [SerializeField] private float bobHeight = 0.09f;
    [SerializeField] private float bobSpeed = 2.2f;

    private Vector3 origin;

    private void Awake()
    {
        origin = transform.localPosition;
    }

    private void OnEnable()
    {
        origin = transform.localPosition;
    }

    private void Update()
    {
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.Self);
        Vector3 position = origin;
        position.y += Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.localPosition = position;
    }
}
