using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss : Enemy
{
    public enum State { Patrol, WindUp, Attacking, Cooldown }
    public State currentState = State.Patrol;

    [Header("Existing Attack Settings")]
    public EnemyProjectile fireballPrefab;
    public GameObject warnPrefab;
    public Transform muzzle;
    public float rainMinX = 1f;
    public float rainMaxX = 14f;
    public float rainTopY = 12f;

    [Header("Movement Settings")]
    public float moveSpeed = 1.5f;
    public float patrolRange = 3f;

    [Header("Vision Settings")]
    public float visionRange = 12f;
    public LayerMask wallLayer;

    [Header("Attack Timing (Fair Attack)")]
    public float windUpTime = 0.7f;
    public float cooldownTime = 2.0f;

    Transform player;
    SpriteRenderer sr;
    float startX;
    int dir = 1;
    bool phase2;
    bool isAttacking;
    
    float timer;
    int attackCounter = 0;

    bool InPhase2 { get { return health.Current <= health.maxHP / 2; } }

    protected override void Awake()
    {
        base.Awake();
        sr = GetComponent<SpriteRenderer>();
        startX = transform.position.x;
    }

    void Start()
    {
        var p = FindAnyObjectByType<PlayerController>();
        if (p != null) player = p.transform;
        
        currentState = State.Patrol;
    }

    void Update()
    {
        if (health.IsDead) return;

        float s = Mathf.Sin(Time.time * 1.6f);
        transform.position = new Vector3(transform.position.x, transform.position.y + s * 0.005f, transform.position.z);

        switch (currentState)
        {
            case State.Patrol:
                PatrolLogic();
                break;
            case State.WindUp:
                WindUpLogic();
                break;
            case State.Cooldown:
                CooldownLogic();
                break;
            case State.Attacking:
                break;
        }
    }

    void PatrolLogic()
    {
        transform.position += new Vector3(dir * moveSpeed * Time.deltaTime, 0f, 0f);
        
        if (transform.position.x > startX + patrolRange) dir = -1;
        else if (transform.position.x < startX - patrolRange) dir = 1;

        if (CanSeePlayer())
        {
            currentState = State.WindUp;
            timer = windUpTime;
        }
    }

    void WindUpLogic()
    {
        sr.color = new Color(1f, 0.4f, 0.4f, 1f); 
        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            sr.color = phase2 ? new Color(1f, 0.55f, 0.55f, 1f) : Color.white;
            currentState = State.Attacking;
            StartCoroutine(ExecuteAttack());
        }
    }

    void CooldownLogic()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            currentState = State.Patrol;
        }
    }

    IEnumerator ExecuteAttack()
    {
        isAttacking = true;
        
        if (attackCounter % 2 == 0) 
            yield return StartCoroutine(FanShot());
        else 
            yield return StartCoroutine(FireRain());
            
        attackCounter++;
        isAttacking = false;
        
        currentState = State.Cooldown;
        timer = cooldownTime; 
    }

    IEnumerator FanShot()
    {
        int volleys = phase2 ? 3 : 2;
        int count = phase2 ? 5 : 3;
        float spread = phase2 ? 70f : 40f;
        for (int v = 0; v < volleys && !health.IsDead; v++)
        {
            Sfx.Play("fireball", 0.8f);
            Vector2 from = muzzle.position;
            Vector2 aim = ((Vector2)player.position + Vector2.up * 0.6f) - from;
            float baseAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            for (int i = 0; i < count; i++)
            {
                float angle = baseAngle + (i - (count - 1) / 2f) * (spread / (count - 1));
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                var fb = Instantiate(fireballPrefab, from, Quaternion.identity);
                fb.Launch(dir);
            }
            yield return new WaitForSeconds(phase2 ? 0.7f : 0.95f);
        }
    }

    IEnumerator FireRain()
    {
        int waves = phase2 ? 2 : 1;
        for (int w = 0; w < waves && !health.IsDead; w++)
        {
            int columns = phase2 ? 5 : 3;
            var xs = new List<float>();
            xs.Add(Mathf.Clamp(player.position.x, rainMinX, rainMaxX));
            int guard = 0;
            while (xs.Count < columns && guard++ < 100)
            {
                float x = Random.Range(rainMinX, rainMaxX);
                bool farEnough = true;
                foreach (float other in xs) if (Mathf.Abs(other - x) < 1.6f) farEnough = false;
                if (farEnough) xs.Add(x);
            }

            var warns = new List<GameObject>();
            foreach (float x in xs)
                warns.Add(Instantiate(warnPrefab, new Vector3(x, 0f, 0f), Quaternion.identity));
            Sfx.Play("warning");

            yield return new WaitForSeconds(phase2 ? 0.8f : 1.1f);

            foreach (var g in warns) Destroy(g);
            Sfx.Play("fireball", 0.8f);
            foreach (float x in xs)
            {
                var fb = Instantiate(fireballPrefab, new Vector3(x, rainTopY, 0f), Quaternion.identity);
                fb.speed = 11f;
                fb.Launch(Vector2.down);
            }
            yield return new WaitForSeconds(0.9f);
        }
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;
        if (Vector2.Distance(transform.position, player.position) > visionRange) return false;

        Vector2 direction = (player.position - transform.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, visionRange, wallLayer);

        return hit.collider == null;
    }

    public override void Hit(int amount)
    {
        if (health.IsDead) return;
        health.TakeDamage(amount);
        Sfx.Play("boss_hit", 0.9f);
        if (!phase2 && InPhase2 && !health.IsDead)
        {
            phase2 = true;
            sr.color = new Color(1f, 0.55f, 0.55f, 1f);
            Sfx.Play("warning");
        }
    }

    protected override void OnDied()
    {
        StopAllCoroutines();
        foreach (var fb in FindObjectsByType<EnemyProjectile>()) Destroy(fb.gameObject);
        GetComponent<Collider2D>().enabled = false;
        Sfx.Play("enemy_die");
        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        float t = 0f;
        Vector3 deathPos = transform.position;
        while (t < 1.4f)
        {
            t += Time.deltaTime;
            float shake = Mathf.Sin(t * 60f) * 0.1f;
            transform.position = deathPos + new Vector3(shake, 0f, 0f);
            var c = sr.color;
            c.a = Mathf.Clamp01(1f - t / 1.4f);
            sr.color = c;
            yield return null;
        }
        if (GameManager.Instance != null) GameManager.Instance.BossDefeated();
        Destroy(gameObject);
    }
}