using UnityEngine;

// Temple snake: slithers back and forth, turns at walls and ledges.
// Jumping on its head also hurts it (stomp).
[RequireComponent(typeof(Rigidbody2D))]
public class PatrolEnemy : Enemy
{
    public float speed = 1.6f;
    public float range = 3f;
    public LayerMask groundMask;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Animator anim;
    float originX;
    int dir = -1;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }

    void Start()
    {
        originX = transform.position.x;
    }

    void FixedUpdate()
    {
        if (health.IsDead) return;

        Vector2 pos = rb.position;
        bool wall = Physics2D.Raycast(pos + Vector2.up * 0.3f, Vector2.right * dir, 0.7f, groundMask);
        bool ledge = !Physics2D.Raycast(pos + new Vector2(dir * 0.7f, 0.3f), Vector2.down, 1f, groundMask);
        bool outOfRange = (dir > 0 && pos.x - originX >= range) || (dir < 0 && pos.x - originX <= -range);
        if (wall || ledge || outOfRange) dir = -dir;

        rb.MovePosition(pos + Vector2.right * dir * speed * Time.fixedDeltaTime);
        sr.flipX = dir > 0; // the sprite looks to the left by default
    }

    protected override void TouchPlayer(PlayerController player)
    {
        bool fallingOnTop = player.transform.position.y > transform.position.y + 0.6f
                            && player.GetComponent<Rigidbody2D>().linearVelocity.y < 0.5f;
        if (fallingOnTop)
        {
            Hit(2);
            player.Bounce(11f);
        }
        else
        {
            base.TouchPlayer(player);
        }
    }

    protected override void OnDied()
    {
        Sfx.Play("enemy_die", 0.8f);
        GetComponent<Collider2D>().enabled = false;
        anim.SetTrigger("Die");
        enabled = false;
        Destroy(gameObject, 0.8f);
    }
}
