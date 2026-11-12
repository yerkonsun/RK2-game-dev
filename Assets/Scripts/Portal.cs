using UnityEngine;

public class Portal : MonoBehaviour
{
    public Sprite lockedSprite;

    SpriteRenderer sr;
    Animator anim;
    bool open;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }

    void Start()
    {
        GameManager gm = GameManager.Instance;
        gm.PortalOpened += Open;
        if (gm.PortalOpen) Open();
        else Lock();
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.PortalOpened -= Open;
    }

    void Lock()
    {
        open = false;
        anim.enabled = false;
        sr.sprite = lockedSprite;
    }

    void Open()
    {
        open = true;
        anim.enabled = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() == null) return;

        if (open) GameManager.Instance.LevelComplete();
        else GameManager.Instance.Say("Collect " + GameManager.Instance.crystalsNeeded + " gold coins to open the temple door");
    }
}
