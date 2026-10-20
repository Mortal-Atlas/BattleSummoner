#if VISTA && UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Pinwheel.Vista
{
    /// <summary>
    /// Stores editor snapshots as regular files under Assets/VistaData/TileSnapshots.
    /// </summary>
    internal sealed class EditorTileSnapshotStorage : TileSnapshotStorage
    {
        private protected override string rootDirectory => Path.Combine(
            Application.dataPath,
            "VistaData",
            "TileSnapshots");

        private protected override AssetIdentity GetAssetIdentity(Object asset)
        {
            if (asset == null ||
                !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localId))
            {
                return default;
            }

            return new AssetIdentity
            {
                guid = guid,
                localFileId = localId
            };
        }

        private protected override T ResolveAsset<T>(AssetIdentity identity)
        {
            if (identity.isEmpty)
                return null;

            string path = AssetDatabase.GUIDToAssetPath(identity.guid);
            if (string.IsNullOrEmpty(path))
                return null;

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; ++i)
            {
                if (!(assets[i] is T candidate))
                    continue;
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    candidate,
                    out string candidateGuid,
                    out long candidateLocalId) &&
                    candidateGuid == identity.guid &&
                    candidateLocalId == identity.localFileId)
                {
                    return candidate;
                }
            }
            return null;
        }
    }
}
#endif
