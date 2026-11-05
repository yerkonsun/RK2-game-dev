using System;
using System.Collections;
using UnityEngine;

// Hit points shared by the player, enemies and the boss.
// invincibleTime > 0 gives the player a blinking invincibility window after a hit,
// hitFlash > 0 makes the sprite flash red for a moment (enemies, boss).
public class Health : MonoBehaviour
{
    public int maxHP = 3;
    public float invincibleTime = 0f;
    public float hitFlash = 0f;

    public int Current { get; private set; }
    public bool IsDead { get; private set; }
    public bool Invincible { get; private set; }

    public event Action<int, int> Changed;
    public event Action Damaged;
    public event Action Died;

    SpriteRenderer sr;
    Color baseColor;

    void Awake()
    {
        Current = maxHP;
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
    }

    public bool TakeDamage(int amount)
    {
        if (IsDead || Invincible) return false;

        Current = Mathf.Max(0, Current - amount);
        Changed?.Invoke(Current, maxHP);
        Damaged?.Invoke();

        if (Current == 0)
        {
            IsDead = true;
            Died?.Invoke();
            return true;
        }

        if (invincibleTime > 0f) StartCoroutine(InvincibilityRoutine());
        else if (hitFlash > 0f) StartCoroutine(FlashRoutine());
        return true;
    }

    public void Kill()
    {
        if (IsDead) return;
        Current = 0;
        IsDead = true;
        Changed?.Invoke(Current, maxHP);
        Died?.Invoke();
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        Current = Mathf.Min(maxHP, Current + amount);
        Changed?.Invoke(Current, maxHP);
    }

    // Blink while invincible: alternate the sprite alpha every 0.1 s.
    IEnumerator InvincibilityRoutine()
    {
        Invincible = true;
        float t = 0f;
        while (t < invincibleTime)
        {
            bool faded = Mathf.FloorToInt(t / 0.1f) % 2 == 0;
            SetAlpha(faded ? 0.3f : 1f);
            t += Time.deltaTime;
            yield return null;
        }
        SetAlpha(1f);
        Invincible = false;
    }

    IEnumerator FlashRoutine()
    {
        if (sr != null) sr.color = new Color(1f, 0.35f, 0.35f, baseColor.a);
        yield return new WaitForSeconds(hitFlash);
        if (sr != null) sr.color = baseColor;
    }

    void SetAlpha(float a)
    {
        if (sr == null) return;
        var c = sr.color;
        c.a = a;
        sr.color = c;
    }
}
