using UnityEngine;

public class Rotationer : MonoBehaviour
{
    [SerializeField] private Vector3 values;

    void Update()
    {
        transform.Rotate(values * Time.deltaTime);
    }
}
