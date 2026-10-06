using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using WorldStreaming;

namespace BetterSolidTerrain;

internal static class HarmonyPatches
{
    internal static void Patch(Harmony harmony) {
        harmony.PatchAll(typeof(EnableCollidersInFartherChunks));
        harmony.PatchAll(typeof(KeepCollisionMeshesInMemory));
        harmony.PatchAll(typeof(ToggleChunkCollisionWithVisual));
    }

    internal static class EnableCollidersInFartherChunks {
        [HarmonyPatch(typeof(WorldStreamer), nameof(WorldStreamer.ParseClipmapSettings))]
        [HarmonyPostfix]
        private static void Postfix(ref ClipMapManager.Settings __result) {
            Plugin.Logger.LogInfo("Patching world streamer clipmap settings!");
            for (int i = 1; i < __result.levels.Length; i++) {
                ClipMapManager.LevelSettings level = __result.levels[i];
                if (!level.entities || level.ignoreMeshes) continue;
                level.colliders = true; 
            }
        }
    }

    internal static class KeepCollisionMeshesInMemory {
        [HarmonyPatch(typeof(ClipmapCell), nameof(ClipmapCell.FinalizeCollidersIfNecessary))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> RemoveMeshClear(
            IEnumerable<CodeInstruction> instructions)
            => new CodeMatcher(instructions)
                .MatchStartForward(
                    new(OpCodes.Ldarg_1),
                    new(OpCodes.Ldfld,
                        AccessTools.Field(typeof(ClipmapChunk), nameof(ClipmapChunk.collision))
                    ),
                    new(OpCodes.Callvirt,
                        AccessTools.PropertyGetter(typeof(MeshCollider), nameof(MeshCollider.sharedMesh))
                    ),
                    new(OpCodes.Callvirt, 
                        AccessTools.Method(typeof(Mesh), nameof(Mesh.Clear))
                    )
                )
                .ThrowIfNotMatch($"Could not match onto {nameof(ClipmapCell)}." +
                                 $"{nameof(ClipmapCell.FinalizeCollidersIfNecessary)}!")
                .RemoveInstructions(4)
                .InstructionEnumeration();
    }

    internal static class ToggleChunkCollisionWithVisual {
        [HarmonyPatch(typeof(ClipmapChunk), nameof(ClipmapChunk.FadeInTerrain))]
        [HarmonyPostfix]
        private static void FadeInTerrain(ClipmapChunk __instance) => ToggleCollision(__instance, true);

        [HarmonyPatch(typeof(ClipmapChunk), nameof(ClipmapChunk.FadeIn))]
        [HarmonyPostfix]
        private static void FadeIn(ClipmapChunk __instance) => ToggleCollision(__instance, true);

        [HarmonyPatch(typeof(ClipmapChunk), nameof(ClipmapChunk.Show))]
        [HarmonyPostfix]
        private static void Show(ClipmapChunk __instance) => ToggleCollision(__instance, true);

        [HarmonyPatch(typeof(ClipmapChunk), nameof(ClipmapChunk.ShowTerrain))]
        [HarmonyPostfix]
        private static void ShowTerrain(ClipmapChunk __instance) => ToggleCollision(__instance, true);

        [HarmonyPatch(typeof(ClipmapChunk), nameof(ClipmapChunk.FadeOutTerrain))]
        [HarmonyPostfix]
        private static void FadeOutTerrain(ClipmapChunk __instance) => ToggleCollision(__instance, false);

        [HarmonyPatch(typeof(ClipmapChunk), nameof(ClipmapChunk.FadeOut))]
        [HarmonyPostfix]
        private static void FadeOut(ClipmapChunk __instance) => ToggleCollision(__instance, false);

        [HarmonyPatch(typeof(ClipmapChunk), nameof(ClipmapChunk.Hide), typeof(bool))]
        [HarmonyPostfix]
        private static void Hide(ClipmapChunk __instance) => ToggleCollision(__instance, false);

        [HarmonyPatch(typeof(ClipmapChunk), nameof(ClipmapChunk.HideTerrain))]
        [HarmonyPostfix]
        private static void HideTerrain(ClipmapChunk __instance) => ToggleCollision(__instance, false);

        private static void ToggleCollision(ClipmapChunk chunk, bool value) {
            MeshCollider collider = chunk.collision;
            if (collider == null) return;
            collider.gameObject.SetActive(value);
        }
    }
}