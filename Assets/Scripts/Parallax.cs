using UnityEngine;

// Background layer that follows the camera partly: factor 1 = glued to the screen, 0 = fixed in the world.
// The layer stays attached to the bottom of the screen (bottomOffset is measured from it).
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
