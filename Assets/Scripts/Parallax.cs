using UnityEngine;

public class Parallax : MonoBehaviour
{
    public float factor = 0.3f;
    public float bottomOffset = 0f;

    Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (cam == null) return;
        Vector3 c = cam.transform.position;
        transform.position = new Vector3(c.x * factor, c.y - cam.orthographicSize + bottomOffset, 0f);
    }
}
