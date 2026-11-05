using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Temple Raider: builds the whole game from the art in Assets/Art: animations, prefabs, input actions and all five scenes.
// Run from the menu "Midterm / Build Everything" or in batch mode (-executeMethod ProjectBuilder.BuildAll).
public static class ProjectBuilder
{
    static Material unlit;
    static PhysicsMaterial2D noFriction;
    static InputActionAsset actions;
    static Font font;

    static GameObject playerPrefab, snakePrefab, bossPrefab, boltPrefab, fireballPrefab,
                      warnPrefab, crystalPrefab, heartPrefab, portalPrefab;
    static RuntimeAnimatorController heroIdleLoop, snakeWalkLoop, torchCtrl;

    static int LGround, LPlayer, LEnemy;
    static int platformCounter;

    static readonly Color Gold = new Color(1f, 0.85f, 0.4f);

    // ------------------------------------------------------------------ entry
    [MenuItem("Midterm/Build Everything")]
    public static void BuildAll()
    {
        try
        {
            Run();
        }
        catch (System.Exception e)
        {
            Debug.LogError("BUILD FAILED: " + e);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }
        Debug.Log("BUILD OK");
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }

    static void Run()
    {
        EnsureLayers();
        LGround = LayerMask.NameToLayer("Ground");
        LPlayer = LayerMask.NameToLayer("Player");
        LEnemy = LayerMask.NameToLayer("Enemy");

        foreach (var folder in new[] { "Assets/Animations", "Assets/Prefabs", "Assets/Scenes" })
            AssetDatabase.DeleteAsset(folder);
        AssetDatabase.Refresh();
        AssetDatabase.CreateFolder("Assets", "Animations");
        AssetDatabase.CreateFolder("Assets", "Prefabs");
        AssetDatabase.CreateFolder("Assets", "Scenes");

        // make sure every picture goes through ArtImporter
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" }))
            AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);

        unlit = AssetDatabase.LoadAssetAtPath<Material>(
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
        if (unlit == null) unlit = new Material(Shader.Find("Sprites/Default"));
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        noFriction = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
        AssetDatabase.CreateAsset(noFriction, "Assets/Prefabs/NoFriction.physicsMaterial2D");

        actions = CreateInputActions();
        BuildAnimationsAndPrefabs();
        BuildScenes();
        ConfigureSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void EnsureLayers()
    {
        var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tm.FindProperty("layers");
        layers.GetArrayElementAtIndex(8).stringValue = "Ground";
        layers.GetArrayElementAtIndex(9).stringValue = "Player";
        layers.GetArrayElementAtIndex(10).stringValue = "Enemy";
        tm.ApplyModifiedProperties();
    }

    // ------------------------------------------------------------------ input actions
    static string Bind(string name, string path, string action, bool composite = false, bool part = false)
    {
        return "{\"name\":\"" + name + "\",\"id\":\"" + System.Guid.NewGuid() + "\",\"path\":\"" + path +
               "\",\"interactions\":\"\",\"processors\":\"\",\"groups\":\"\",\"action\":\"" + action +
               "\",\"isComposite\":" + (composite ? "true" : "false") +
               ",\"isPartOfComposite\":" + (part ? "true" : "false") + "}";
    }

    static string ActionJson(string name, string type, string control)
    {
        return "{\"name\":\"" + name + "\",\"type\":\"" + type + "\",\"id\":\"" + System.Guid.NewGuid() +
               "\",\"expectedControlType\":\"" + control + "\",\"processors\":\"\",\"interactions\":\"\",\"initialStateCheck\":" +
               (type == "Value" ? "true" : "false") + "}";
    }

    static InputActionAsset CreateInputActions()
    {
        var b = new List<string>();
        b.Add(Bind("WASD", "2DVector(mode=2)", "Move", true));
        b.Add(Bind("up", "<Keyboard>/w", "Move", false, true));
        b.Add(Bind("down", "<Keyboard>/s", "Move", false, true));
        b.Add(Bind("left", "<Keyboard>/a", "Move", false, true));
        b.Add(Bind("right", "<Keyboard>/d", "Move", false, true));
        b.Add(Bind("Arrows", "2DVector(mode=2)", "Move", true));
        b.Add(Bind("up", "<Keyboard>/upArrow", "Move", false, true));
        b.Add(Bind("down", "<Keyboard>/downArrow", "Move", false, true));
        b.Add(Bind("left", "<Keyboard>/leftArrow", "Move", false, true));
        b.Add(Bind("right", "<Keyboard>/rightArrow", "Move", false, true));
        b.Add(Bind("", "<Gamepad>/leftStick", "Move"));
        b.Add(Bind("", "<Keyboard>/space", "Jump"));
        b.Add(Bind("", "<Keyboard>/w", "Jump"));
        b.Add(Bind("", "<Keyboard>/upArrow", "Jump"));
        b.Add(Bind("", "<Gamepad>/buttonSouth", "Jump"));
        b.Add(Bind("", "<Keyboard>/x", "Fire"));
        b.Add(Bind("", "<Keyboard>/j", "Fire"));
        b.Add(Bind("", "<Keyboard>/leftCtrl", "Fire"));
        b.Add(Bind("", "<Gamepad>/buttonWest", "Fire"));

        var json = new StringBuilder();
        json.Append("{\"name\":\"GameControls\",\"maps\":[{\"name\":\"Player\",\"id\":\"" + System.Guid.NewGuid() + "\",\"actions\":[");
        json.Append(ActionJson("Move", "Value", "Vector2") + ",");
        json.Append(ActionJson("Jump", "Button", "Button") + ",");
        json.Append(ActionJson("Fire", "Button", "Button"));
        json.Append("],\"bindings\":[" + string.Join(",", b) + "]}],\"controlSchemes\":[]}");

        string path = "Assets/Settings/GameControls.inputactions";
        File.WriteAllText(path, json.ToString());
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        if (asset == null) throw new System.Exception("Input actions asset failed to import");
        return asset;
    }

    // ------------------------------------------------------------------ helpers
    static Sprite Spr(string rel)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/" + rel);
        if (s == null) throw new System.Exception("Missing sprite: " + rel);
        return s;
    }

    static Sprite[] Frames(string folder, string prefix, int count)
    {
        var list = new Sprite[count];
        for (int i = 0; i < count; i++) list[i] = Spr(folder + "/" + prefix + "_" + i + ".png");
        return list;
    }

    static SpriteRenderer SR(GameObject go, Sprite s, int order)
    {
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = s;
        r.sortingOrder = order;
        r.sharedMaterial = unlit;
        return r;
    }

    static SpriteRenderer Tiled(GameObject go, Sprite s, Vector2 size, int order)
    {
        var r = SR(go, s, order);
        r.drawMode = SpriteDrawMode.Tiled;
        r.tileMode = SpriteTileMode.Continuous;
        r.size = size;
        return r;
    }

    static GameObject SavePrefab(GameObject go, string name)
    {
        var asset = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/" + name + ".prefab");
        Object.DestroyImmediate(go);
        return asset;
    }

    static AnimationClip MakeClip(string name, Sprite[] sprites, float fps, bool loop)
    {
        var clip = new AnimationClip { frameRate = fps };
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, "Assets/Animations/" + name + ".anim");
        return clip;
    }

    // Moving platform animation: the Mover child slides by `delta` and back, easing at both ends.
    static AnimationClip MakeMoveClip(string name, Vector2 delta, float period)
    {
        var clip = new AnimationClip { frameRate = 60 };
        var cx = new AnimationCurve(new Keyframe(0, 0, 0, 0), new Keyframe(period / 2f, delta.x, 0, 0), new Keyframe(period, 0, 0, 0));
        var cy = new AnimationCurve(new Keyframe(0, 0, 0, 0), new Keyframe(period / 2f, delta.y, 0, 0), new Keyframe(period, 0, 0, 0));
        clip.SetCurve("", typeof(Transform), "localPosition.x", cx);
        clip.SetCurve("", typeof(Transform), "localPosition.y", cy);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, "Assets/Animations/" + name + ".anim");
        return clip;
    }

    static AnimatorController LoopController(string name, AnimationClip clip)
    {
        var c = AnimatorController.CreateAnimatorControllerAtPath("Assets/Animations/" + name + ".controller");
        var sm = c.layers[0].stateMachine;
        var st = sm.AddState("Loop");
        st.motion = clip;
        sm.defaultState = st;
        return c;
    }

    static void Transition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float threshold, string param)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration = 0f;
        t.AddCondition(mode, threshold, param);
    }

    // ------------------------------------------------------------------ animations + prefabs
    static void BuildAnimationsAndPrefabs()
    {
        // ---- hero controller: Idle / Walk / Jump ----
        var idle = MakeClip("Hero_Idle", Frames("Sprites", "hero_idle", 5), 6, true);
        var walk = MakeClip("Hero_Walk", Frames("Sprites", "hero_walk", 5), 12, true);
        var jump = MakeClip("Hero_Jump", Frames("Sprites", "hero_jump", 3), 6, false);
        var hero = AnimatorController.CreateAnimatorControllerAtPath("Assets/Animations/Hero.controller");
        hero.AddParameter("Speed", AnimatorControllerParameterType.Float);
        hero.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        var hsm = hero.layers[0].stateMachine;
        var sIdle = hsm.AddState("Idle"); sIdle.motion = idle;
        var sWalk = hsm.AddState("Walk"); sWalk.motion = walk;
        var sJump = hsm.AddState("Jump"); sJump.motion = jump;
        hsm.defaultState = sIdle;
        Transition(sIdle, sWalk, AnimatorConditionMode.Greater, 0.1f, "Speed");
        Transition(sWalk, sIdle, AnimatorConditionMode.Less, 0.1f, "Speed");
        Transition(sIdle, sJump, AnimatorConditionMode.IfNot, 0, "Grounded");
        Transition(sWalk, sJump, AnimatorConditionMode.IfNot, 0, "Grounded");
        Transition(sJump, sIdle, AnimatorConditionMode.If, 0, "Grounded");
        heroIdleLoop = LoopController("HeroIdleLoop", idle);

        // ---- snake: Walk, Death on trigger ----
        var sWalkClip = MakeClip("Snake_Walk", Frames("Sprites", "snake_walk", 4), 8, true);
        var sDeathClip = MakeClip("Snake_Death", Frames("Sprites", "snake_death", 4), 8, false);
        var sc = AnimatorController.CreateAnimatorControllerAtPath("Assets/Animations/Snake.controller");
        sc.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        var ssm = sc.layers[0].stateMachine;
        var scWalk = ssm.AddState("Walk"); scWalk.motion = sWalkClip;
        var scDeath = ssm.AddState("Death"); scDeath.motion = sDeathClip;
        ssm.defaultState = scWalk;
        var anyDie = ssm.AddAnyStateTransition(scDeath);
        anyDie.hasExitTime = false;
        anyDie.duration = 0f;
        anyDie.canTransitionToSelf = false;
        anyDie.AddCondition(AnimatorConditionMode.If, 0, "Die");
        snakeWalkLoop = LoopController("SnakeWalkLoop", sWalkClip);
        torchCtrl = LoopController("Torch", MakeClip("Torch_Flame", Frames("World", "torch", 4), 8, true));

        // ---- pickups / portal ----
        var crystalCtrl = LoopController("Crystal", MakeClip("Crystal_Spin", Frames("World", "crystal", 4), 6, true));
        var portalCtrl = LoopController("Portal", MakeClip("Portal_Swirl", Frames("World", "portal", 4), 8, true));

        // ---- bolt (player projectile) ----
        var go = new GameObject("Bolt"); // one shotgun pellet
        SR(go, Spr("World/bolt.png"), 11);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var cc = go.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 0.2f;
        var bolt = go.AddComponent<PlayerBolt>();
        bolt.speed = 17f;
        bolt.lifetime = 0.5f;
        boltPrefab = SavePrefab(go, "Bolt");

        // ---- fireball (boss projectile) ----
        go = new GameObject("Fireball");
        SR(go, Spr("World/fireball.png"), 11);
        go.transform.localScale = Vector3.one * 1.2f;
        rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        cc = go.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 0.35f;
        var fb = go.AddComponent<EnemyProjectile>();
        fb.speed = 7f;
        fb.lifetime = 6f;
        fireballPrefab = SavePrefab(go, "Fireball");

        // ---- warning column + "!" ----
        go = new GameObject("WarnColumn");
        var col = new GameObject("Column");
        col.transform.SetParent(go.transform, false);
        col.transform.localPosition = new Vector3(0, 7f, 0);
        SR(col, Spr("World/white.png"), 3).color = new Color(1f, 0.15f, 0.1f, 0.3f);
        col.transform.localScale = new Vector3(1.2f / 0.25f, 14f / 0.25f, 1f); // white.png is 4px = 0.25 units
        var mark = new GameObject("Mark");
        mark.transform.SetParent(go.transform, false);
        mark.transform.localPosition = new Vector3(0, 4f, 0);
        mark.transform.localScale = Vector3.one * 2f;
        SR(mark, Spr("World/warn.png"), 4);
        warnPrefab = SavePrefab(go, "WarnColumn");

        // ---- crystal ----
        go = new GameObject("Crystal");
        SR(go, Spr("World/crystal_0.png"), 5);
        go.AddComponent<Animator>().runtimeAnimatorController = crystalCtrl;
        cc = go.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 0.45f;
        go.AddComponent<Pickup>().kind = Pickup.Kind.Crystal;
        crystalPrefab = SavePrefab(go, "Crystal");

        // ---- heart pickup ----
        go = new GameObject("HeartPickup");
        SR(go, Spr("World/heart_full.png"), 5);
        go.transform.localScale = Vector3.one * 1.1f;
        cc = go.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 0.5f;
        go.AddComponent<Pickup>().kind = Pickup.Kind.Heart;
        heartPrefab = SavePrefab(go, "HeartPickup");

        // ---- portal ----
        go = new GameObject("Portal");
        SR(go, Spr("World/portal_0.png"), 4);
        go.transform.localScale = Vector3.one * 1.3f;
        go.AddComponent<Animator>().runtimeAnimatorController = portalCtrl;
        var bc = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(1.0f, 1.8f);
        bc.offset = new Vector2(0, 1f);
        go.AddComponent<Portal>().lockedSprite = Spr("World/portal_locked.png");
        portalPrefab = SavePrefab(go, "Portal");

        // ---- snake ----
        go = new GameObject("Snake");
        go.layer = LEnemy;
        SR(go, Spr("Sprites/snake_walk_0.png"), 8);
        go.AddComponent<Animator>().runtimeAnimatorController = sc;
        rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        bc = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(1.8f, 1.0f);
        bc.offset = new Vector2(0, 0.55f);
        var h = go.AddComponent<Health>();
        h.maxHP = 2;
        h.hitFlash = 0.1f;
        var pe = go.AddComponent<PatrolEnemy>();
        pe.groundMask = 1 << LGround;
        snakePrefab = SavePrefab(go, "Snake");

        // ---- boss ----
        go = new GameObject("Boss");
        go.layer = LEnemy;
        SR(go, Spr("Sprites/boss.png"), 8);
        rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        bc = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(3.0f, 5.4f);
        bc.offset = new Vector2(0.1f, 2.9f);
        h = go.AddComponent<Health>();
        h.maxHP = 30; // the shotgun fires 5 pellets per shot
        h.hitFlash = 0.1f;
        var boss = go.AddComponent<Boss>();
        boss.contactDamage = 1;
        boss.rainMaxX = 13f;
        boss.fireballPrefab = fireballPrefab.GetComponent<EnemyProjectile>();
        boss.warnPrefab = warnPrefab;
        var muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(go.transform, false);
        muzzle.transform.localPosition = new Vector3(-2.4f, 3.1f, 0);
        boss.muzzle = muzzle.transform;
        bossPrefab = SavePrefab(go, "Boss");

        // ---- player ----
        go = new GameObject("Player");
        go.layer = LPlayer;
        SR(go, Spr("Sprites/hero_idle_0.png"), 10);
        go.AddComponent<Animator>().runtimeAnimatorController = hero;
        rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3.5f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        var cap = go.AddComponent<CapsuleCollider2D>();
        cap.direction = CapsuleDirection2D.Vertical;
        cap.size = new Vector2(0.8f, 1.4f);
        cap.offset = new Vector2(0, 0.7f);
        cap.sharedMaterial = noFriction;
        h = go.AddComponent<Health>();
        h.maxHP = 3;
        h.invincibleTime = 1.2f;
        var pc = go.AddComponent<PlayerController>();
        pc.groundMask = 1 << LGround;
        pc.boltPrefab = boltPrefab.GetComponent<PlayerBolt>();
        var pi = go.AddComponent<PlayerInput>();
        pi.actions = actions;
        pi.defaultActionMap = "Player";
        pi.notificationBehavior = PlayerNotifications.SendMessages;
        playerPrefab = SavePrefab(go, "Player");
    }

    // ------------------------------------------------------------------ level building blocks
    class LB
    {
        public Transform root;

        GameObject Make(string name, Vector2 pos)
        {
            var g = new GameObject(name);
            g.transform.SetParent(root);
            g.transform.position = pos;
            return g;
        }

        GameObject Place(GameObject prefab, Vector2 pos)
        {
            var g = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            g.transform.SetParent(root);
            g.transform.position = pos;
            return g;
        }

        // Solid block of earth: top surface at topY, mossy top row + stone below.
        public void Block(float x0, float x1, float topY, float depth)
        {
            float w = x1 - x0;
            var g = Make("Ground", new Vector2((x0 + x1) / 2f, topY - depth / 2f));
            g.layer = LGround;
            var bc = g.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(w, depth);
            bc.sharedMaterial = noFriction;

            var top = new GameObject("Top");
            top.transform.SetParent(g.transform, false);
            top.transform.localPosition = new Vector3(0, depth / 2f - 0.5f, 0);
            Tiled(top, Spr("World/tile_top.png"), new Vector2(w, 1), 0);

            if (depth > 1)
            {
                var fill = new GameObject("Fill");
                fill.transform.SetParent(g.transform, false);
                fill.transform.localPosition = new Vector3(0, -0.5f, 0);
                Tiled(fill, Spr("World/tile_fill.png"), new Vector2(w, depth - 1), 0);
            }
        }

        public void Ground(float x0, float x1) { Block(x0, x1, 0f, 6f); }

        // One-tile-thick floating platform, topY is the walking surface.
        public void Plat(float x, float topY, float w)
        {
            var g = Make("Platform", new Vector2(x + w / 2f, topY - 0.5f));
            g.layer = LGround;
            var bc = g.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(w, 1f);
            bc.sharedMaterial = noFriction;
            var vis = new GameObject("Visual");
            vis.transform.SetParent(g.transform, false);
            Tiled(vis, Spr("World/tile_float.png"), new Vector2(w, 1f), 1);
        }

        // Platform moved by an Animation Clip (Mover child is animated, root stays put).
        public void Moving(float x, float topY, float w, float dx, float dy, float period)
        {
            platformCounter++;
            var rootGo = Make("MovingPlatform", new Vector2(x + w / 2f, topY - 0.25f));
            var mover = new GameObject("Mover");
            mover.transform.SetParent(rootGo.transform, false);
            mover.layer = LGround;
            Tiled(mover, Spr("World/tile_move.png"), new Vector2(w, 0.5f), 2);
            var bc = mover.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(w, 0.5f);
            bc.sharedMaterial = noFriction;
            var rb = mover.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            mover.AddComponent<MovingPlatform>();

            var clip = MakeMoveClip("MovePlat_" + platformCounter, new Vector2(dx, dy), period);
            var anim = mover.AddComponent<Animator>();
            anim.runtimeAnimatorController = LoopController("MovePlat_" + platformCounter, clip);
            anim.updateMode = AnimatorUpdateMode.Fixed;
        }

        public void Spikes(float x, float topY, float w)
        {
            var g = Make("Spikes", new Vector2(x + w / 2f, topY + 0.5f));
            Tiled(g, Spr("World/spikes.png"), new Vector2(w, 1f), 3);
            var bc = g.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(w * 0.9f, 0.5f);
            bc.offset = new Vector2(0, -0.2f);
            g.AddComponent<Hazard>().damage = 1;
        }

        public void Crystal(float x, float y) { Place(crystalPrefab, new Vector2(x, y)); }
        public void Heart(float x, float y) { Place(heartPrefab, new Vector2(x, y)); }
        public void Portal(float x, float y) { Place(portalPrefab, new Vector2(x, y)); }

        public GameObject Snake(float x, float y, float range)
        {
            var g = Place(snakePrefab, new Vector2(x, y));
            g.GetComponent<PatrolEnemy>().range = range;
            return g;
        }

        public GameObject Boss(float x, float y)
        {
            var g = Place(bossPrefab, new Vector2(x, y));
            return g;
        }

        // Wall torch with a flickering flame and a warm glow (background decoration).
        public void Torch(float x, float y)
        {
            var g = Make("Torch", new Vector2(x, y));
            SR(g, Spr("World/torch_0.png"), -60);
            g.transform.localScale = Vector3.one * 1.3f;
            g.AddComponent<Animator>().runtimeAnimatorController = torchCtrl;
            var glow = new GameObject("Glow");
            glow.transform.SetParent(g.transform, false);
            glow.transform.localPosition = new Vector3(0, 0.45f, 0);
            glow.transform.localScale = Vector3.one * 5f;
            SR(glow, Spr("World/glow.png"), -61);
        }

        // The temple treasure: hidden until the guardian dies (see Treasure.cs).
        public void PlaceTreasure(float x, float y)
        {
            var g = Make("Treasure", new Vector2(x, y));
            g.transform.localScale = Vector3.one * 1.5f;
            SR(g, Spr("World/treasure_closed.png"), 7);
            var t = g.AddComponent<Treasure>();
            t.closedSprite = Spr("World/treasure_closed.png");
            t.openSprite = Spr("World/treasure_open.png");
            t.coinSprite = Spr("World/crystal_0.png");
        }

        // Invisible wall at the edge of the level.
        public void Wall(float x, float width = 1f)
        {
            var g = Make("Wall", new Vector2(x, 10f));
            g.layer = LGround;
            var bc = g.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(width, 40f);
            bc.sharedMaterial = noFriction;
        }

        public void KillZone(float x0, float x1, float y)
        {
            var g = Make("KillZone", new Vector2((x0 + x1) / 2f, y));
            var bc = g.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(x1 - x0 + 80f, 2f);
            g.AddComponent<Hazard>().instantKill = true;
        }
    }

    // ------------------------------------------------------------------ scenes
    static Camera MakeCamera(Vector3 pos, bool follow, float minX, float maxX, float minY, float maxY)
    {
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = pos;
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.10f, 0.07f, 0.13f);
        camGo.AddComponent<AudioListener>();
        if (follow)
        {
            var cf = camGo.AddComponent<CameraFollow>();
            cf.minX = minX; cf.maxX = maxX; cf.minY = minY; cf.maxY = maxY;
        }
        AddBackground(camGo.transform);
        return cam;
    }

    static void AddBackground(Transform cam)
    {
        var sky = new GameObject("Sky");
        sky.transform.SetParent(cam, false);
        sky.transform.localPosition = new Vector3(0, 0, 10);
        sky.transform.localScale = Vector3.one * 1.7f;
        SR(sky, Spr("World/bg_sky.png"), -100);

        AddLayer("FarHills", "World/bg_far.png", 0.12f, 1.5f, -0.5f, -90);
        AddLayer("NearHills", "World/bg_near.png", 0.35f, 1.3f, -1.2f, -80);
    }

    static void AddLayer(string name, string sprite, float factor, float scale, float bottomOffset, int order)
    {
        var g = new GameObject(name);
        var s = Spr(sprite);
        var r = Tiled(g, s, new Vector2(320f, s.rect.height / s.pixelsPerUnit), order);
        g.transform.localScale = new Vector3(scale, scale, 1f);
        var p = g.AddComponent<Parallax>();
        p.factor = factor;
        p.bottomOffset = bottomOffset;
    }

    static void BuildLevel(string sceneName, int needed, string nextScene, string hint, string music,
                           float minX, float maxX, float minCamY, float maxCamY, Vector2 start,
                           System.Action<LB> design, bool boss)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        MakeCamera(new Vector3((minX + maxX) / 2f, 3f, -10f), true, minX, maxX, minCamY, maxCamY);

        var lb = new LB { root = new GameObject("Level").transform };
        design(lb);
        for (float tx = minX + 6f; tx < maxX - 3f; tx += 14f) lb.Torch(tx, 3.3f);
        lb.KillZone(minX, maxX, -7f);

        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.position = start;

        var gmGo = new GameObject("GameManager");
        var gm = gmGo.AddComponent<GameManager>();
        gm.crystalsNeeded = needed;
        gm.nextScene = nextScene;
        gm.musicName = music;
        gm.startHint = hint;

        BuildHud(true);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/" + sceneName + ".unity");
    }

    static void BuildScenes()
    {
        // ---------------- LEVEL 1 : temple entrance ----------------
        BuildLevel("Level1", 6, "Level2",
            "Move: A / D or stick   Jump: W / Space   Fire shotgun: X   -   collect 6 gold coins!",
            "music_game", 0f, 80f, 1.0f, 5f, new Vector2(2f, 0.1f), lb =>
        {
            lb.Wall(-0.5f); lb.Wall(80.5f);
            lb.Ground(0, 16);
            lb.Crystal(6, 1.0f); lb.Crystal(8, 1.0f);
            lb.Plat(11, 2.6f, 4); lb.Crystal(12.5f, 3.7f);

            lb.Ground(20, 36);
            lb.Spikes(26, 0f, 2); lb.Crystal(27, 2.8f);
            lb.Snake(31, 0f, 3f);

            lb.Ground(40, 60);
            lb.Plat(43, 2.6f, 3); lb.Heart(44.5f, 3.7f);
            lb.Plat(48, 4.8f, 3); lb.Crystal(49.5f, 5.8f);
            lb.Snake(54, 0f, 2f);
            lb.Spikes(46, 0f, 2); lb.Crystal(47, 2.8f);

            lb.Ground(64, 80);
            lb.Crystal(68, 1.0f); lb.Crystal(70, 1.0f);
            lb.Portal(76, 0f);
        }, false);

        // ---------------- LEVEL 2 : deep in the temple ----------------
        BuildLevel("Level2", 9, "Level3",
            "Level 2: the deep temple. Snakes, pits and moving stones. Collect 9 gold coins!",
            "music_game", 0f, 112f, 1.0f, 5.5f, new Vector2(2f, 0.1f), lb =>
        {
            lb.Wall(-0.5f); lb.Wall(112.5f);
            lb.Ground(0, 12);
            lb.Crystal(6, 1.0f);
            lb.Moving(12.5f, 0f, 3, 3f, 0f, 4f);

            lb.Ground(18, 30);
            lb.Snake(21, 0f, 1.5f);
            lb.Spikes(23, 0f, 2); lb.Crystal(24, 2.8f);

            lb.Plat(31, 1.2f, 2); lb.Crystal(32, 2.3f);
            lb.Plat(34.5f, 2.4f, 2); lb.Crystal(35.5f, 3.5f);

            lb.Ground(38, 56);
            lb.Snake(42, 0f, 2.5f); lb.Snake(50, 0f, 2f);
            lb.Spikes(46, 0f, 2); lb.Crystal(47, 2.8f);
            lb.Heart(53, 1.0f);

            lb.Moving(57.5f, 0f, 3, 0f, 3f, 4f);
            lb.Block(61, 70, 3f, 9f);
            lb.Crystal(65, 4.0f);
            lb.Snake(66, 3f, 2f);

            lb.Ground(72, 90);
            lb.Crystal(75, 1.0f); lb.Crystal(78, 2.8f);
            lb.Plat(80, 2.6f, 3); lb.Crystal(81.5f, 3.8f);
            lb.Snake(85, 0f, 2.5f);
            lb.Spikes(77, 0f, 2);

            lb.Moving(90.5f, 0f, 3, 3f, 0f, 4f);
            lb.Ground(96, 112);
            lb.Crystal(100, 1.0f); lb.Crystal(103, 1.0f);
            lb.Heart(98, 1.0f);
            lb.Snake(106, 0f, 2.5f);
            lb.Portal(110, 0f);
        }, false);

        // ---------------- LEVEL 3 : guardian's treasure chamber ----------------
        BuildLevel("Level3", 0, "Win",
            "The Temple Guardian protects the treasure! Blast him with X  -  dodge the orbs and the red columns!",
            "music_boss", 0f, 21f, 4.4f, 4.4f, new Vector2(3f, 0.1f), lb =>
        {
            lb.Ground(0, 21);
            lb.Block(-3, 0, 12f, 13f);
            lb.Block(21, 24, 12f, 13f);
            lb.Plat(2, 2.6f, 4);
            lb.Plat(8, 2.6f, 4);
            lb.Plat(5, 4.9f, 3);
            lb.Heart(9.5f, 3.7f);
            lb.PlaceTreasure(16.8f, 0f);
            lb.Boss(16.8f, 0f);
        }, true);

        BuildMenuScene();
        BuildWinScene();
    }

    // ------------------------------------------------------------------ UI helpers
    static RectTransform UIRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static Image UIImage(string name, Transform parent, Sprite s, Color c, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var rt = UIRect(name, parent);
        Place(rt, anchor, pos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = s;
        img.color = c;
        img.preserveAspect = true;
        return img;
    }

    static Text UIText(string name, Transform parent, string text, int size, Color color,
                       Vector2 anchor, Vector2 pos, Vector2 boxSize, TextAnchor align = TextAnchor.MiddleCenter)
    {
        var rt = UIRect(name, parent);
        Place(rt, anchor, pos, boxSize);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.fontStyle = FontStyle.Bold;
        t.alignment = align;
        t.color = color;
        t.text = text;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var o = rt.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0.05f, 0.03f, 0.12f, 1f);
        o.effectDistance = new Vector2(4, -4);
        return t;
    }

    static Canvas MakeCanvas(string name)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    static void MakeEventSystem()
    {
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    // Bronze temple button (sliced pixel-art sprite). Text is gold with a dark outline.
    static Button StoneButton(Transform parent, string label, Vector2 pos, Vector2 size, int fontSize = 64)
    {
        var img = UIImage("Button_" + label, parent, Spr("UI/button.png"), Color.white,
                          new Vector2(0.5f, 0.5f), pos, size);
        img.type = Image.Type.Sliced;
        img.preserveAspect = false;
        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var colors = b.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.1f, 1f);
        colors.pressedColor = new Color(0.7f, 0.65f, 0.6f, 1f);
        b.colors = colors;
        UIText("Label", img.transform, label, fontSize, Gold, new Vector2(0.5f, 0.5f), Vector2.zero, size);
        return b;
    }

    static Button MakeButton(Transform parent, string label, Vector2 pos, Vector2 size, UIButton.Action action, string scene)
    {
        var b = StoneButton(parent, label, pos, size);
        var ub = b.gameObject.AddComponent<UIButton>();
        ub.action = action;
        ub.sceneName = scene;
        return b;
    }

    // ------------------------------------------------------------------ HUD + touch controls
    static void BuildHud(bool withBossBar)
    {
        MakeEventSystem();
        var canvas = MakeCanvas("HUD");
        var hud = canvas.gameObject.AddComponent<Hud>();
        var TL = new Vector2(0, 1);
        var TC = new Vector2(0.5f, 1);

        // hearts
        var heartFull = Spr("World/heart_full.png");
        var heartEmpty = Spr("World/heart_empty.png");
        hud.heartFull = heartFull;
        hud.heartEmpty = heartEmpty;
        hud.hearts = new Image[3];
        for (int i = 0; i < 3; i++)
            hud.hearts[i] = UIImage("Heart" + i, canvas.transform, heartFull, Color.white, TL,
                                    new Vector2(30 + i * 90, -30), new Vector2(80, 80));

        // crystal counter
        var group = UIRect("CrystalGroup", canvas.transform);
        Place(group, TL, new Vector2(30, -125), new Vector2(300, 80));
        hud.crystalGroup = group.gameObject;
        UIImage("Icon", group, Spr("World/crystal_0.png"), Color.white, TL, new Vector2(10, 0), new Vector2(60, 80));
        hud.crystalText = UIText("Count", group, "0 / 0", 60, Color.white, TL, new Vector2(85, -5), new Vector2(240, 70), TextAnchor.MiddleLeft);

        // center message
        hud.messageText = UIText("Message", canvas.transform, "", 52, Gold, TC, new Vector2(0, -230), new Vector2(1700, 120));

        // boss bar
        var bossPanel = UIRect("BossPanel", canvas.transform);
        Place(bossPanel, TC, new Vector2(0, -30), new Vector2(760, 80));
        hud.bossPanel = bossPanel.gameObject;
        var white = Spr("World/white.png");
        var back = UIImage("Back", bossPanel, white, new Color(0.08f, 0.04f, 0.12f, 0.85f), TC, new Vector2(0, -34), new Vector2(760, 44));
        back.preserveAspect = false;
        var fill = UIImage("Fill", bossPanel, white, new Color(0.85f, 0.15f, 0.2f, 1f), TC, new Vector2(0, -34), new Vector2(744, 32));
        fill.preserveAspect = false;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 1f;
        hud.bossFill = fill;
        UIText("Name", bossPanel, "TEMPLE GUARDIAN", 34, Color.white, TC, new Vector2(0, -2), new Vector2(600, 40));
        bossPanel.gameObject.SetActive(false);

        // joystick (bottom-left)
        var BL = new Vector2(0, 0);
        var BR = new Vector2(1, 0);
        var stickBg = UIImage("JoystickBack", canvas.transform, Spr("UI/joy_bg.png"), Color.white, BL, new Vector2(60, 60), new Vector2(400, 400));
        var knob = UIImage("JoystickKnob", stickBg.transform, Spr("UI/joy_knob.png"), Color.white,
                           new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220, 220));
        var stick = knob.gameObject.AddComponent<OnScreenStick>();
        stick.movementRange = 110f;
        stick.controlPath = "<Gamepad>/leftStick";

        // buttons (bottom-right)
        var jump = UIImage("JumpButton", canvas.transform, Spr("UI/btn_jump.png"), Color.white, BR, new Vector2(-70, 130), new Vector2(240, 240));
        jump.gameObject.AddComponent<OnScreenButton>().controlPath = "<Gamepad>/buttonSouth";
        var fire = UIImage("FireButton", canvas.transform, Spr("UI/btn_fire.png"), Color.white, BR, new Vector2(-340, 70), new Vector2(200, 200));
        fire.gameObject.AddComponent<OnScreenButton>().controlPath = "<Gamepad>/buttonWest";

        BuildPause(canvas);
    }

    // Stone "pause" button (top-right) + the pause window with RESUME / RESTART / MENU.
    // Must be built last so the window is drawn above every other HUD element and blocks the touch controls.
    static void BuildPause(Canvas canvas)
    {
        var pm = canvas.gameObject.AddComponent<PauseMenu>();
        var C = new Vector2(0.5f, 0.5f);

        var pauseImg = UIImage("PauseButton", canvas.transform, Spr("UI/btn_pause.png"), Color.white,
                               new Vector2(1, 1), new Vector2(-30, -30), new Vector2(110, 110));
        var pauseBtn = pauseImg.gameObject.AddComponent<Button>();
        pauseBtn.targetGraphic = pauseImg;
        var pc = pauseBtn.colors;
        pc.highlightedColor = new Color(1.2f, 1.2f, 1.1f, 1f);
        pc.pressedColor = new Color(0.7f, 0.65f, 0.6f, 1f);
        pauseBtn.colors = pc;
        UnityEventTools.AddVoidPersistentListener(pauseBtn.onClick, new UnityAction(pm.Pause));

        // full-screen dark overlay (also swallows touches meant for the game controls)
        var overlayRt = UIRect("PausePanel", canvas.transform);
        overlayRt.anchorMin = Vector2.zero; overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero; overlayRt.offsetMax = Vector2.zero;
        var overlay = overlayRt.gameObject.AddComponent<Image>();
        overlay.sprite = Spr("World/white.png");
        overlay.color = new Color(0.05f, 0.02f, 0.08f, 0.78f);

        // stone window
        var window = UIImage("Window", overlayRt, Spr("UI/panel.png"), Color.white, C, new Vector2(0, 0), new Vector2(820, 880));
        window.type = Image.Type.Sliced;
        window.preserveAspect = false;
        UIText("Title", window.transform, "PAUSED", 110, Gold, C, new Vector2(0, 300), new Vector2(760, 140));
        UIText("Sub", window.transform, "The temple waits...", 40, new Color(0.85f, 0.78f, 0.65f), C, new Vector2(0, 200), new Vector2(760, 60));

        var resume = StoneButton(window.transform, "RESUME", new Vector2(0, 80), new Vector2(560, 130));
        var restart = StoneButton(window.transform, "RESTART", new Vector2(0, -80), new Vector2(560, 130));
        var menu = StoneButton(window.transform, "MENU", new Vector2(0, -240), new Vector2(560, 130));
        UnityEventTools.AddVoidPersistentListener(resume.onClick, new UnityAction(pm.Resume));
        UnityEventTools.AddVoidPersistentListener(restart.onClick, new UnityAction(pm.Restart));
        UnityEventTools.AddVoidPersistentListener(menu.onClick, new UnityAction(pm.LoadMenu));

        var so = new SerializedObject(pm);
        so.FindProperty("pausePanel").objectReferenceValue = overlayRt.gameObject;
        so.FindProperty("popup").objectReferenceValue = window.rectTransform;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ------------------------------------------------------------------ menu + win
    static GameObject Decor(string name, Sprite s, Vector2 pos, float scale, int order, RuntimeAnimatorController ctrl)
    {
        var g = new GameObject(name);
        g.transform.position = pos;
        g.transform.localScale = Vector3.one * scale;
        SR(g, s, order);
        if (ctrl != null) g.AddComponent<Animator>().runtimeAnimatorController = ctrl;
        return g;
    }

    static void BuildMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        MakeCamera(new Vector3(0, 0, -10), false, 0, 0, 0, 0);
        var lb = new LB { root = new GameObject("Scenery").transform };
        lb.Block(-16, 16, -3.6f, 3f);

        var boss = Decor("BossShadow", Spr("Sprites/boss.png"), new Vector2(8f, -3.6f), 1.5f, 1, null);
        boss.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.5f);
        Decor("Hero", Spr("Sprites/hero_idle_0.png"), new Vector2(-6.8f, -3.6f), 2.2f, 5, heroIdleLoop);
        var sc = Decor("Snake", Spr("Sprites/snake_walk_0.png"), new Vector2(-2.6f, -3.6f), 1.5f, 4, snakeWalkLoop);
        sc.GetComponent<SpriteRenderer>().flipX = true;
        Decor("Crystal", Spr("World/crystal_0.png"), new Vector2(-8.5f, -1f), 1.8f, 3, null);
        Decor("Crystal2", Spr("World/crystal_2.png"), new Vector2(-3.8f, 0.2f), 1.4f, 3, null);

        new GameObject("Music").AddComponent<SceneMusic>().musicName = "music_menu";

        MakeEventSystem();
        var canvas = MakeCanvas("Menu");
        var C = new Vector2(0.5f, 0.5f);
        UIText("Title", canvas.transform, "TEMPLE RAIDER", 150, Gold, C, new Vector2(0, 330), new Vector2(1500, 200));
        UIText("Subtitle", canvas.transform, "THE LOST TREASURE", 60, Color.white, C, new Vector2(0, 190), new Vector2(1500, 90));
        MakeButton(canvas.transform, "PLAY", new Vector2(0, 20), new Vector2(520, 140), UIButton.Action.LoadScene, "Level1");
        MakeButton(canvas.transform, "QUIT", new Vector2(0, -150), new Vector2(520, 140), UIButton.Action.Quit, "");
        UIText("Hint", canvas.transform, "A / D move   W / Space jump   X fire shotgun   -   Esc pause   -   or use the touch controls",
               34, new Color(0.8f, 0.8f, 0.9f), new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(1600, 60));

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
    }

    static void BuildWinScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        MakeCamera(new Vector3(0, 0, -10), false, 0, 0, 0, 0);
        var lb = new LB { root = new GameObject("Scenery").transform };
        lb.Block(-16, 16, -3.6f, 3f);
        Decor("Hero", Spr("Sprites/hero_idle_0.png"), new Vector2(-6.5f, -3.6f), 2.4f, 5, heroIdleLoop);
        Decor("Treasure", Spr("World/treasure_open.png"), new Vector2(0f, -3.6f), 3.2f, 4, null);
        Decor("CrystalL", Spr("World/crystal_1.png"), new Vector2(-4f, -2.2f), 2f, 3, null);
        Decor("CrystalR", Spr("World/crystal_3.png"), new Vector2(4f, -2.2f), 2f, 3, null);
        Decor("CrystalL2", Spr("World/crystal_2.png"), new Vector2(-7f, -2.6f), 1.5f, 3, null);
        Decor("CrystalR2", Spr("World/crystal_0.png"), new Vector2(7f, -2.6f), 1.5f, 3, null);

        new GameObject("Music").AddComponent<SceneMusic>().musicName = "music_menu";

        MakeEventSystem();
        var canvas = MakeCanvas("WinScreen");
        var C = new Vector2(0.5f, 0.5f);
        UIText("Title", canvas.transform, "TREASURE FOUND!", 150, Gold, C, new Vector2(0, 300), new Vector2(1500, 220));
        UIText("Sub", canvas.transform, "The Guardian is defeated. The temple treasure is yours!", 52, Color.white, C,
               new Vector2(0, 150), new Vector2(1700, 90));
        MakeButton(canvas.transform, "MAIN MENU", new Vector2(0, 10), new Vector2(620, 140), UIButton.Action.LoadScene, "MainMenu");

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Win.unity");
    }

    // ------------------------------------------------------------------ project settings
    static void ConfigureSettings()
    {
        PlayerSettings.companyName = "Midterm";
        PlayerSettings.productName = "Temple Raider";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.midterm.whiskers");
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.midterm.whiskers");

        var scenes = new List<EditorBuildSettingsScene>();
        foreach (var n in new[] { "MainMenu", "Level1", "Level2", "Level3", "Win" })
            scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/" + n + ".unity", true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
