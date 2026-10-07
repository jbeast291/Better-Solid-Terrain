using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Nautilus.Extensions;
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
        /// <summary> called when hidden by parent </summary>
        [HarmonyPatch(typeof(ClipmapCell), nameof(ClipmapCell.FadeInMesh), typeof(object), typeof(object))]
        [HarmonyPostfix]
        private static void FadeIn(object owner)
            => ToggleCollision(((ClipmapCell) owner).chunk, true);
        
        [HarmonyPatch(typeof(ClipmapCell), nameof(ClipmapCell.FinalizeCollidersIfNecessary))]
        [HarmonyPostfix]
        private static void FinalizeCollidersIfNecessary(ClipmapChunk chunk)
            => ToggleCollision(chunk, true);
        
        [HarmonyPatch(typeof(ClipmapCell), nameof(ClipmapCell.ShowMesh), typeof(object),
            typeof(object))]
        [HarmonyPostfix]
        private static void Show(object owner)
            => ToggleCollision(((ClipmapCell) owner).chunk, true);

        [HarmonyPatch(typeof(ClipmapCell), nameof(ClipmapCell.HideMesh), typeof(object),
            typeof(object))]
        [HarmonyPostfix]
        private static void Hide(object owner)
            => ToggleCollision(((ClipmapCell) owner).chunk, false);

        private static void ToggleCollision(ClipmapChunk chunk, bool value) {
            if (!chunk.Exists()) return;
            MeshCollider collider = chunk.collision;
            if (collider == null) return;
            collider.gameObject.SetActive(value);
        }
    }
}