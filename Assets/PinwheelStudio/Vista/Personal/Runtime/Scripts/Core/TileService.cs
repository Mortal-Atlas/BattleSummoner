#if VISTA
namespace Pinwheel.Vista
{
    /// <summary>
    /// Provides tile-related services for the current platform.
    /// </summary>
    public static class TileService
    {
        private static readonly TileSnapshotStorage s_snapshotStorage = CreateSnapshotStorage();

        public static bool isSnapshotStorageAvailable => s_snapshotStorage != null;
        public static TileSnapshotStorage snapshotStorage => s_snapshotStorage;

        private static TileSnapshotStorage CreateSnapshotStorage()
        {
#if UNITY_EDITOR
            return new EditorTileSnapshotStorage();
#else
            return new RuntimeTileSnapshotStorage();
#endif
        }
    }
}
#endif
