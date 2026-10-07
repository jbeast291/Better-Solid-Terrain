using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace BetterSolidTerrain;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
[BepInIncompatibility("SolidTerrain")] // I dislike how mr. purple didn't make this more specific :(
internal class Plugin : BaseUnityPlugin {
    private const string PLUGIN_GUID = "com.jbeast.betterSolidTerrain";
    private const string PLUGIN_NAME = "Better Solid Terrain";
    private const string PLUGIN_VERSION = "1.0.0";
    private const bool ENABLE_DEBUG_COLLISION_VIEW = false;

    internal new static ManualLogSource Logger { get; private set; }

    private void Awake() {
        Logger = base.Logger;

        var harmony = new Harmony(PLUGIN_GUID);
        HarmonyPatches.Patch(harmony);

#pragma warning disable CS0162 // Unreachable code detected
        if (ENABLE_DEBUG_COLLISION_VIEW) {
            DebugCollisionView.Patch(harmony);
        }
#pragma warning restore CS0162 // Unreachable code detected

        Logger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!"); 
    }
}