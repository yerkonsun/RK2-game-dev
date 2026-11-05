using UnityEngine;

// Gold coin (counts towards the door) or heart (heals 1 HP). Collected through a trigger.
public class Pickup : MonoBehaviour
{
    public enum Kind { Crystal, Heart }
    public Kind kind = Kind.Crystal;

    float baseY;
    float phase;

    void Start()
    {
        baseY = transform.position.y;
        phase = Random.value * 6.28f;
    }

    void Update()
    {
        // gentle floating
        Vector3 p = transform.position;
        p.y = baseY + Mathf.Sin(Time.time * 2.5f + phase) * 0.08f;
        transform.position = p;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;

        if (kind == Kind.Crystal)
        {
            GameManager.Instance.AddCrystal();
        }
        else
        {
            Health h = player.GetComponent<Health>();
            if (h.Current >= h.maxHP) return; // keep the heart for later
            h.Heal(1);
            Sfx.Play("pickup");
        }
        Destroy(gameObject);
    }
}
