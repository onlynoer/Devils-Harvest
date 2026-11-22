#if HARVEST_PLAYTEST
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Opt-in Windows acceptance replay. Normal launches leave the player's input and save selection alone.
public sealed partial class HarvestReview : MonoBehaviour
{
    private static string save;
    private static string routeMode;
    private static Keyboard kb;
    private static Mouse mouse;
    private static Player p;
    private static WeaponSystem gun;
    private static HarvestTrade trade;
    private static float distance;
    private static string Output => Path.Combine(Path.GetDirectoryName(Application.dataPath), "PlaytestEvidence");

    private const string ActiveKey = "DevilsHarvest.ActiveSaveId";
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Launch()
    {
        var args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-dh-review");
        if (index < 0 || index + 1 >= args.Length)
            return;
        routeMode = args[index + 1];
        var runner = new GameObject("Windows acceptance replay").AddComponent<HarvestReview>();
        DontDestroyOnLoad(runner.gameObject);
        runner.StartCoroutine(runner.Run(args[index + 1] == "mechanics"));
    }

    private IEnumerator Run(bool mechanics)
    {
        Application.runInBackground = true;
        Directory.CreateDirectory(Output);
        string previous = PlayerPrefs.GetString(ActiveKey, "");
        var stack = new Stack<IEnumerator>();
        stack.Push(Gameplay(mechanics));
        bool success = true;
        while (stack.Count > 0)
        {
            object current;
            try
            {
                if (!stack.Peek().MoveNext())
                {
                    stack.Pop();
                    continue;
                }

                current = stack.Peek().Current;
            }
            catch (Exception exception)
            {
                Debug.LogError("REVIEW FAILED " + exception);
                success = false;
                break;
            }

            if (current is IEnumerator nested)
                stack.Push(nested);
            else
                yield return current;
        }

        var created = SaveGameService.ActiveSave;
        if (created != null && created.saveName == save)
            SaveGameService.Delete(created.id);
        if (string.IsNullOrEmpty(previous))
            PlayerPrefs.DeleteKey(ActiveKey);
        else
            PlayerPrefs.SetString(ActiveKey, previous);
        PlayerPrefs.Save();
        Debug.Log("REVIEW complete=" + success);
        Application.Quit(success ? 0 : 1);
    }

    private static void Check(bool condition, string description)
    {
        if (!condition)
            throw new InvalidOperationException(description);
        Debug.Log("REVIEW PASS " + description);
    }

    private static Vector2 ParsePoint(string line)
    {
        var parts = line.Split(' ');
        return new Vector2(float.Parse(parts[0], CultureInfo.InvariantCulture), float.Parse(parts[1], CultureInfo.InvariantCulture));
    }

    private static bool Sight(Creature target, Vector3 eye)
    {
        var center = target.GetComponent<CharacterController>().bounds.center;
        return Physics.Raycast(eye, center - eye, out var hit, 75f) && hit.collider.GetComponentInParent<Creature>() == target;
    }

    private const string BeachTrail = "330.0 115.0,326.0 153.0,349.0 198.0,369.0 244.0,345.0 295.0,397.0 282.0,440.0 290.0,450.0 294.0,459.0 297.0,468.0 300.0,477.0 306.0,486.0 309.0,495.0 315.0,504.0 321.0,513.0 321.0,522.0 327.0,531.0 330.0,540.0 336.0,549.0 342.0,558.0 345.0,567.0 351.0,576.0 354.0,585.0 363.0,594.0 366.0,603.0 372.0,612.0 378.0,620 384,620 388,626 394,626 396,630 400,630 404,632 406,632 424,668 460,684 460,692 452,708 452,712 448,718 448,720 446,722 446,724 444,728 444,738 434,785.0 410.0,830.0 395.0,862.0 403.0,875.0 420.0,895.0 420.0,895.0 438.0,895.0 441.0";
    private const string SaddleTrail = "895 420,896 420,900 424,900 452,908 460,908 464,900 472,896 472,884 484,884 536,872 548,872 556,840 588,840 592,828 604,828 608,820 616,820 620,800 640,800 648,796 652,796 680,792 684,792 732,788 736,788 744";
    static IEnumerator Frames(int n)
    {
        while (n-- > 0)
            yield return null;
    }

    static void Keys(params Key[] k) => InputSystem.QueueStateEvent(kb, new KeyboardState(k));
    static IEnumerator Press(Key k)
    {
        Keys(k);
        yield return Frames(2);
        Keys();
        yield return Frames(2);
    }

    static IEnumerator Click(Vector2 pos)
    {
        var state = new MouseState
        {
            position = pos
        };
        InputSystem.QueueStateEvent(mouse, state);
        yield return Frames(2);
        InputSystem.QueueStateEvent(mouse, state.WithButton(MouseButton.Left));
        yield return Frames(2);
        InputSystem.QueueStateEvent(mouse, state.WithButton(MouseButton.Left, false));
        yield return Frames(2);
    }

    static IEnumerator ButtonClick(Button b)
    {
        Canvas.ForceUpdateCanvases();
        var r = (RectTransform)b.transform;
        var pos = RectTransformUtility.WorldToScreenPoint(null, r.TransformPoint(r.rect.center));
        var es = UnityEngine.EventSystems.EventSystem.current;
        var hits = new List<UnityEngine.EventSystems.RaycastResult>();
        es.RaycastAll(new UnityEngine.EventSystems.PointerEventData(es) { position = pos }, hits);
        var mod = es.currentInputModule as UnityEngine.InputSystem.UI.InputSystemUIInputModule;
        Debug.Log("DAQ button " + b.name + " " + pos + " hit " + string.Join(",", hits.Take(5).Select(h => h.gameObject.name)) + " module " + mod + " focus " + Application.isFocused);
        yield return Click(pos);
    }

    static Vector2 XZ(Vector3 v) => new Vector2(v.x, v.z);
    static void Face(Vector3 dest)
    {
        var dir = dest - gun.playerCamera.transform.position;
        var yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        float pitch = -Mathf.Atan2(dir.y, XZ(dir).magnitude) * Mathf.Rad2Deg;
        p.transform.rotation = Quaternion.Euler(0, yaw, 0);
        var look = gun.playerCamera.GetComponent<FirstPersonLook>();
        typeof(FirstPersonLook).GetField("xRotation", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(look, pitch);
        look.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
    }

    static IEnumerator Walk(Vector2 to, float tol = .55f)
    {
        var points = new Queue<Vector2>();
        points.Enqueue(to);
        int stuck = 0, replans = 0, frames = 0;
        Keys(Key.W);
        while (points.Count > 0 && frames++ < 2800)
        {
            var pos = XZ(p.transform.position);
            var goal = points.Peek();
            var delta = goal - pos;
            if (delta.magnitude <= tol)
            {
                points.Dequeue();
                continue;
            }

            p.transform.rotation = Quaternion.Euler(0, Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg, 0);
            yield return null;
            var step = Vector2.Distance(pos, XZ(p.transform.position));
            distance += step;
            stuck = step < .025f ? stuck + 1 : 0;
            if (p.IsDead)
                throw new Exception("died walking at " + p.transform.position);
            if (stuck < 14)
                continue;
            if (++replans > 4)
                throw new Exception("blocked at " + p.transform.position + " toward " + to);
            points = PathAround(XZ(p.transform.position), to);
            stuck = 0;
        }

        Keys();
        if (points.Count > 0)
            throw new Exception("timeout on foot " + to);
    }

    static Queue<Vector2> PathAround(Vector2 start, Vector2 end)
    {
        const float gap = 1.5f;
        var terrain = Terrain.activeTerrain;
        Vector3 At(Vector2 pt)
        {
            var w = new Vector3(pt.x, 0, pt.y);
            w.y = terrain.SampleHeight(w) + terrain.transform.position.y;
            return w;
        }

        var origin = Vector2Int.zero;
        var target = Vector2Int.RoundToInt((end - start) / gap);
        var open = new List<Vector2Int>
        {
            origin
        };
        var cost = new Dictionary<Vector2Int, float>
        {
            {
                origin,
                0
            }
        };
        var parent = new Dictionary<Vector2Int, Vector2Int>();
        var closed = new HashSet<Vector2Int>();
        var walkable = new Dictionary<Vector2Int, bool>();
        while (open.Count > 0 && closed.Count < 3500)
        {
            int best = 0;
            float score = float.MaxValue;
            for (int i = 0; i < open.Count; i++)
            {
                var q = open[i];
                var v = cost[q] + Vector2Int.Distance(q, target);
                if (v < score)
                {
                    best = i;
                    score = v;
                }
            }

            var cur = open[best];
            open.RemoveAt(best);
            if (!closed.Add(cur))
                continue;
            if (cur == target || Vector2Int.Distance(cur, target) < 1.5f)
            {
                var steps = new List<Vector2>
                {
                    end
                };
                while (cur != origin)
                {
                    steps.Add(start + (Vector2)cur * gap);
                    cur = parent[cur];
                }

                steps.Reverse();
                return new Queue<Vector2>(steps);
            }

            var here = At(start + (Vector2)cur * gap);
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0)
                        continue;
                    var n = cur + new Vector2Int(i, j);
                    if (closed.Contains(n))
                        continue;
                    var wp = At(start + (Vector2)n * gap);
                    if (!walkable.TryGetValue(n, out bool ok))
                    {
                        ok = wp.y > 18.4f && !Physics.OverlapCapsule(wp + Vector3.up * .55f, wp + Vector3.up * 1.85f, .48f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).Any(c => !(c is TerrainCollider) && !c.transform.IsChildOf(p.transform));
                        walkable[n] = ok;
                    }

                    float length = Mathf.Sqrt(i * i + j * j);
                    if (!ok || Mathf.Abs(wp.y - here.y) > gap * length * .77f)
                        continue;
                    float nc = cost[cur] + length + .1f * Mathf.Abs(wp.y - here.y);
                    if (cost.TryGetValue(n, out var was) && was <= nc)
                        continue;
                    cost[n] = nc;
                    parent[n] = cur;
                    if (!open.Contains(n))
                        open.Add(n);
                }
        }

        throw new Exception("no walking detour between " + start + " and " + end);
    }

    static void Stage(float x, float z, float y = -999)
    {
        var cc = p.GetComponent<CharacterController>();
        cc.enabled = false;
        var pt = new Vector3(x, 0, z);
        pt.y = y == -999 ? Terrain.activeTerrain.SampleHeight(pt) + .12f : y;
        p.transform.position = pt;
        cc.enabled = true;
        p.GetComponent<PlayerMovement>().ResetMotion();
        Physics.SyncTransforms();
    }

    static void Player()
    {
        p = Object.FindAnyObjectByType<Player>();
        gun = p.GetComponent<WeaponSystem>();
        trade = p.GetComponent<HarvestTrade>();
    }

    static IEnumerator Target(Component item)
    {
        bool aimed = false;
        var cs = item.GetComponentsInChildren<Collider>();
        foreach (var c in cs)
        {
            foreach (int x in new[]
            {
                0,
                1,
                -1,
                2,
                -2,
                3,
                -3
            }

            )
                foreach (int y in new[]
                {
                    0,
                    1,
                    -1,
                    2,
                    -2,
                    3,
                    -3
                }

                )
                    foreach (int z in new[]
                    {
                        0,
                        1,
                        -1,
                        2,
                        -2
                    }

                    )
                    {
                        if (aimed)
                            break;
                        Vector3 pos = c.bounds.center + Vector3.Scale(c.bounds.extents, new Vector3(x * .28f, y * .28f, z * .4f));
                        var eye = gun.playerCamera.transform.position;
                        if (Physics.Raycast(eye, pos - eye, out var hit, gun.pickupRange) && (hit.collider.transform == item.transform || hit.collider.transform.IsChildOf(item.transform)))
                        {
                            Face(pos);
                            aimed = true;
                        }
                    }

            if (aimed)
                break;
        }

        if (!aimed)
            throw new Exception("cannot target " + item.name + " at " + item.transform.position + " from " + gun.playerCamera.transform.position);
        yield return Frames(2);
        Physics.Raycast(gun.playerCamera.transform.position, gun.playerCamera.transform.forward, out var qaHit, gun.pickupRange);
        Debug.Log("DAQ targeting " + item.name + " ray " + qaHit.collider?.name + " eye " + gun.playerCamera.transform.position + " target " + item.transform.position + " prompt " + gun.CurrentInteractable?.GetPrompt(gun));
    }

    static IEnumerator Pic(string name)
    {
        string file = Path.Combine(Output, name + ".png");
        yield return new WaitForEndOfFrame();
        var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(file, screenshot.EncodeToPNG());
        Object.Destroy(screenshot);
    }

    static IEnumerator Open()
    {
        yield return Target(trade.dealer);
        yield return Press(Key.E);
        Check(trade.IsOpen && Cursor.visible && !gun.playerCamera.GetComponent<FirstPersonLook>().enabled, "dealer interaction opened and released cursor");
    }

    static float Hp(Creature c) => c ? (float)typeof(Creature).GetField("currentHealth", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(c) : 0;
    static IEnumerator Approach(Creature target, float meters)
    {
        for (int i = 0; i < 8; i++)
        {
            var targetAt = target.transform.position;
            var candidate = targetAt + Quaternion.Euler(0, i * 45, 0) * (Vector3.left * meters);
            Stage(candidate.x, candidate.z);
            yield return Frames(4);
            var center = target.GetComponent<CharacterController>().bounds.center;
            var eye = gun.playerCamera.transform.position;
            if (Physics.Raycast(eye, center - eye, out var hit, meters + 5) && hit.collider.GetComponentInParent<Creature>() == target)
                yield break;
        }

        throw new Exception("No clear approach to " + target.name);
    }

    static IEnumerator Take(MeatPickup m, int expected)
    {
        int old = trade.Meat;
        var dest = XZ(m.transform.position);
        yield return Walk(dest + (XZ(p.transform.position) - dest).normalized * 1.45f);
        yield return Target(m);
        Check(m.Amount == expected && gun.CurrentInteractable == (IInteractable)m, "ray selected " + expected + " meat");
        yield return Press(Key.E);
        yield return Press(Key.E);
        Check(!m && trade.Meat == old + expected, "E collected once " + old + " -> " + trade.Meat);
    }

    private static IEnumerator VisitCache(string key, Vector2 trail)
    {
        var reward = Object.FindObjectsByType<PowerupPickup>(FindObjectsInactive.Include).Single(r => r.cacheId == key);
        if (reward.isActiveAndEnabled)
        {
            var site = XZ(reward.transform.position);
            yield return Walk(site, .8f);
            yield return Frames(3);
        }

        Check(SaveGameService.ActiveSave.progress.exploredCaches.Contains(key), "collected " + key + " on foot from the main route");
        yield return Walk(trail, .8f);
    }

    static IEnumerator Gameplay(bool after)
    {
        yield return Frames(5);
        Time.captureFramerate = 30;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
        UnityEngine.Random.InitState(517);
        InputSystem.settings = Object.Instantiate(InputSystem.settings);
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        foreach (var device in InputSystem.devices.ToArray())
            if (device is Keyboard || device is Mouse)
                InputSystem.DisableDevice(device);
        kb = InputSystem.AddDevice<Keyboard>();
        mouse = InputSystem.AddDevice<Mouse>();
        yield return Frames(5);
        var menu = Object.FindAnyObjectByType<NewGameMenuController>();
        yield return ButtonClick(menu.newGameButton);
        Check(menu.newGameScreen.activeInHierarchy, "pointer opens New Game");
        int slotsBefore = SaveGameService.GetAll().Count;
        yield return Press(Key.Enter);
        Check(menu.newGameScreen.activeInHierarchy && SaveGameService.GetAll().Count == slotsBefore && !string.IsNullOrEmpty(menu.messageText.text), "empty name kept menu open and created no save");
        save = "Integration " + Guid.NewGuid().ToString("N").Substring(0, 9);
        yield return ButtonClick(menu.levelButtons[0]);
        var field = (RectTransform)menu.saveNameInput.transform;
        yield return Click(RectTransformUtility.WorldToScreenPoint(null, field.TransformPoint(field.rect.center)));
        yield return Frames(2);
        Check(menu.saveNameInput.isFocused, "harvest name field focused");
        foreach (char letter in save)
        {
            menu.saveNameInput.ProcessEvent(new Event { type = EventType.KeyDown, character = letter });
            yield return null;
        }

        Check(menu.saveNameInput.text == save, "typed harvest name");
        yield return Press(Key.Enter);
        for (int i = 0; i < 320 && SceneManager.GetActiveScene().name != "DevilsHarvest_Island"; i++)
            yield return null;
        yield return Frames(100);
        Player();
        Check(p != null, "Start Harvest on island as " + p.name);
        Check(SaveGameService.GetAll().Count(slot => slot.saveName == save) == 1, "single Enter created one save");
        float since = Time.time;
        var ak = Object.FindObjectsByType<WeaponPickup>().Where(w => w.isActiveAndEnabled && w.Definition is WeaponData d && d.useType == WeaponUseType.Gun).OrderBy(w => Vector3.Distance(w.transform.position, p.transform.position)).First();
        Face(ak.transform.position);
        yield return Frames(2);
        yield return Pic("morning-beach");
        yield return Walk(XZ(ak.transform.position) + Vector2.down * 1.5f);
        yield return Target(ak);
        yield return Press(Key.E);
        Check(gun.LoadedAmmo == 150, "first day beach AK");
        if (routeMode == "trail")
        {
            yield return IslandTrail();
            yield break;
        }
        if (after)
        {
            yield return Mechanics();
            yield break;
        }

        var route = BeachTrail.Split(',').Select(ParsePoint).ToArray();
        int hunts = 0;
        foreach (var wp in route)
        {
            yield return Walk(wp);
            if (wp == new Vector2(326, 153))
                yield return VisitCache("tidewatch", wp);
            if (hunts >= 3)
                continue;
            var eye = gun.playerCamera.transform.position;
            var candidates = Object.FindObjectsByType<CreatureAI>().OrderBy(c => Vector2.Distance(XZ(c.transform.position), XZ(eye))).ToArray();
            foreach (var ai in candidates)
            {
                if (!ai || Vector3.Distance(eye, ai.transform.position) > 64)
                    continue;
                var c = ai.GetComponent<Creature>();
                var cc = c.GetComponent<CharacterController>();
                if (!Sight(c, eye))
                    continue;
                if (hunts == 0)
                    Debug.Log("DAY spotted at " + (Time.time - since) + " range=" + Vector3.Distance(eye, cc.bounds.center));
                for (int i = 0; c && i < 10; i++)
                {
                    if (!Sight(c, gun.playerCamera.transform.position))
                        break;
                    Face(cc.bounds.center);
                    yield return Frames(1);
                    yield return Click(Vector2.zero);
                    yield return Frames(3);
                }

                if (c && Hp(c) <= 0)
                    yield return Frames(35);
                if (c)
                    continue;
                var drop = Object.FindObjectsByType<MeatPickup>().OrderBy(m => Vector3.Distance(eye, m.transform.position)).First();
                yield return Take(drop, 5);
                if (trade.Meat == 10)
                {
                    Check(GameObject.Find("Destination bearings") != null, "earned meat revealed both bearings");
                    yield return Pic("earned-bearings");
                }

                hunts++;
                Debug.Log("DAY hunt " + hunts + " elapsed=" + (Time.time - since) + "s meat=" + trade.Meat + " at " + p.transform.position);
                yield return Walk(wp);
                break;
            }
        }

        Check(hunts >= 3 && trade.Meat >= 15, "three earned beach/trail hunts");
        yield return Open();
        yield return Pic("dealer");
        yield return ButtonClick(trade.shop.rows[0].button);
        Check(gun.EquippedWeapon == trade.dealer.stock[0].weapon && trade.Meat == 11, "sidearm with earned meat; 11 left");
        yield return Press(Key.Escape);
        var back = SaddleTrail.Split(',').Select(ParsePoint).ToArray();
        foreach (var wp in back)
        {
            yield return Walk(wp);
            if (wp == new Vector2(800, 648))
                yield return VisitCache("ridge-survey", wp);
        }

        yield return Walk(new Vector2(790, 752));
        yield return Walk(new Vector2(793, 754.4f));
        var altar = Object.FindAnyObjectByType<AltarOffering>();
        yield return Target(altar);
        for (int i = 0; i < 10; i++)
            yield return Press(Key.E);
        Check(SaveGameService.ActiveSave.progress.firstQuotaResult == 1 && trade.Meat == 1, "quota complete with earned meat");
        Debug.Log("DAY outcome elapsed=" + SaveGameService.ActiveSave.progress.firstDayElapsed + " distance=" + distance + " remaining=" + trade.Meat);
        yield return Pic("First quota met");
        var slot = SaveGameService.ActiveSave.id;
        yield return Press(Key.Enter);
        yield return Frames(8);
        menu = Object.FindAnyObjectByType<NewGameMenuController>();
        yield return ButtonClick(menu.loadGameButton);
        var row = menu.saveListRoot.Find("Save_" + slot).GetComponent<Button>();
        yield return ButtonClick(row);
        yield return ButtonClick(menu.playSelectedSaveButton);
        yield return Frames(35);
        Player();
        Check(gun.EquippedWeapon == trade.dealer.stock[0].weapon && trade.Meat == 1 && SaveGameService.ActiveSave.progress.firstQuotaResult == 1, "quota outcome and purchased sidearm restored");
    }
}
#endif
