using System.Collections;
using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
public class BatEnemy : Enemy
{
    public enum State { Sleep = 0, WindUp = 1, Dash = 2, Cooldown = 3 }

    [Header("Vision")]
    public float wakeRange = 6f;
    public LayerMask groundMask;

    [Header("Attack")]
    public float windUpTime = 0.6f;
    public float shakeAmount = 0.06f;     
    public float dashSpeed = 10f;
    public float dashDistance = 8f;       
    public float dashRadius = 0.35f;      

    [Header("Rest")]
    public float cooldownTime = 2.5f;     
    public float returnSpeed = 3f;        

    public State Current { get { return state; } }

    Rigidbody2D rb;
    Animator anim;
    PlayerController player;
    Vector2 home;
    Vector2 dashDir;
    float timer;
    float dashTravelled;
    State state = State.Sleep;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Start()
    {
        home = rb.position;
        player = FindAnyObjectByType<PlayerController>();
        SetState(State.Sleep);
    }

    void SetState(State next)
    {
        state = next;
        timer = 0f;
        if (anim != null) anim.SetInteger("State", (int)next);

        if (next == State.WindUp) Sfx.Play("warning", 0.6f);
        if (next == State.Dash)
        {
            dashTravelled = 0f;
            Vector2 target = (Vector2)player.transform.position + Vector2.up * 0.3f;
            dashDir = (target - rb.position).normalized; // locked: the player can dodge it
        }
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;
        Vector2 from = rb.position;
        Vector2 to = player.transform.position;
        if (Vector2.Distance(from, to) > wakeRange) return false;
        return !Physics2D.Linecast(from, to, groundMask);
    }

    void FixedUpdate()
    {
        if (health.IsDead) return;
        timer += Time.fixedDeltaTime;

        switch (state)
        {
            case State.Sleep:    DoSleep();    break;
            case State.WindUp:   DoWindUp();   break;
            case State.Dash:     DoDash();     break;
            case State.Cooldown: DoCooldown(); break;
        }
    }

    void DoSleep()
    {
        if (CanSeePlayer()) SetState(State.WindUp);
    }

    void DoWindUp()
    {
        Vector2 shake = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * shakeAmount;
        rb.MovePosition(home + shake);
        if (timer >= windUpTime) SetState(State.Dash);
    }

    void DoDash()
    {
        float step = dashSpeed * Time.fixedDeltaTime;
        bool wall = Physics2D.CircleCast(rb.position, dashRadius, dashDir, step, groundMask);
        if (wall || dashTravelled >= dashDistance)
        {
            SetState(State.Cooldown);
            return;
        }
        rb.MovePosition(rb.position + dashDir * step);
        dashTravelled += step;
    }

    void DoCooldown()
    {
        Vector2 pos = rb.position;
        rb.MovePosition(Vector2.MoveTowards(pos, home, returnSpeed * Time.fixedDeltaTime));
        bool atHome = Vector2.Distance(pos, home) < 0.05f;
        if (timer >= cooldownTime && atHome) SetState(State.Sleep);
    }

    protected override void TouchPlayer(PlayerController player)
    {
        if (state == State.Sleep) return;
        base.TouchPlayer(player);
    }

    protected override void OnDied()
    {
        Sfx.Play("enemy_die", 0.8f);
        GetComponent<Collider2D>().enabled = false;
        StartCoroutine(FallAndFade());
    }

    IEnumerator FallAndFade()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            transform.position += Vector3.down * 6f * Time.deltaTime;
            if (sr != null) { Color c = sr.color; c.a = 1f - t / 0.6f; sr.color = c; }
            yield return null;
        }
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, wakeRange);
    }
}
