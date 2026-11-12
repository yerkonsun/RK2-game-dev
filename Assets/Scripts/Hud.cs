using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class Hud : MonoBehaviour
{
    public Image[] hearts;
    public Sprite heartFull;
    public Sprite heartEmpty;
    public Text crystalText;
    public GameObject crystalGroup;
    public Text messageText;
    public GameObject bossPanel;
    public Image bossFill;

    Coroutine messageRoutine;

    void Start()
    {
        var player = FindAnyObjectByType<PlayerController>();
        var health = player.GetComponent<Health>();
        health.Changed += RefreshHearts;
        RefreshHearts(health.Current, health.maxHP);

        var gm = GameManager.Instance;
        if (gm.crystalsNeeded > 0)
        {
            gm.CrystalsChanged += RefreshCrystals;
            RefreshCrystals(gm.Crystals, gm.crystalsNeeded);
        }
        else if (crystalGroup != null)
        {
            crystalGroup.SetActive(false);
        }
        gm.Message += ShowMessage;

        var boss = FindAnyObjectByType<Boss>();
        if (boss != null)
        {
            bossPanel.SetActive(true);
            boss.Health.Changed += RefreshBoss;
            RefreshBoss(boss.Health.Current, boss.Health.maxHP);
        }
        else if (bossPanel != null)
        {
            bossPanel.SetActive(false);
        }

        messageText.text = "";
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.Message -= ShowMessage;
    }

    void RefreshHearts(int current, int max)
    {
        for (int i = 0; i < hearts.Length; i++)
            hearts[i].sprite = i < current ? heartFull : heartEmpty;
    }

    void RefreshCrystals(int current, int needed)
    {
        crystalText.text = Mathf.Min(current, needed) + " / " + needed;
    }

    void RefreshBoss(int current, int max)
    {
        bossFill.fillAmount = (float)current / max;
    }

    void ShowMessage(string text)
    {
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(MessageRoutine(text));
    }

    IEnumerator MessageRoutine(string text)
    {
        messageText.text = text;
        messageText.color = Color.white;
        yield return new WaitForSeconds(3f);
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            messageText.color = new Color(1f, 1f, 1f, 1f - t / 0.5f);
            yield return null;
        }
        messageText.text = "";
    }
}
