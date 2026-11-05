using UnityEngine;

// Boss fireball: damages the player, disappears on walls and floor.
public class EnemyProjectile : Projectile
{
    protected override bool OnHit(Collider2D other)
    {
        if (IsSolid(other)) return true;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            player.Hit(damage, transform.position);
            return true;
        }
        return false;
    }
}
