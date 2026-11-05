using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

// Play-mode smoke tests: walks through every scene, drives the hero with a virtual keyboard
// and saves screenshots to the folder given in the SHOTS environment variable.
public class SmokeTests
{
    Keyboard kb;
    string shots;

    [SetUp]
    public void SetUp()
    {
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        kb = InputSystem.GetDevice<Keyboard>() ?? InputSystem.AddDevice<Keyboard>();
        shots = System.Environment.GetEnvironmentVariable("SHOTS");
        if (string.IsNullOrEmpty(shots)) shots = Path.Combine(Path.GetTempPath(), "shots");
        Directory.CreateDirectory(shots);
    }

    IEnumerator Load(string scene)
    {
        Debug.Log("[TEST] load " + scene);
        SceneManager.LoadScene(scene);
        yield return null;
        if (Camera.main != null) Camera.main.aspect = 16f / 9f;
        yield return new WaitForSeconds(0.8f);
    }

    void Keys(params Key[] keys)
    {
        InputSystem.QueueStateEvent(kb, new KeyboardState(keys));
    }

    // Renders the main camera (and the HUD, temporarily switched to camera space) into a PNG.
    IEnumerator Shot(string name)
    {
        Debug.Log("[TEST] shot " + name);
        var cam = Camera.main;
        var canvases = Object.FindObjectsByType<Canvas>();
        var modes = new RenderMode[canvases.Length];
        for (int i = 0; i < canvases.Length; i++)
        {
            modes[i] = canvases[i].renderMode;
            if (canvases[i].renderMode == RenderMode.ScreenSpaceOverlay)
            {
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = cam;
                canvases[i].planeDistance = 1f;
            }
        }
        yield return null;
        var rt = new RenderTexture(1600, 900, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(shots, name + ".png"), tex.EncodeToPNG());
        cam.targetTexture = null;
        RenderTexture.active = null;
        Object.Destroy(rt);
        Object.Destroy(tex);
        for (int i = 0; i < canvases.Length; i++)
            if (canvases[i] != null) canvases[i].renderMode = modes[i];
        yield return null;
    }

    GameObject Player() { return GameObject.Find("Player"); }

    void Teleport(Vector2 pos)
    {
        var p = Player();
        p.transform.position = pos;
        var rb = p.GetComponent<Rigidbody2D>();
        rb.position = pos;
        rb.linearVelocity = Vector2.zero;
    }

    int Hp()
    {
        var h = Player().GetComponent("Health");
        return (int)h.GetType().GetProperty("Current").GetValue(h);
    }

    IEnumerator CollectAllAndExit(string expectNext)
    {
        // teleport onto every crystal
        int guard = 0;
        while (guard++ < 40)
        {
            GameObject crystal = null;
            foreach (var go in Object.FindObjectsByType<Transform>())
                if (go.name == "Crystal") { crystal = go.gameObject; break; }
            if (crystal == null) break;
            Teleport(crystal.transform.position);
            yield return new WaitForSeconds(0.12f);
        }
        yield return Shot(SceneManager.GetActiveScene().name + "_portal_open");
        GameObject portal = GameObject.Find("Portal");
        Assert.NotNull(portal, "portal missing");
        Teleport(portal.transform.position + Vector3.up * 0.5f);
        yield return new WaitForSeconds(2.0f);
        Assert.AreEqual(expectNext, SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator MenuScreens()
    {
        yield return Load("MainMenu");
        yield return Shot("menu");
        var play = GameObject.Find("Button_PLAY");
        Assert.NotNull(play);
        play.GetComponent<Button>().onClick.Invoke();
        yield return new WaitForSeconds(1.5f);
        Assert.AreEqual("Level1", SceneManager.GetActiveScene().name);

        yield return Load("Win");
        yield return Shot("win");
    }

    [UnityTest]
    public IEnumerator Shotgun_KillsSnake()
    {
        yield return Load("Level1");
        Teleport(new Vector2(22f, 0.1f));
        Keys(Key.X);
        yield return new WaitForSeconds(4f);
        Keys();
        yield return new WaitForSeconds(1f);
        foreach (var t in Object.FindObjectsByType<Transform>())
            if (t.name == "Snake") Assert.Greater(t.position.x, 45f, "first snake should be killed by the shotgun");
    }

    [UnityTest]
    public IEnumerator Bolts_DefeatBoss()
    {
        yield return Load("Level3");
        var health = Player().GetComponent("Health");
        health.GetType().GetField("maxHP").SetValue(health, 999);
        health.GetType().GetMethod("Heal").Invoke(health, new object[] { 999 });
        Teleport(new Vector2(12.5f, 0.1f));
        Keys(Key.X);
        float t = 0;
        while (t < 60f && SceneManager.GetActiveScene().name != "Win")
        {
            t += Time.deltaTime;
            yield return null;
        }
        Keys();
        Debug.Log("[TEST] boss fight took " + t);
        Assert.AreEqual("Win", SceneManager.GetActiveScene().name, "boss should die from bolts");
    }

    [UnityTest]
    public IEnumerator Level1_Mechanics()
    {
        yield return Load("Level1");
        yield return Shot("L1_start");

        float x0 = Player().transform.position.x;
        Keys(Key.D);
        yield return new WaitForSeconds(1.0f);
        float x1 = Player().transform.position.x;
        Assert.Greater(x1, x0 + 4f, "hero should run right");
        Keys();
        yield return new WaitForSeconds(0.3f);

        Keys(Key.Space);
        yield return new WaitForSeconds(0.25f);
        Assert.Greater(Player().transform.position.y, 0.8f, "hero should jump");
        yield return Shot("L1_jump");
        Keys();
        yield return new WaitForSeconds(1.0f);

        Keys(Key.X);
        yield return new WaitForSeconds(0.1f);
        Assert.NotNull(GameObject.Find("Bolt(Clone)"), "shotgun pellets should be spawned");
        Keys();

        // spikes hurt
        Teleport(new Vector2(26.5f, 0.2f));
        int before = Hp();
        yield return new WaitForSeconds(0.4f);
        Assert.Less(Hp(), before, "spikes should hurt");
        yield return Shot("L1_hurt");

        // fall into a pit -> level restarts
        yield return new WaitForSeconds(1.5f);
        Teleport(new Vector2(18f, -3f));
        yield return new WaitForSeconds(3.0f);
        Assert.AreEqual("Level1", SceneManager.GetActiveScene().name);
        Assert.AreEqual(3, Hp(), "hp restored after restart");
        Assert.Less(Player().transform.position.x, 5f, "hero back at start");

        yield return CollectAllAndExit("Level2");
    }

    [UnityTest]
    public IEnumerator Level2_Completable()
    {
        yield return Load("Level2");
        yield return Shot("L2_start");
        Keys(Key.D);
        yield return new WaitForSeconds(1.0f);
        Keys();
        yield return CollectAllAndExit("Level3");
    }

    [UnityTest]
    public IEnumerator Level3_BossFight()
    {
        yield return Load("Level3");
        yield return Shot("L3_start");

        bool sawFireball = false, sawWarn = false;
        float t = 0;
        while (t < 12f)
        {
            if (GameObject.Find("Fireball(Clone)") != null) sawFireball = true;
            if (GameObject.Find("WarnColumn(Clone)") != null)
            {
                if (!sawWarn) yield return Shot("L3_warn");
                sawWarn = true;
            }
            if (sawFireball && t > 4f && !System.IO.File.Exists(Path.Combine(shots, "L3_fireballs.png")))
                yield return Shot("L3_fireballs");
            t += Time.deltaTime;
            yield return null;
        }
        Assert.IsTrue(sawFireball, "boss should shoot");
        Assert.IsTrue(sawWarn, "boss should show a rain warning");

        // kill the boss through the same Hit() the bolts use
        var boss = GameObject.Find("Boss");
        Assert.NotNull(boss);
        var bossComp = boss.GetComponent("Boss");
        var hit = bossComp.GetType().GetMethod("Hit");
        for (int i = 0; i < 16; i++) hit.Invoke(bossComp, new object[] { 1 });
        yield return Shot("L3_phase2");
        for (int i = 0; i < 14; i++) hit.Invoke(bossComp, new object[] { 1 });
        yield return new WaitForSeconds(9f); // boss fade + treasure chest + fade out
        Assert.AreEqual("Win", SceneManager.GetActiveScene().name);
    }
}
