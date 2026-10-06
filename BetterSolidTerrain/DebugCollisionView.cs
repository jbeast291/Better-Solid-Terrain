using HarmonyLib;
using Unity.Jobs;
using UnityEngine;
using WorldStreaming;

namespace BetterSolidTerrain;

internal static class DebugCollisionView {

    internal static void Patch(Harmony harmony) 
        => harmony.PatchAll(typeof(DebugCollisionView));

    [HarmonyPatch(typeof(ClipmapCell), nameof(ClipmapCell.FinalizeCollidersIfNecessary))]
    [HarmonyPostfix]
    private static void AttachDebugVisualCollisionMesh(ClipmapChunk chunk, JobHandle jobHandle) {
        if (chunk == null) return;
        AttachDebugCollisionMesh(chunk);
    }

    private static void AttachDebugCollisionMesh(ClipmapChunk chunk) {
        if (chunk == null || chunk.collision == null || chunk.collision.sharedMesh == null) return;

        MeshCollider collider = chunk.collision;

        Transform oldDebugMesh = collider.transform.Find("Collision View");
        if (oldDebugMesh != null) Object.DestroyImmediate(oldDebugMesh.gameObject);

        GameObject collisionView = new("Collision View");
        collisionView.transform.SetParent(collider.transform, false);
        collisionView.transform.localPosition = Vector3.zero;
        collisionView.transform.localRotation = Quaternion.identity;
        collisionView.transform.localScale = Vector3.one;
        collisionView.layer = 0;

        Mesh visualMesh = Object.Instantiate(collider.sharedMesh);
        visualMesh.RecalculateNormals();
        visualMesh.RecalculateBounds();

        MeshFilter filter = collisionView.EnsureComponent<MeshFilter>();
        collisionView.EnsureComponent<MeshRenderer>();
        filter.sharedMesh = visualMesh;
    }
}