using UnityEngine;

// Smooth side-scrolling camera that stays inside the level bounds.
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    public float minX = 0f;
    public float maxX = 80f;
    public float minY = 0.5f;
    public float maxY = 6f;
    public float yOffset = 1.8f;
    public float smooth = 8f;

    Camera cam;
    Transform target;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        FindTarget();
        if (target != null) transform.position = Desired();
    }

    void FindTarget()
    {
        var p = FindAnyObjectByType<PlayerController>();
        if (p != null) target = p.transform;
    }

    Vector3 Desired()
    {
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        float x = target.position.x;
        float y = target.position.y + yOffset;

        // if the level is narrower than the screen just centre it
        float xLo = minX + halfW, xHi = maxX - halfW;
        x = xLo > xHi ? (minX + maxX) / 2f : Mathf.Clamp(x, xLo, xHi);
        y = Mathf.Clamp(y, minY, maxY);
        return new Vector3(x, y, transform.position.z);
    }

    void LateUpdate()
    {
        if (target == null) { FindTarget(); return; }
        Vector3 d = Desired();
        transform.position = Vector3.Lerp(transform.position, d, 1f - Mathf.Exp(-smooth * Time.deltaTime));
    }
}
