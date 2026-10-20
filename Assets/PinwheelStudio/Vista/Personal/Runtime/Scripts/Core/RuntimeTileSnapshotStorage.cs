#if VISTA
using System;
using System.IO;
using UnityEngine;

namespace Pinwheel.Vista
{
    /// <summary>
    /// Stores player snapshots under the application's persistent data directory.
    /// </summary>
    /// <remarks>
    /// Unity asset GUIDs are unavailable in players. Runtime asset identities therefore use the concrete type and
    /// object name, and can resolve only assets that are already loaded. Ambiguous identities are treated as
    /// unresolved so the shared storage falls back to its serialized template or terrain-layer data.
    /// </remarks>
    internal sealed class RuntimeTileSnapshotStorage : TileSnapshotStorage
    {
        private const string RUNTIME_ID_PREFIX = "runtime:";

        private protected override string rootDirectory => Path.Combine(
            Application.persistentDataPath,
            "VistaData",
            "TileSnapshots");

        private protected override AssetIdentity GetAssetIdentity(UnityEngine.Object asset)
        {
            if (asset == null || IsSceneObject(asset))
                return default;
            if (IsSnapshotContainerType(asset.GetType()))
                return default;
            if (string.IsNullOrEmpty(asset.name))
            {
                Debug.LogWarning(
                    $"Runtime snapshot reference of type '{asset.GetType().FullName}' has no name and cannot be " +
                    "resolved after loading. Assign a name to the runtime-created object before capturing the snapshot.",
                    asset);
                return default;
            }

            return new AssetIdentity
            {
                guid = GetRuntimeId(asset),
                localFileId = 0
            };
        }

        private protected override T ResolveAsset<T>(AssetIdentity identity)
        {
            // Snapshot containers can be synthesized, unnamed, and owned by another capture or loaded BiomeData.
            // Resolving them from the global object list can alias multiple metadata records to the same transient
            // object. Always reconstruct containers from their serialized fallback instead.
            if (IsSnapshotContainerType(typeof(T)))
            {
                return null;
            }

            if (identity.isEmpty || !identity.guid.StartsWith(RUNTIME_ID_PREFIX, StringComparison.Ordinal))
                return null;

            T match = null;
            T[] candidates = Resources.FindObjectsOfTypeAll<T>();
            for (int i = 0; i < candidates.Length; ++i)
            {
                T candidate = candidates[i];
                if (candidate == null ||
                    IsSceneObject(candidate) ||
                    IsPrefabChild(candidate) ||
                    GetRuntimeId(candidate) != identity.guid)
                {
                    continue;
                }
                if (match != null && match != candidate)
                {
                    Debug.LogWarning(
                        $"Runtime snapshot reference '{identity.guid}' is ambiguous. " +
                        "Rename the referenced assets so their type and name are unique.");
                    return null;
                }
                match = candidate;
            }
            if (match == null)
            {
                Debug.LogWarning(
                    $"Runtime snapshot reference '{identity.guid}' could not be resolved. " +
                    "Ensure the referenced asset is loaded before loading the snapshot.");
            }
            return match;
        }

        private static string GetRuntimeId(UnityEngine.Object asset)
        {
            return RUNTIME_ID_PREFIX + asset.GetType().FullName + ":" + asset.name;
        }

        private static bool IsSnapshotContainerType(Type type)
        {
            return type == typeof(TerrainLayer) ||
                type == typeof(TreeTemplate) ||
                type == typeof(DetailTemplate);
        }

        private static bool IsSceneObject(UnityEngine.Object asset)
        {
            if (asset is GameObject gameObject)
                return gameObject.scene.IsValid();
            if (asset is Component component)
                return component.gameObject.scene.IsValid();
            return false;
        }

        private static bool IsPrefabChild(UnityEngine.Object asset)
        {
            return asset is GameObject gameObject && gameObject.transform.parent != null;
        }
    }
}
#endif
