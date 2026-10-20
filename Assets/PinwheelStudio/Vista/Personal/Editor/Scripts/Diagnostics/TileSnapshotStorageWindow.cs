#if VISTA
using System;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista.Graph;
using UnityEditor;
using UnityEngine;

namespace Pinwheel.Vista.Editor
{
    internal sealed class TileSnapshotStorageWindow : EditorWindow
    {
        private const string MENU_PATH = "Window/Vista/Diagnostics/Tile Snapshot Storage";

        private MonoBehaviour m_tileComponent;
        private BiomeData m_capturedData;
        private BiomeData m_loadedData;
        private TransientResourceRegistry m_captureResources;
        private TransientResourceRegistry m_loadedResources;
        private CancellationHandle m_captureCancellation;
        private TileSnapshotStorage.Key m_captureKey;
        private bool m_hasCaptureKey;
        private bool m_isCapturing;
        private bool m_isClosed;
        private TileSnapshotStorage.Metadata m_metadata;
        private Texture2D m_thumbnail;
        private string m_status;
        private Vector2 m_scrollPosition;
        private bool m_showCapturedData = true;
        private bool m_showLoadedData = true;
        private bool m_showThumbnail = true;
        private bool m_showMetadata = true;

        private ITileSnapshotProvider selectedProvider => m_tileComponent as ITileSnapshotProvider;

        [MenuItem(MENU_PATH)]
        private static void Open()
        {
            GetWindow<TileSnapshotStorageWindow>("Tile Snapshots");
        }

        private void OnEnable()
        {
            m_isClosed = false;
        }

        private void OnDisable()
        {
            m_isClosed = true;
            m_captureCancellation?.Cancel();
            if (!m_isCapturing)
                ClearCapturedData();
            ClearLoadedData();
            ClearThumbnail();
        }

        private void OnGUI()
        {
            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
            DrawTileSelection();
            EditorGUILayout.Space();
            DrawKey();
            EditorGUILayout.Space();
            DrawActions();

            if (!string.IsNullOrEmpty(m_status))
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(m_status, MessageType.Info);
            }

            EditorGUILayout.Space();
            m_showThumbnail = EditorGUILayout.Foldout(m_showThumbnail, "Stored Thumbnail", true);
            if (m_showThumbnail)
                DrawThumbnail();

            EditorGUILayout.Space();
            m_showCapturedData = EditorGUILayout.Foldout(m_showCapturedData, "Captured Data", true);
            if (m_showCapturedData)
                DrawBiomeData(m_capturedData);

            EditorGUILayout.Space();
            m_showLoadedData = EditorGUILayout.Foldout(m_showLoadedData, "Loaded Data", true);
            if (m_showLoadedData)
                DrawBiomeData(m_loadedData);

            EditorGUILayout.Space();
            m_showMetadata = EditorGUILayout.Foldout(m_showMetadata, "Stored Metadata", true);
            if (m_showMetadata)
                DrawMetadata(m_metadata);
            EditorGUILayout.EndScrollView();
        }

        private void DrawTileSelection()
        {
            EditorGUILayout.LabelField("Scene Tile", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            MonoBehaviour candidate = (MonoBehaviour)EditorGUILayout.ObjectField(
                "Snapshot Provider",
                m_tileComponent,
                typeof(MonoBehaviour),
                true);
            if (EditorGUI.EndChangeCheck())
            {
                m_tileComponent = candidate is ITileSnapshotProvider ? candidate : null;
                m_metadata = null;
                ClearThumbnail();
                m_status = candidate == null || m_tileComponent != null
                    ? null
                    : "The selected component does not implement ITileSnapshotProvider.";
            }

            if (GUILayout.Button("Use Current Selection"))
            {
                m_tileComponent = FindProviderComponent(Selection.activeGameObject);
                m_metadata = null;
                ClearThumbnail();
                m_status = m_tileComponent != null
                    ? null
                    : "The current selection does not contain an ITileSnapshotProvider.";
            }
        }

        private void DrawKey()
        {
            ITileSnapshotProvider provider = selectedProvider;
            EditorGUILayout.LabelField("Current Snapshot Key", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Vista Manager ID", provider != null ? provider.managerId : string.Empty);
                EditorGUILayout.TextField(
                    "Region",
                    provider != null ? RectXZ.FromBounds(provider.worldBounds).ToString() : string.Empty);
            }
        }

        private void DrawActions()
        {
            ITileSnapshotProvider provider = selectedProvider;
            TileSnapshotStorage storage = TileService.snapshotStorage;
            bool hasValidKey = provider != null && !string.IsNullOrWhiteSpace(provider.managerId);

            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!hasValidKey || m_isCapturing))
                {
                    if (GUILayout.Button(m_isCapturing ? "Capturing..." : "Capture"))
                        BeginCapture(provider);
                }
                using (new EditorGUI.DisabledScope(m_capturedData == null || !m_hasCaptureKey || storage == null))
                {
                    if (GUILayout.Button("Store Captured"))
                        StoreCaptured(storage);
                }
                using (new EditorGUI.DisabledScope(!hasValidKey || storage == null))
                {
                    if (GUILayout.Button("Load Stored"))
                        LoadStored(storage, CreateKey(provider));
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!hasValidKey || storage == null))
                {
                    if (GUILayout.Button("Refresh Metadata"))
                        RefreshMetadata(storage, CreateKey(provider));
                }
                using (new EditorGUI.DisabledScope(m_isCapturing))
                {
                    if (GUILayout.Button("Clear In-Memory Data"))
                    {
                        ClearCapturedData();
                        ClearLoadedData();
                        ClearThumbnail();
                        m_status = "Released captured and loaded in-memory data.";
                    }
                }
            }

            if (provider != null && !hasValidKey)
                EditorGUILayout.HelpBox("The selected tile has no Vista Manager ID.", MessageType.Warning);
            if (storage == null)
                EditorGUILayout.HelpBox("No snapshot storage is available for the current platform.", MessageType.Warning);
        }

        private void BeginCapture(ITileSnapshotProvider provider)
        {
            ClearCapturedData();
            m_captureKey = CreateKey(provider);
            m_hasCaptureKey = true;
            m_captureResources = new TransientResourceRegistry();
            m_captureCancellation = new CancellationHandle();
            try
            {
                BiomeDataRequest request = provider.CaptureSnapshot(
                    m_captureResources,
                    m_captureCancellation.signal);
                m_isCapturing = true;
                m_status = "Capturing the selected tile...";
                CoroutineUtility.StartCoroutine(
                    CompleteCapture(request),
                    HandleCaptureException);
            }
            catch (Exception exception)
            {
                HandleCaptureException(exception);
            }
        }

        private IEnumerator CompleteCapture(BiomeDataRequest request)
        {
            yield return request;
            m_isCapturing = false;
            m_captureCancellation = null;
            if (request.isFaulted)
            {
                request.data?.Dispose();
                DisposeCaptureResources();
                m_hasCaptureKey = false;
                m_status = $"Capture failed: {request.exception.Message}";
            }
            else if (m_isClosed)
            {
                request.data?.Dispose();
                DisposeCaptureResources();
                m_hasCaptureKey = false;
            }
            else
            {
                m_capturedData = request.data;
                m_status = $"Captured tile '{m_captureKey}'.";
                Repaint();
            }
        }

        private void HandleCaptureException(Exception exception)
        {
            m_isCapturing = false;
            m_captureCancellation = null;
            m_capturedData?.Dispose();
            m_capturedData = null;
            DisposeCaptureResources();
            m_hasCaptureKey = false;
            m_status = $"Capture failed: {exception.Message}";
            Debug.LogException(exception, m_tileComponent);
            if (!m_isClosed)
                Repaint();
        }

        private void StoreCaptured(TileSnapshotStorage storage)
        {
            if (storage.TryStore(m_captureKey, m_capturedData, out string error))
            {
                m_status = $"Stored snapshot '{m_captureKey}'.";
                RefreshMetadata(storage, m_captureKey, false);
            }
            else
            {
                m_status = $"Store failed: {error}";
            }
        }

        private void LoadStored(TileSnapshotStorage storage, TileSnapshotStorage.Key key)
        {
            ClearLoadedData();
            m_loadedResources = new TransientResourceRegistry();
            if (storage.TryLoad(key, m_loadedResources, out m_loadedData, out string error))
            {
                m_status = $"Loaded snapshot '{key}'.";
                RefreshMetadata(storage, key, false);
            }
            else
            {
                m_loadedResources.Dispose();
                m_loadedResources = null;
                m_status = $"Load failed: {error}";
            }
        }

        private void RefreshMetadata(
            TileSnapshotStorage storage,
            TileSnapshotStorage.Key key,
            bool updateStatus = true)
        {
            if (storage.TryGetMetadata(key, out m_metadata, out string error))
            {
                if (updateStatus)
                    m_status = $"Read metadata for '{key}'.";
            }
            else
            {
                m_metadata = null;
                if (updateStatus)
                    m_status = $"Metadata read failed: {error}";
            }
            RefreshThumbnail(storage, key, updateStatus);
        }

        private void RefreshThumbnail(
            TileSnapshotStorage storage,
            TileSnapshotStorage.Key key,
            bool updateStatus)
        {
            ClearThumbnail();
            if (!storage.TryGetThumbnail(key, out m_thumbnail, out string error))
            {
                if (updateStatus)
                    m_status = $"Thumbnail read failed: {error}";
                else
                    Debug.LogWarning($"Could not load tile snapshot thumbnail '{key}': {error}");
            }
        }

        private void DrawThumbnail()
        {
            if (m_thumbnail == null)
            {
                EditorGUILayout.HelpBox("No thumbnail is stored for this snapshot.", MessageType.None);
                return;
            }

            EditorGUILayout.LabelField($"{m_thumbnail.width} x {m_thumbnail.height}");
            float availableWidth = Mathf.Max(1, EditorGUIUtility.currentViewWidth - 40);
            float aspect = m_thumbnail.width / (float)m_thumbnail.height;
            float height = Mathf.Min(256, availableWidth / aspect);
            float width = height * aspect;
            Rect row = GUILayoutUtility.GetRect(availableWidth, height);
            Rect preview = new Rect(row.x + (row.width - width) * 0.5f, row.y, width, height);
            EditorGUI.DrawPreviewTexture(preview, m_thumbnail, null, ScaleMode.ScaleToFit);
        }

        private static void DrawBiomeData(BiomeData data)
        {
            if (data == null)
            {
                EditorGUILayout.HelpBox("No data.", MessageType.None);
                return;
            }

            DrawTexture("Height", data.heightMap);
            DrawTexture("Holes", data.holeMap);
            DrawTexture("Mesh Density", data.meshDensityMap);
            DrawTexture("Albedo", data.albedoMap);
            DrawTexture("Metallic", data.metallicMap);

            List<TerrainLayer> layers = new List<TerrainLayer>();
            List<RenderTexture> weights = new List<RenderTexture>();
            data.GetLayerWeights(layers, weights);
            EditorGUILayout.LabelField($"Terrain Layers ({layers.Count})", EditorStyles.boldLabel);
            for (int i = 0; i < layers.Count; ++i)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.ObjectField($"Layer {i}", layers[i], typeof(TerrainLayer), false);
                    DrawTexture("Weight", weights[i]);
                }
            }

            List<TreeTemplate> treeTemplates = new List<TreeTemplate>();
            List<ComputeBuffer> treeBuffers = new List<ComputeBuffer>();
            data.GetTrees(treeTemplates, treeBuffers);
            EditorGUILayout.LabelField($"Trees ({treeTemplates.Count})", EditorStyles.boldLabel);
            for (int i = 0; i < treeTemplates.Count; ++i)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.ObjectField($"Template {i}", treeTemplates[i], typeof(TreeTemplate), false);
                    ComputeBuffer buffer = treeBuffers[i];
                    EditorGUILayout.LabelField(
                        "Instance Buffer",
                        buffer != null ? $"{buffer.count} elements, {buffer.stride}-byte stride" : "Missing");
                }
            }

            List<DetailTemplate> detailDensityTemplates = new List<DetailTemplate>();
            List<RenderTexture> detailDensityMaps = new List<RenderTexture>();
            data.GetDensityMaps(detailDensityTemplates, detailDensityMaps);
            EditorGUILayout.LabelField($"Detail Densities ({detailDensityTemplates.Count})", EditorStyles.boldLabel);
            for (int i = 0; i < detailDensityTemplates.Count; ++i)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.ObjectField($"Template {i}", detailDensityTemplates[i], typeof(DetailTemplate), false);
                    DrawTexture("Density", detailDensityMaps[i]);
                }
            }

            List<DetailTemplate> detailInstanceTemplates = new List<DetailTemplate>();
            List<ComputeBuffer> detailInstanceBuffers = new List<ComputeBuffer>();
            data.GetDetailInstances(detailInstanceTemplates, detailInstanceBuffers);
            EditorGUILayout.LabelField($"Detail Instances ({detailInstanceTemplates.Count})", EditorStyles.boldLabel);
            for (int i = 0; i < detailInstanceTemplates.Count; ++i)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.ObjectField($"Template {i}", detailInstanceTemplates[i], typeof(DetailTemplate), false);
                    ComputeBuffer buffer = detailInstanceBuffers[i];
                    EditorGUILayout.LabelField(
                        "Instance Buffer",
                        buffer != null ? $"{buffer.count} elements, {buffer.stride}-byte stride" : "Missing");
                }
            }
        }

        private static void DrawTexture(string label, RenderTexture texture)
        {
            EditorGUILayout.ObjectField(label, texture, typeof(RenderTexture), false);
            if (texture != null)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.LabelField(
                        "Descriptor",
                        $"{texture.width} x {texture.height}, {texture.graphicsFormat}");
                }
            }
        }

        private static void DrawMetadata(TileSnapshotStorage.Metadata metadata)
        {
            if (metadata == null)
            {
                EditorGUILayout.HelpBox("No metadata loaded.", MessageType.None);
                return;
            }

            EditorGUILayout.LabelField("Key", metadata.key.ToString());
            EditorGUILayout.IntField("Format Version", metadata.formatVersion);
            DateTime capturedUtc = new DateTime(metadata.capturedUtcTicks, DateTimeKind.Utc);
            EditorGUILayout.TextField("Captured UTC", capturedUtc.ToString("O"));

            EditorGUILayout.LabelField($"Layers ({metadata.layers.Count})", EditorStyles.boldLabel);
            for (int i = 0; i < metadata.layers.Count; ++i)
            {
                TileSnapshotStorage.LayerMetadata layer = metadata.layers[i];
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.LabelField($"Layer {i}", layer?.terrainLayer?.name ?? "Invalid");
                    if (layer?.terrainLayer != null)
                    {
                        EditorGUILayout.TextField("Asset GUID", layer.terrainLayer.asset.guid ?? string.Empty);
                        EditorGUILayout.LongField("Local File ID", layer.terrainLayer.asset.localFileId);
                        EditorGUILayout.TextField("Weight Blob ID", layer.weightBlobId ?? string.Empty);
                    }
                }
            }

            EditorGUILayout.LabelField($"Trees ({metadata.trees.Count})", EditorStyles.boldLabel);
            for (int i = 0; i < metadata.trees.Count; ++i)
            {
                TileSnapshotStorage.TreeMetadata tree = metadata.trees[i];
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.LabelField($"Tree {i}", tree?.template?.name ?? "Invalid");
                    if (tree?.template != null)
                    {
                        EditorGUILayout.TextField("Asset GUID", tree.template.asset.guid ?? string.Empty);
                        EditorGUILayout.LongField("Local File ID", tree.template.asset.localFileId);
                        EditorGUILayout.TextField("Prefab GUID", tree.template.prefab.guid ?? string.Empty);
                        EditorGUILayout.TextField("Instance Blob ID", tree.instanceBlobId ?? string.Empty);
                    }
                }
            }

            EditorGUILayout.LabelField($"Detail Densities ({metadata.detailDensities.Count})", EditorStyles.boldLabel);
            for (int i = 0; i < metadata.detailDensities.Count; ++i)
            {
                TileSnapshotStorage.DetailDensityMetadata detail = metadata.detailDensities[i];
                DrawDetailMetadata($"Detail Density {i}", detail?.template, detail?.densityBlobId);
            }

            EditorGUILayout.LabelField($"Detail Instances ({metadata.detailInstances.Count})", EditorStyles.boldLabel);
            for (int i = 0; i < metadata.detailInstances.Count; ++i)
            {
                TileSnapshotStorage.DetailInstanceMetadata detail = metadata.detailInstances[i];
                DrawDetailMetadata($"Detail Instance {i}", detail?.template, detail?.instanceBlobId);
            }

            EditorGUILayout.LabelField($"Blobs ({metadata.blobs.Count})", EditorStyles.boldLabel);
            for (int i = 0; i < metadata.blobs.Count; ++i)
            {
                TileSnapshotStorage.BlobMetadata blob = metadata.blobs[i];
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.LabelField($"{i}: {blob.id}", blob.kind.ToString());
                    EditorGUILayout.TextField("File", blob.fileName);
                    if (blob.kind == TileSnapshotStorage.BlobKind.TreeInstances ||
                        blob.kind == TileSnapshotStorage.BlobKind.DetailInstances)
                    {
                        EditorGUILayout.LabelField("Buffer", $"{blob.elementCount} elements, {blob.stride}-byte stride");
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Texture", $"{blob.width} x {blob.height}, {(UnityEngine.Experimental.Rendering.GraphicsFormat)blob.graphicsFormat}");
                        EditorGUILayout.LabelField("Filter Mode", blob.filterMode.ToString());
                        EditorGUILayout.LabelField("Wrap Mode", blob.wrapMode.ToString());
                        EditorGUILayout.Toggle("Use Mip Map", blob.useMipMap);
                        EditorGUILayout.Toggle("Auto Generate Mips", blob.autoGenerateMips);
                        EditorGUILayout.Toggle("Random Write", blob.enableRandomWrite);
                    }
                    EditorGUILayout.LabelField("Compression", blob.compression.ToString());
                    EditorGUILayout.LongField("Raw Bytes", blob.uncompressedLength);
                    EditorGUILayout.TextField("Checksum", blob.checksum);
                }
            }
        }

        private static void DrawDetailMetadata(
            string label,
            TileSnapshotStorage.DetailTemplateMetadata template,
            string blobId)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.LabelField(label, template?.name ?? "Invalid");
                if (template == null)
                    return;
                EditorGUILayout.TextField("Asset GUID", template.asset.guid ?? string.Empty);
                EditorGUILayout.LongField("Local File ID", template.asset.localFileId);
                EditorGUILayout.EnumPopup("Render Mode", template.renderMode);
                EditorGUILayout.TextField("Texture GUID", template.texture.guid ?? string.Empty);
                EditorGUILayout.TextField("Prefab GUID", template.prefab.guid ?? string.Empty);
                EditorGUILayout.TextField("Blob ID", blobId ?? string.Empty);
            }
        }

        private static TileSnapshotStorage.Key CreateKey(ITileSnapshotProvider provider)
        {
            return new TileSnapshotStorage.Key(
                provider.managerId,
                RectXZ.FromBounds(provider.worldBounds));
        }

        private static MonoBehaviour FindProviderComponent(GameObject selected)
        {
            if (selected == null || !selected.scene.IsValid())
                return null;
            MonoBehaviour[] components = selected.GetComponentsInParent<MonoBehaviour>(true);
            for (int i = 0; i < components.Length; ++i)
            {
                if (components[i] is ITileSnapshotProvider)
                    return components[i];
            }
            return null;
        }

        private void ClearCapturedData()
        {
            m_capturedData?.Dispose();
            m_capturedData = null;
            DisposeCaptureResources();
            m_hasCaptureKey = false;
        }

        private void DisposeCaptureResources()
        {
            m_captureResources?.Dispose();
            m_captureResources = null;
        }

        private void ClearLoadedData()
        {
            m_loadedData?.Dispose();
            m_loadedData = null;
            m_loadedResources?.Dispose();
            m_loadedResources = null;
        }

        private void ClearThumbnail()
        {
            if (m_thumbnail != null)
                DestroyImmediate(m_thumbnail);
            m_thumbnail = null;
        }
    }
}
#endif
