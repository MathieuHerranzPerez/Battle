using UnityEngine;

public class RotateToFollowMovement : MonoBehaviour
{
    private Vector3 previousPosition = Vector3.zero;

    void Awake()
    {
        previousPosition = transform.position;
    }

    void Update()
    {
        Vector3 dif = transform.position - previousPosition;
        float angle = Mathf.Atan2(dif.y, dif.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        previousPosition = transform.position;
    }
}
