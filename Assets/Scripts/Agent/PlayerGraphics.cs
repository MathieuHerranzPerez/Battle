using UnityEngine;

public class PlayerGraphics : MonoBehaviour
{
    [SerializeField] private NetworkCharacterControllerPrototypeCustom ccc;

    [Space(20)]
    [SerializeField] private ParticleSystem jumpParticle;

    void OnEnable()
    {
        ccc.OnJump += Jump;
    }

    void OnDisable()
    {
        ccc.OnJump -= Jump;
    }

    private void Jump(Vector2 direction)
    {
        Debug.DrawLine(transform.position, transform.position + new Vector3(direction.x, direction.y, 0) * 2, Color.green, 1f);
        jumpParticle.transform.LookAt(transform.position + new Vector3(direction.x, direction.y, 0));
        jumpParticle.Play();
    }
}
