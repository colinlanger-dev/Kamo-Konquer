using UnityEngine;

public class CameraScript : MonoBehaviour
{
    Rigidbody rb;
    public Vector2 moveInput;
    public float speed = 3;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    
    void Update()
    {
        float moveInputHor = Input.GetAxisRaw("Horizontal");
        float moveInputVer = Input.GetAxisRaw("Vertikal");

        rb.linearVelocity = new Vector3(
            moveInputHor,
            rb.linearVelocity.y,
            moveInputVer
        );
    }
}
