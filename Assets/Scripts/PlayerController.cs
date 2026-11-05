using UnityEngine;
using UnityEngine.InputSystem;

// Player movement, jumping and shooting.
// PlayerInput (Behavior = Send Messages) calls OnMove / OnJump / OnFire.
// The same actions are fed by the keyboard and by the on-screen stick/buttons.
[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 7f;
    public float jumpVelocity = 15f;
    public float coyoteTime = 0.1f;
    public float jumpBuffer = 0.12f;
    public LayerMask groundMask;

    // Shotgun: one shot = a fan of short-range pellets. Deadly up close, weak from far away.
    public Projectile boltPrefab;
    public float fireCooldown = 0.7f;
    public int pellets = 5;
    public float spreadAngle = 16f;     // total width of the fan in degrees
    public float pelletSpeed = 17f;
    public float pelletLifetime = 0.5f; // short life = limited range
    public float recoil = 2.5f;

    public bool Grounded { get; private set; }
    public bool Dead { get; private set; }

    Rigidbody2D rb;
    SpriteRenderer sr;
    Animator anim;
    Health health;

    Vector2 move;
    float lastGrounded = -10f;
    float lastJumpPressed = -10f;
    float nextFire;
    float knockUntil;
    int facing = 1;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        health = GetComponent<Health>();
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    // ---- Input System messages ----
    void OnMove(InputValue value) { move = value.Get<Vector2>(); }

    void OnJump(InputValue value)
    {
        if (Time.timeScale == 0f) return; // paused
        if (value.isPressed)
        {
            lastJumpPressed = Time.time;
        }
        else
        {
            // releasing the button early cuts the jump short
            if (rb.linearVelocity.y > 0f)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }
    }

    // one shot per press (cooldown still applies), so a missed "button released" can never lock the gun
    void OnFire(InputValue value)
    {
        if (Time.timeScale == 0f) return; // paused
        if (value.isPressed && !Dead && Time.time >= nextFire) Shoot();
    }

    void Update()
    {
        if (Dead) return;

        anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        anim.SetBool("Grounded", Grounded);
    }

    void FixedUpdate()
    {
        if (Dead) return;

        Grounded = Physics2D.OverlapBox(rb.position, new Vector2(0.6f, 0.2f), 0f, groundMask) != null;
        if (Grounded) lastGrounded = Time.time;

        // stick gives analog values: below 0.2 is a dead zone, above 0.7 is full speed
        float raw = move.x;
        float x = 0f;
        if (Mathf.Abs(raw) > 0.2f)
            x = Mathf.Sign(raw) * Mathf.Clamp01((Mathf.Abs(raw) - 0.2f) / 0.5f);

        Vector2 vel = rb.linearVelocity;
        if (Time.time >= knockUntil) vel.x = x * moveSpeed;

        // jump buffer + coyote time make jumps forgiving, especially on a touch screen
        if (Time.time - lastJumpPressed <= jumpBuffer && Time.time - lastGrounded <= coyoteTime)
        {
            vel.y = jumpVelocity;
            lastJumpPressed = -10f;
            lastGrounded = -10f;
            Sfx.Play("jump", 0.8f);
        }
        rb.linearVelocity = vel;

        if (x > 0.01f) facing = 1;
        else if (x < -0.01f) facing = -1;
        sr.flipX = facing < 0;
    }

    void Shoot()
    {
        nextFire = Time.time + fireCooldown;
        Vector3 pos = transform.position + new Vector3(facing * 1.0f, 0.72f, 0f);
        for (int i = 0; i < pellets; i++)
        {
            // evenly spread fan + a little randomness so it feels like real buckshot
            float t = pellets == 1 ? 0f : (i / (float)(pellets - 1)) - 0.5f;
            float angle = t * spreadAngle + Random.Range(-2f, 2f);
            Vector2 dir = Quaternion.Euler(0f, 0f, angle) * new Vector2(facing, 0f);
            Projectile p = Instantiate(boltPrefab, pos, Quaternion.identity);
            p.speed = pelletSpeed * Random.Range(0.9f, 1.1f);
            p.lifetime = pelletLifetime * Random.Range(0.85f, 1.15f);
            p.Launch(dir);
        }
        // small kick backwards
        if (Time.time >= knockUntil)
        {
            rb.linearVelocity = new Vector2(-facing * recoil, rb.linearVelocity.y);
            knockUntil = Time.time + 0.06f;
        }
        Sfx.Play("shoot", 0.9f);
    }

    // Every damage source calls this: damage + knockback away from the source.
    public void Hit(int damage, Vector2 sourcePosition)
    {
        if (Dead) return;
        if (!health.TakeDamage(damage)) return;
        if (health.IsDead) return;

        float dir = transform.position.x >= sourcePosition.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * 6f, 9f);
        knockUntil = Time.time + 0.25f;
    }

    public void Bounce(float speed)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, speed);
    }

    void OnDamaged() { Sfx.Play("hurt"); }

    void OnDied()
    {
        Dead = true;
        anim.enabled = false;
        sr.color = new Color(1f, 0.6f, 0.6f, 1f);
        GetComponent<Collider2D>().enabled = false;
        rb.linearVelocity = new Vector2(0f, 10f);
        if (GameManager.Instance != null) GameManager.Instance.PlayerDied();
    }
}
