using UnityEngine;

// Base class for the snakes and the boss. Enemies are kinematic triggers:
// they hurt the player on contact and lose HP through Hit().
[RequireComponent(typeof(Health))]
public abstract class Enemy : MonoBehaviour
{
    public int contactDamage = 1;

    protected Health health;
    public Health Health { get { return health; } }

    protected virtual void Awake()
    {
        health = GetComponent<Health>();
        health.Died += OnDied;
    }

    public virtual void Hit(int amount)
    {
        health.TakeDamage(amount);
    }

    protected abstract void OnDied();

    void OnTriggerStay2D(Collider2D other)
    {
        if (health.IsDead) return;
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null) TouchPlayer(player);
    }

    protected virtual void TouchPlayer(PlayerController player)
    {
        player.Hit(contactDamage, transform.position);
    }
}
