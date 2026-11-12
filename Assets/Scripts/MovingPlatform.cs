using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    Collider2D col;

    void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.collider.GetComponent<PlayerController>() == null) return;

        bool onTop = collision.transform.position.y >= col.bounds.max.y - 0.15f;
        if (onTop && collision.transform.parent != transform) collision.transform.SetParent(transform);
        else if (!onTop && collision.transform.parent == transform) collision.transform.SetParent(null);
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.transform.parent == transform) collision.transform.SetParent(null);
    }
}
