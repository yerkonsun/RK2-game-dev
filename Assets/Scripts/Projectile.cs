using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class Projectile : MonoBehaviour
{
    public float speed = 12f;
    public int damage = 1;
    public float lifetime = 3f;

    protected Rigidbody2D rb;
    int groundLayer;
    int wallLayer;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        groundLayer = LayerMask.NameToLayer("Ground");
        wallLayer = LayerMask.NameToLayer("Wall");
    }

    public virtual void Launch(Vector2 direction)
    {
        direction.Normalize();
        rb.linearVelocity = direction * speed;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        Destroy(gameObject, lifetime);
    }

    protected bool IsSolid(Collider2D other)
    {
        int layer = other.gameObject.layer;
        return layer == groundLayer || layer == wallLayer;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (OnHit(other)) Destroy(gameObject);
    }

    protected abstract bool OnHit(Collider2D other);
}