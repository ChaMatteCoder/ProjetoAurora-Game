using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 5f, -8f);
    public Vector3 lookOffset = new Vector3(1.1f, 1.2f, 7f);
    public float positionSmooth = 8f;
    public float rotationSmooth = 7f;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, positionSmooth * Time.deltaTime);
        Quaternion look = Quaternion.LookRotation(target.position + lookOffset - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, rotationSmooth * Time.deltaTime);
    }
}
