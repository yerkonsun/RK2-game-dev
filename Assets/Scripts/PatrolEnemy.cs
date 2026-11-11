using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PatrolEnemy : Enemy
{
    public enum State { Patrol, Chase, Attack, Cooldown }
    public State currentState = State.Patrol;

    [Header("Patrol Settings")]
    public float speed = 1.6f;
    public float range = 3f;
    public LayerMask groundMask;

    [Header("Vision & Chase Settings")]
    public float visionRange = 5f;
    public float chaseSpeed = 2.5f;
    public Transform player;
    public LayerMask wallLayer;

    [Header("Attack Settings")]
    public float attackRange = 1.5f;
    public float windUpTime = 0.5f;
    public float cooldownTime = 1.5f;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Animator anim;
    float originX;
    int dir = -1;
    float timer;
    bool isOnCooldown = false;

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
        
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
    }

    void FixedUpdate()
    {
        if (health.IsDead) return;

        switch (currentState)
        {
            case State.Patrol:
                PatrolLogic();
                break;
            case State.Chase:
                ChaseLogic();
                break;
            case State.Attack:
                AttackLogic();
                break;
            case State.Cooldown:
                CooldownLogic();
                break;
        }
    }

    void PatrolLogic()
    {
        Vector2 pos = rb.position;
        bool wall = Physics2D.Raycast(pos + Vector2.up * 0.3f, Vector2.right * dir, 0.7f, groundMask);
        bool ledge = !Physics2D.Raycast(pos + new Vector2(dir * 0.7f, 0.3f), Vector2.down, 1f, groundMask);
        bool outOfRange = (dir > 0 && pos.x - originX >= range) || (dir < 0 && pos.x - originX <= -range);
        
        if (wall || ledge || outOfRange) dir = -dir;

        rb.MovePosition(pos + Vector2.right * dir * speed * Time.fixedDeltaTime);
        sr.flipX = dir > 0; 

        if (!isOnCooldown && player != null && CanSeePlayer())
        {
            currentState = State.Chase;
        }
    }

    void ChaseLogic()
    {
        if (player == null) return;

        Vector2 pos = rb.position;
        Vector2 targetPos = new Vector2(player.position.x, pos.y);
        rb.MovePosition(Vector2.MoveTowards(pos, targetPos, chaseSpeed * Time.fixedDeltaTime));

        dir = player.position.x > pos.x ? 1 : -1;
        sr.flipX = dir > 0;

        if (Vector2.Distance(pos, player.position) > visionRange || !CanSeePlayer())
        {
            currentState = State.Patrol;
            originX = pos.x;
        }
        else if (Vector2.Distance(pos, player.position) <= attackRange)
        {
            currentState = State.Attack;
            timer = windUpTime;
        }
    }

        void AttackLogic()
    {
        rb.linearVelocity = Vector2.zero;
        sr.color = Color.red;
        timer -= Time.fixedDeltaTime;

        if (timer <= 0)
        {
            sr.color = Color.white;
            
            if (player != null)
            {
                PlayerController pc = player.GetComponent<PlayerController>();
                if (pc != null) 
                {
                    base.TouchPlayer(pc);
                }
            }
            
            isOnCooldown = true;
            currentState = State.Cooldown;
            timer = cooldownTime;
        }
    }

    void CooldownLogic()
    {
        rb.linearVelocity = Vector2.zero;
        timer -= Time.fixedDeltaTime;

        if (timer <= 0)
        {
            isOnCooldown = false;
            currentState = State.Chase;
        }
    }

    bool CanSeePlayer()
    {
        Vector2 pos = rb.position;
        if (Vector2.Distance(pos, player.position) > visionRange) return false;

        Vector2 direction = (player.position - (Vector3)pos).normalized;
        RaycastHit2D hit = Physics2D.Raycast(pos, direction, visionRange, wallLayer);

        return hit.collider == null;
    }

    protected override void TouchPlayer(PlayerController player)
    {
        if (currentState == State.Attack || currentState == State.Cooldown)
        {
            base.TouchPlayer(player);
            return;
        }

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