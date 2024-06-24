using UnityEngine;

public class PlayerControl : MonoBehaviour
{
    public float speed = 10.0f;

    void Update()
    {
        Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        transform.Translate(move * speed * Time.deltaTime);
    }
}
