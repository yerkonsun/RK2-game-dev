using System.Collections;
using UnityEngine;

// The temple treasure. Hidden while the guardian is alive; GameManager.BossDefeated() calls Reveal():
// the chest drops in, opens and bursts into gold coins.
public class Treasure : MonoBehaviour
{
    public Sprite closedSprite;
    public Sprite openSprite;
    public Sprite coinSprite;

    SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = closedSprite;
        sr.enabled = false;
    }

    public void Reveal()
    {
        StartCoroutine(RevealRoutine());
    }

    IEnumerator RevealRoutine()
    {
        sr.enabled = true;
        Vector3 end = transform.position;
        Vector3 start = end + Vector3.up * 4f;
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / 0.5f);
            transform.position = Vector3.Lerp(start, end, k * k);
            yield return null;
        }
        transform.position = end;
        Sfx.Play("portal");
        yield return new WaitForSeconds(0.5f);

        sr.sprite = openSprite;
        Sfx.Play("pickup");
        for (int i = 0; i < 18; i++) StartCoroutine(Coin(end + Vector3.up * 1.2f));
    }

    // one flying gold coin
    IEnumerator Coin(Vector3 from)
    {
        var go = new GameObject("TreasureCoin");
        go.transform.position = from;
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = coinSprite;
        r.sortingOrder = 12;
        r.sharedMaterial = sr.sharedMaterial;
        Vector2 vel = new Vector2(Random.Range(-4f, 4f), Random.Range(5f, 9f));
        float t = 0f;
        while (t < 1.2f)
        {
            t += Time.deltaTime;
            vel.y -= 18f * Time.deltaTime;
            go.transform.position += (Vector3)(vel * Time.deltaTime);
            var c = r.color; c.a = Mathf.Clamp01((1.2f - t) / 0.4f); r.color = c;
            yield return null;
        }
        Destroy(go);
    }
}
