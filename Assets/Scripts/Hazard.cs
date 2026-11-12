using UnityEngine;

public class Hazard : MonoBehaviour
{
    public int damage = 1;
    public bool instantKill = false;

    void OnTriggerStay2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null || player.Dead) return;

        if (instantKill) player.GetComponent<Health>().Kill();
        else player.Hit(damage, transform.position);
    }
}
