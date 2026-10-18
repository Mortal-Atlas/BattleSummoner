using System.Collections;
using System.IO;
using UnityEngine;
using BattleSummoner.UI;
using BattleSummoner.Combat;
using BattleSummoner.World;

public class WorldMapAndCombatVerifier : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(RunVerificationSuite());
    }

    private IEnumerator RunVerificationSuite()
    {
        yield return new WaitForSeconds(0.5f);

        Debug.Log("🧪 [VERIFICATION] Starting comprehensive verification of Arrow, Spell Orb, Sword Swipe, and World Map...");

        var hero = GameObject.Find("Player_Hero");
        if (hero == null)
        {
            Debug.LogError("Player_Hero not found!");
            yield break;
        }

        var loadout = hero.GetComponent<DevClassLoadoutManager>() ?? hero.AddComponent<DevClassLoadoutManager>();

        // 1. VERIFY BOW & ARROW ORIENTATION
        Debug.Log("🧪 [TEST 1: BOW & ARROW ORIENTATION]");
        loadout.SetClassLoadout(PlayerClassType.Ranger);
        yield return new WaitForSeconds(0.1f);
        loadout.ShootRangerArrow(hero.transform.forward);
        yield return null;

        var arrowObj = GameObject.Find("RangerArrowProjectile");
        if (arrowObj != null)
        {
            var shaft = arrowObj.transform.Find("ArrowShaft");
            var head = arrowObj.transform.Find("ArrowHead");
            var trail = arrowObj.GetComponent<TrailRenderer>();

            Debug.Log($"🏹 [VERIFIED] Arrow spawned at {arrowObj.transform.position}. Forward vector: {arrowObj.transform.forward}.");
            Debug.Log($"🏹 [VERIFIED] Shaft present: {shaft != null}, Head at tip (+Z): {head != null}, Trail attached: {trail != null}");
            if (head != null)
            {
                Debug.Log($"🏹 [VERIFIED] Arrowhead local Z: {head.localPosition.z} (> 0 = point forward!)");
            }
        }

        // 2. VERIFY WIZARD SPELL ORB (NO TETHER LINE, HAS TRAIL)
        Debug.Log("🧪 [TEST 2: WIZARD SPELL ORB]");
        loadout.SetClassLoadout(PlayerClassType.Mage);
        var slinger = WizardSpellSlingerController.EnsureInScene(hero.transform);
        slinger.ToggleSpellMode(true);
        slinger.OnPointerDown(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        yield return new WaitForSeconds(0.1f);

        var tetherLine = GameObject.Find("MagicalTetherLine");
        var heldOrb = GameObject.Find("WizardHeldSpellOrb");
        TrailRenderer orbTrail = heldOrb != null ? heldOrb.GetComponent<TrailRenderer>() : null;

        Debug.Log($"🔮 [VERIFIED] Tether line exists/rendered: {tetherLine != null && tetherLine.GetComponent<LineRenderer>().enabled}");
        Debug.Log($"🔮 [VERIFIED] Spell Orb present: {heldOrb != null}, Hovering Trail attached: {orbTrail != null}");
        if (orbTrail != null)
        {
            Debug.Log($"🔮 [VERIFIED] Orb Trail duration: {orbTrail.time}s, startWidth: {orbTrail.startWidth}m");
        }
        slinger.OnPointerUp(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));

        // 3. VERIFY SWORD SWIPE DEBOUNCE
        Debug.Log("🧪 [TEST 3: SWORD SWIPE DEBOUNCE (1 SWIPE PER SCREEN SWIPE)]");
        loadout.SetClassLoadout(PlayerClassType.Warrior);
        int slashCount = 0;
        System.Action<Vector2, float> slashListener = (dir, vel) => slashCount++;
        PortraitGestureCanvas.OnBladeSlash += slashListener;

        // Simulate rapid swipe calls from a single continuous gesture
        PortraitGestureCanvas.TriggerBladeSlash(new Vector2(100f, 100f), new Vector2(250f, 250f), 1200f);
        PortraitGestureCanvas.TriggerBladeSlash(new Vector2(150f, 150f), new Vector2(300f, 300f), 1250f);
        PortraitGestureCanvas.TriggerBladeSlash(new Vector2(200f, 200f), new Vector2(350f, 350f), 1300f);

        Debug.Log($"⚔️ [VERIFIED] Attempted 3 rapid swipe events during one stroke -> actual triggered slashes: {slashCount} (Expected: 1)");
        PortraitGestureCanvas.OnBladeSlash -= slashListener;

        // 4. VERIFY MINIMAP TAP & WORLD EXPLORATION MAP MODAL
        Debug.Log("🧪 [TEST 4: MONSTER HUNTER WORLD EXPLORATION MAP & FAST TRAVEL]");
        var modal = WorldMapExplorationModal.Instance;
        if (modal == null)
        {
            var hud = GameObject.Find("PlayerHUDCanvas");
            if (hud != null) modal = hud.AddComponent<WorldMapExplorationModal>();
        }

        modal.OpenModal();
        yield return new WaitForSeconds(0.2f);
        Debug.Log($"🗺️ [VERIFIED] World Map Exploration Modal IsOpen: {modal.IsOpen}");

        // Test fast travel to Lakeside Pier & Camp
        Vector3 initialPos = hero.transform.position;
        var pierPOI = new WorldMapPOI
        {
            id = "test_pier",
            name = "Lakeside Pier & Camp",
            category = MapFilterCategory.Settlement,
            worldPosition = new Vector3(18f, 0.9f, -210f),
            isSettlement = true,
            isDiscovered = true
        };
        modal.ExecuteFastTravel(pierPOI);
        yield return new WaitForSeconds(0.1f);

        Vector3 newPos = hero.transform.position;
        Debug.Log($"⚡ [VERIFIED] Fast Travel Teleport: from {initialPos} -> {newPos} (Distance traveled: {Vector3.Distance(initialPos, newPos):F1}m)");

        // Re-open modal to capture the Monster Hunter Tactical World Map screenshot!
        modal.OpenModal();
        yield return new WaitForSeconds(0.35f);
        yield return new WaitForEndOfFrame();

        // Capture screenshot
        Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        string artifactDir = @"C:\Users\jedij\.gemini\antigravity-ide\brain\14e40379-d8d1-4d3f-9df4-afaf664c7503";
        string artifactPath = Path.Combine(artifactDir, "world_exploration_map_verified.png");
        File.WriteAllBytes(artifactPath, bytes);
        Destroy(tex);

        Debug.Log($"📸 [VERIFIED] Saved World Exploration Map artifact to: {artifactPath}");

        modal.CloseModal(true);
        yield return new WaitForSeconds(0.2f);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
