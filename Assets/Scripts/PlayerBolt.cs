using UnityEngine;

public class PlayerBolt : Projectile
{
    protected override bool OnHit(Collider2D other)
    {
        if (IsSolid(other)) return true;

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null && !enemy.Health.IsDead)
        {
            enemy.Hit(damage);
            return true;
        }
        return false;
    }
}
