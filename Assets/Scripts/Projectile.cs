using UnityEngine;

// Base class for everything that flies: the player bolts and the boss fireballs.
// Subclasses decide in OnHit() what happens when they touch something.
[RequireComponent(typeof(Rigidbody2D))]
public abstract class Projectile : MonoBehaviour
{
    public float speed = 12f;
    public int damage = 1;
    public float lifetime = 3f;

    protected Rigidbody2D rb;
    int groundLayer;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        groundLayer = LayerMask.NameToLayer("Ground");
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
        return other.gameObject.layer == groundLayer;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (OnHit(other)) Destroy(gameObject);
    }

    // return true if the projectile should disappear
    protected abstract bool OnHit(Collider2D other);
}
