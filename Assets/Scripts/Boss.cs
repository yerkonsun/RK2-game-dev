using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The Temple Guardian. Alternates two attacks with a pause between them (the window to shoot back):
//   FanShot  - a fan of magma orbs aimed at the player
//   FireRain - red columns with a "!" warn the player, then magma falls down them
// Below half HP (phase 2) the pauses are shorter and the attacks are bigger.
public class Boss : Enemy
{
    public EnemyProjectile fireballPrefab;
    public GameObject warnPrefab;
    public Transform muzzle;
    public float rainMinX = 1f;
    public float rainMaxX = 14f;
    public float rainTopY = 12f;

    Transform player;
    SpriteRenderer sr;
    Vector3 basePos;
    bool phase2;

    bool InPhase2 { get { return health.Current <= health.maxHP / 2; } }

    protected override void Awake()
    {
        base.Awake();
        sr = GetComponent<SpriteRenderer>();
        basePos = transform.position;
    }

    void Start()
    {
        var p = FindAnyObjectByType<PlayerController>();
        if (p != null) player = p.transform;
        StartCoroutine(AttackLoop());
    }

    void Update()
    {
        if (health.IsDead) return;
        // idle sway so the boss feels alive
        float s = Mathf.Sin(Time.time * 1.6f);
        transform.position = basePos + new Vector3(0f, s * 0.08f, 0f);
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

    IEnumerator AttackLoop()
    {
        yield return new WaitForSeconds(2f);
        int n = 0;
        while (!health.IsDead)
        {
            if (n % 2 == 0) yield return FanShot();
            else yield return FireRain();
            n++;
            // pause between attacks: a window to run up and shoot
            yield return new WaitForSeconds(phase2 ? 1.6f : 2.6f);
        }
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
            // one column always lands near the player, so the warning matters
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
        while (t < 1.4f)
        {
            t += Time.deltaTime;
            float shake = Mathf.Sin(t * 60f) * 0.1f;
            transform.position = basePos + new Vector3(shake, 0f, 0f);
            var c = sr.color;
            c.a = Mathf.Clamp01(1f - t / 1.4f);
            sr.color = c;
            yield return null;
        }
        if (GameManager.Instance != null) GameManager.Instance.BossDefeated();
        Destroy(gameObject);
    }
}
