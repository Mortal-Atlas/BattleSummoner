#if VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Pinwheel.Vista.Graph;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Pinwheel.Vista
{
    /// <summary>
    /// Shared filesystem implementation for tile snapshots. Platform implementations supply the root directory and
    /// asset identity resolution.
    /// </summary>
    public abstract class TileSnapshotStorage
    {
        private const string METADATA_FILE_NAME = "metadata.json";
        private const string THUMBNAIL_FILE_NAME = ".thumbnail.jpg";
        private const string BLOB_EXTENSION = ".blob";
        private const int THUMBNAIL_JPG_QUALITY = 85;
        private const int FORMAT_VERSION = 2;
        private const int MAX_BLOB_UNCOMPRESSED_BYTES = 512 * 1024 * 1024;

        [Serializable]
        public struct Key : IEquatable<Key>
        {
            [SerializeField]
            private string m_vistaManagerId;
            public string vistaManagerId => m_vistaManagerId;

            [SerializeField]
            private RectXZ m_region;
            public RectXZ region => m_region;

            public Key(string vistaManagerId, RectXZ region)
            {
                if (string.IsNullOrWhiteSpace(vistaManagerId))
                    throw new ArgumentException("Vista Manager ID cannot be null or empty.", nameof(vistaManagerId));
                m_vistaManagerId = vistaManagerId;
                m_region = region;
            }

            public bool Equals(Key other)
            {
                return string.Equals(m_vistaManagerId, other.m_vistaManagerId, StringComparison.Ordinal) &&
                    m_region.Equals(other.m_region);
            }

            public override bool Equals(object obj)
            {
                return obj is Key other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((m_vistaManagerId != null ? m_vistaManagerId.GetHashCode() : 0) * 397) ^ m_region.GetHashCode();
                }
            }

            public override string ToString()
            {
                return $"{m_vistaManagerId}:{m_region}";
            }
        }

        internal enum BlobKind
        {
            Height,
            Holes,
            MeshDensity,
            Albedo,
            Metallic,
            LayerWeight,
            TreeInstances,
            DetailDensity,
            DetailInstances
        }

        internal enum BlobCompression
        {
            None,
            GZip
        }

        [Serializable]
        internal struct AssetIdentity
        {
            public string guid;
            public long localFileId;

            public bool isEmpty => string.IsNullOrEmpty(guid);
        }

        [Serializable]
        internal sealed class TerrainLayerMetadata
        {
            public AssetIdentity asset;
            public string name;
            public AssetIdentity diffuseTexture;
            public AssetIdentity normalMapTexture;
            public AssetIdentity maskMapTexture;
            public Vector2 tileSize;
            public Vector2 tileOffset;
            public Color specular;
            public float metallic;
            public float smoothness;
            public float normalScale;
            public Vector4 diffuseRemapMin;
            public Vector4 diffuseRemapMax;
            public Vector4 maskMapRemapMin;
            public Vector4 maskMapRemapMax;

            public TerrainLayerMetadata Clone()
            {
                return (TerrainLayerMetadata)MemberwiseClone();
            }
        }

        [Serializable]
        internal sealed class LayerMetadata
        {
            public TerrainLayerMetadata terrainLayer;
            public string weightBlobId;

            public LayerMetadata Clone()
            {
                LayerMetadata clone = (LayerMetadata)MemberwiseClone();
                clone.terrainLayer = terrainLayer?.Clone();
                return clone;
            }
        }

        [Serializable]
        internal sealed class TreeTemplateMetadata
        {
            public AssetIdentity asset;
            public string name;
            public AssetIdentity prefab;
            public int navMeshLod;
            public float bendFactor;
            public AssetIdentity billboard;
            public ShadowCastingMode shadowCastingMode;
            public bool receiveShadow;
            public ShadowCastingMode billboardShadowCastingMode;
            public bool billboardReceiveShadow;
            public int layer;
            public bool keepPrefabLayer;
            public float pivotOffset;
            public Quaternion baseRotation;
            public Vector3 baseScale;

            public TreeTemplateMetadata Clone()
            {
                return (TreeTemplateMetadata)MemberwiseClone();
            }
        }

        [Serializable]
        internal sealed class TreeMetadata
        {
            public TreeTemplateMetadata template;
            public string instanceBlobId;

            public TreeMetadata Clone()
            {
                TreeMetadata clone = (TreeMetadata)MemberwiseClone();
                clone.template = template?.Clone();
                return clone;
            }
        }

        [Serializable]
        internal sealed class DetailTemplateMetadata
        {
            public AssetIdentity asset;
            public string name;
            public DetailRenderMode renderMode;
            public AssetIdentity texture;
            public AssetIdentity prefab;
            public Color primaryColor;
            public Color secondaryColor;
            public float minHeight;
            public float maxHeight;
            public float minWidth;
            public float maxWidth;
            public float noiseSpread;
            public float holeEdgePadding;
#if UNITY_2021_2_OR_NEWER
            public bool useInstancing;
#endif
            public float pivotOffset;
            public float bendFactor;
            public int layer;
            public bool alignToSurface;
            public ShadowCastingMode castShadow;
            public bool receiveShadow;
            public int density;
            public DetailTemplate.TextureBasedGrassShape textureBasedGrassShape;

            public DetailTemplateMetadata Clone()
            {
                return (DetailTemplateMetadata)MemberwiseClone();
            }
        }

        [Serializable]
        internal sealed class DetailDensityMetadata
        {
            public DetailTemplateMetadata template;
            public string densityBlobId;

            public DetailDensityMetadata Clone()
            {
                DetailDensityMetadata clone = (DetailDensityMetadata)MemberwiseClone();
                clone.template = template?.Clone();
                return clone;
            }
        }

        [Serializable]
        internal sealed class DetailInstanceMetadata
        {
            public DetailTemplateMetadata template;
            public string instanceBlobId;

            public DetailInstanceMetadata Clone()
            {
                DetailInstanceMetadata clone = (DetailInstanceMetadata)MemberwiseClone();
                clone.template = template?.Clone();
                return clone;
            }
        }

        [Serializable]
        internal sealed class BlobMetadata
        {
            public string id;
            public BlobKind kind;
            public string fileName;
            public int width;
            public int height;
            public int graphicsFormat;
            public FilterMode filterMode;
            public TextureWrapMode wrapMode;
            public bool useMipMap;
            public bool autoGenerateMips;
            public bool enableRandomWrite;
            public int elementCount;
            public int stride;
            public int uncompressedLength;
            public string checksum;
            public BlobCompression compression;

            public BlobMetadata Clone()
            {
                return (BlobMetadata)MemberwiseClone();
            }
        }

        [Serializable]
        internal sealed class Metadata
        {
            public Key key;
            public int formatVersion = FORMAT_VERSION;
            public long capturedUtcTicks;
            public List<LayerMetadata> layers = new List<LayerMetadata>();
            public List<TreeMetadata> trees = new List<TreeMetadata>();
            public List<DetailDensityMetadata> detailDensities = new List<DetailDensityMetadata>();
            public List<DetailInstanceMetadata> detailInstances = new List<DetailInstanceMetadata>();
            public List<BlobMetadata> blobs = new List<BlobMetadata>();

            public Metadata Clone()
            {
                Metadata clone = (Metadata)MemberwiseClone();
                clone.layers = new List<LayerMetadata>(layers != null ? layers.Count : 0);
                if (layers != null)
                {
                    for (int i = 0; i < layers.Count; ++i)
                        clone.layers.Add(layers[i]?.Clone());
                }
                clone.trees = new List<TreeMetadata>(trees != null ? trees.Count : 0);
                if (trees != null)
                {
                    for (int i = 0; i < trees.Count; ++i)
                        clone.trees.Add(trees[i]?.Clone());
                }
                clone.detailDensities = new List<DetailDensityMetadata>(detailDensities != null ? detailDensities.Count : 0);
                if (detailDensities != null)
                {
                    for (int i = 0; i < detailDensities.Count; ++i)
                        clone.detailDensities.Add(detailDensities[i]?.Clone());
                }
                clone.detailInstances = new List<DetailInstanceMetadata>(detailInstances != null ? detailInstances.Count : 0);
                if (detailInstances != null)
                {
                    for (int i = 0; i < detailInstances.Count; ++i)
                        clone.detailInstances.Add(detailInstances[i]?.Clone());
                }
                clone.blobs = new List<BlobMetadata>(blobs.Count);
                for (int i = 0; i < blobs.Count; ++i)
                    clone.blobs.Add(blobs[i].Clone());
                return clone;
            }
        }

        private protected abstract string rootDirectory { get; }
        private protected abstract AssetIdentity GetAssetIdentity(UnityEngine.Object asset);
        private protected abstract T ResolveAsset<T>(AssetIdentity identity) where T : UnityEngine.Object;

        internal bool TryGetMetadata(Key key, out Metadata metadata, out string error)
        {
            metadata = null;
            error = null;
            try
            {
                metadata = LoadMetadataOrThrow(key).Clone();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool TryStore(Key key, BiomeData data, out string error)
        {
            error = null;
            if (data == null)
            {
                error = "Biome data cannot be null.";
                return false;
            }

            // Build the replacement in staging, preserve the current capture for rollback,
            // then promote staging only after every blob and the metadata file are complete.
            string activeDirectory = null;
            string stagingDirectory = null;
            string rollbackDirectory = null;
            try
            {
                activeDirectory = GetSnapshotDirectory(key);
                string parentDirectory = Path.GetDirectoryName(activeDirectory);
                Directory.CreateDirectory(parentDirectory);
                stagingDirectory = activeDirectory + ".staging-" + Guid.NewGuid().ToString("N");
                Directory.CreateDirectory(stagingDirectory);

                Metadata metadata = new Metadata
                {
                    key = key,
                    formatVersion = FORMAT_VERSION,
                    capturedUtcTicks = DateTime.UtcNow.Ticks
                };

                AddTexture(metadata, stagingDirectory, BlobKind.Height, data.heightMap);
                AddTexture(metadata, stagingDirectory, BlobKind.Holes, data.holeMap);
                AddTexture(metadata, stagingDirectory, BlobKind.MeshDensity, data.meshDensityMap);
                AddTexture(metadata, stagingDirectory, BlobKind.Albedo, data.albedoMap);
                AddTexture(metadata, stagingDirectory, BlobKind.Metallic, data.metallicMap);

                List<TerrainLayer> terrainLayers = new List<TerrainLayer>();
                List<RenderTexture> layerWeights = new List<RenderTexture>();
                data.GetLayerWeights(terrainLayers, layerWeights);
                EnsurePairCount(terrainLayers.Count, layerWeights.Count, "terrain layers", "layer weights");
                for (int i = 0; i < terrainLayers.Count; ++i)
                {
                    if (terrainLayers[i] == null)
                        throw new InvalidOperationException($"Terrain layer {i} is null.");
                    if (layerWeights[i] == null)
                        throw new InvalidOperationException($"Terrain layer weight {i} is null.");

                    string blobId = $"layer-weight-{i}";
                    AddTexture(
                        metadata,
                        stagingDirectory,
                        BlobKind.LayerWeight,
                        layerWeights[i],
                        blobId);
                    metadata.layers.Add(new LayerMetadata
                    {
                        terrainLayer = CaptureTerrainLayer(terrainLayers[i]),
                        weightBlobId = blobId
                    });
                }

                List<TreeTemplate> treeTemplates = new List<TreeTemplate>();
                List<ComputeBuffer> treeBuffers = new List<ComputeBuffer>();
                data.GetTrees(treeTemplates, treeBuffers);
                EnsurePairCount(treeTemplates.Count, treeBuffers.Count, "tree templates", "tree buffers");
                for (int i = 0; i < treeTemplates.Count; ++i)
                {
                    if (treeTemplates[i] == null)
                        throw new InvalidOperationException($"Tree template {i} is null.");
                    if (treeBuffers[i] == null)
                        throw new InvalidOperationException($"Tree instance buffer {i} is null.");

                    string blobId = $"tree-instances-{i}";
                    AddBuffer(
                        metadata,
                        stagingDirectory,
                        BlobKind.TreeInstances,
                        treeBuffers[i],
                        blobId);
                    metadata.trees.Add(new TreeMetadata
                    {
                        template = CaptureTreeTemplate(treeTemplates[i]),
                        instanceBlobId = blobId
                    });
                }

                List<DetailTemplate> detailDensityTemplates = new List<DetailTemplate>();
                List<RenderTexture> detailDensityMaps = new List<RenderTexture>();
                data.GetDensityMaps(detailDensityTemplates, detailDensityMaps);
                EnsurePairCount(
                    detailDensityTemplates.Count,
                    detailDensityMaps.Count,
                    "detail density templates",
                    "detail density maps");
                for (int i = 0; i < detailDensityTemplates.Count; ++i)
                {
                    if (detailDensityTemplates[i] == null)
                        throw new InvalidOperationException($"Detail density template {i} is null.");
                    if (detailDensityMaps[i] == null)
                        throw new InvalidOperationException($"Detail density map {i} is null.");

                    string blobId = $"detail-density-{i}";
                    AddTexture(metadata, stagingDirectory, BlobKind.DetailDensity, detailDensityMaps[i], blobId);
                    metadata.detailDensities.Add(new DetailDensityMetadata
                    {
                        template = CaptureDetailTemplate(detailDensityTemplates[i]),
                        densityBlobId = blobId
                    });
                }

                List<DetailTemplate> detailInstanceTemplates = new List<DetailTemplate>();
                List<ComputeBuffer> detailInstanceBuffers = new List<ComputeBuffer>();
                data.GetDetailInstances(detailInstanceTemplates, detailInstanceBuffers);
                EnsurePairCount(
                    detailInstanceTemplates.Count,
                    detailInstanceBuffers.Count,
                    "detail instance templates",
                    "detail instance buffers");
                for (int i = 0; i < detailInstanceTemplates.Count; ++i)
                {
                    if (detailInstanceTemplates[i] == null)
                        throw new InvalidOperationException($"Detail instance template {i} is null.");
                    if (detailInstanceBuffers[i] == null)
                        throw new InvalidOperationException($"Detail instance buffer {i} is null.");

                    string blobId = $"detail-instances-{i}";
                    AddBuffer(metadata, stagingDirectory, BlobKind.DetailInstances, detailInstanceBuffers[i], blobId);
                    metadata.detailInstances.Add(new DetailInstanceMetadata
                    {
                        template = CaptureDetailTemplate(detailInstanceTemplates[i]),
                        instanceBlobId = blobId
                    });
                }

                if (metadata.blobs.Count == 0)
                    throw new InvalidOperationException("Biome data does not contain any supported channel.");

                WriteMetadata(stagingDirectory, metadata);

                if (Directory.Exists(activeDirectory))
                {
                    rollbackDirectory = activeDirectory + ".rollback-" + Guid.NewGuid().ToString("N");
                    Directory.Move(activeDirectory, rollbackDirectory);
                }
                Directory.Move(stagingDirectory, activeDirectory);
                stagingDirectory = null;

                if (!string.IsNullOrEmpty(rollbackDirectory))
                {
                    TryDeleteDirectory(rollbackDirectory);
                    rollbackDirectory = null;
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                try
                {
                    if (!string.IsNullOrEmpty(rollbackDirectory) && Directory.Exists(rollbackDirectory) &&
                        !string.IsNullOrEmpty(activeDirectory) && !Directory.Exists(activeDirectory))
                    {
                        Directory.Move(rollbackDirectory, activeDirectory);
                        rollbackDirectory = null;
                    }
                }
                catch (Exception rollbackException)
                {
                    error += $" Rollback failed: {rollbackException.Message}";
                }
                return false;
            }
            finally
            {
                TryDeleteDirectory(stagingDirectory);
            }
        }

        /// <summary>
        /// Loads a snapshot into a newly created <see cref="BiomeData"/> instance.
        /// </summary>
        /// <param name="key">The snapshot to load.</param>
        /// <param name="transientResources">
        /// Caller-owned lifetime scope for reconstructed terrain layers and content templates. The caller must keep
        /// this registry alive while any consumer can reference the loaded snapshot's reconstructed objects.
        /// </param>
        /// <param name="data">
        /// The loaded data on success; otherwise, null. Pass an uninitialized or null variable because any
        /// existing reference is overwritten and is not disposed by this method. The caller owns and must
        /// dispose the returned data.
        /// </param>
        /// <param name="error">The failure message, or null on success.</param>
        public bool TryLoad(
            Key key,
            TransientResourceRegistry transientResources,
            out BiomeData data,
            out string error)
        {
            data = null;
            error = null;
            if (transientResources == null)
            {
                error = "Transient resource registry cannot be null.";
                return false;
            }
            try
            {
                string snapshotDirectory = GetSnapshotDirectory(key);
                Metadata metadata = LoadMetadataOrThrow(key);
                BiomeData loaded = new BiomeData();
                List<UnityEngine.Object> reconstructedObjects = new List<UnityEngine.Object>();
                try
                {
                    Dictionary<string, BlobMetadata> blobsById = BuildBlobLookup(metadata);
                    HashSet<BlobKind> restoredDirectChannels = new HashSet<BlobKind>();
                    for (int i = 0; i < metadata.blobs.Count; ++i)
                    {
                        BlobMetadata blob = metadata.blobs[i];
                        if (blob.kind == BlobKind.LayerWeight ||
                            blob.kind == BlobKind.TreeInstances ||
                            blob.kind == BlobKind.DetailDensity ||
                            blob.kind == BlobKind.DetailInstances)
                            continue;
                        if (!restoredDirectChannels.Add(blob.kind))
                            throw new InvalidDataException($"Snapshot contains duplicate '{blob.kind}' blobs.");
                        RenderTexture texture = RestoreTexture(blob, ReadBlob(snapshotDirectory, blob));
                        switch (blob.kind)
                        {
                            case BlobKind.Height:
                                loaded.heightMap = texture;
                                break;
                            case BlobKind.Holes:
                                loaded.holeMap = texture;
                                break;
                            case BlobKind.MeshDensity:
                                loaded.meshDensityMap = texture;
                                break;
                            case BlobKind.Albedo:
                                loaded.albedoMap = texture;
                                break;
                            case BlobKind.Metallic:
                                loaded.metallicMap = texture;
                                break;
                            default:
                                ReleaseTexture(texture);
                                throw new InvalidDataException($"Unsupported snapshot blob kind '{blob.kind}'.");
                        }
                    }

                    for (int i = 0; i < metadata.layers.Count; ++i)
                    {
                        LayerMetadata layerMetadata = metadata.layers[i];
                        if (layerMetadata == null || layerMetadata.terrainLayer == null)
                            throw new InvalidDataException($"Terrain layer metadata {i} is invalid.");
                        BlobMetadata weightBlob = GetReferencedBlob(
                            blobsById,
                            layerMetadata.weightBlobId,
                            BlobKind.LayerWeight,
                            $"Terrain layer {i}");

                        TerrainLayer terrainLayer = RestoreTerrainLayer(layerMetadata.terrainLayer, out bool isTransient);
                        if (isTransient)
                            RegisterReconstructedObject(transientResources, reconstructedObjects, terrainLayer);
                        RenderTexture weight = RestoreTexture(
                            weightBlob,
                            ReadBlob(snapshotDirectory, weightBlob));
                        loaded.AddTextureLayer(terrainLayer, weight);
                    }

                    for (int i = 0; i < metadata.trees.Count; ++i)
                    {
                        TreeMetadata treeMetadata = metadata.trees[i];
                        if (treeMetadata == null || treeMetadata.template == null)
                            throw new InvalidDataException($"Tree metadata {i} is invalid.");
                        BlobMetadata instanceBlob = GetReferencedBlob(
                            blobsById,
                            treeMetadata.instanceBlobId,
                            BlobKind.TreeInstances,
                            $"Tree {i}");

                        TreeTemplate treeTemplate = RestoreTreeTemplate(treeMetadata.template, out bool isTransient);
                        if (isTransient)
                            RegisterReconstructedObject(transientResources, reconstructedObjects, treeTemplate);
                        ComputeBuffer instances = RestoreBuffer(
                            instanceBlob,
                            ReadBlob(snapshotDirectory, instanceBlob));
                        loaded.AddTree(treeTemplate, instances);
                    }

                    for (int i = 0; i < metadata.detailDensities.Count; ++i)
                    {
                        DetailDensityMetadata detailMetadata = metadata.detailDensities[i];
                        if (detailMetadata == null || detailMetadata.template == null)
                            throw new InvalidDataException($"Detail density metadata {i} is invalid.");
                        BlobMetadata densityBlob = GetReferencedBlob(
                            blobsById,
                            detailMetadata.densityBlobId,
                            BlobKind.DetailDensity,
                            $"Detail density {i}");

                        DetailTemplate detailTemplate = RestoreDetailTemplate(detailMetadata.template, out bool isTransient);
                        if (isTransient)
                            RegisterReconstructedObject(transientResources, reconstructedObjects, detailTemplate);
                        RenderTexture densityMap = RestoreTexture(
                            densityBlob,
                            ReadBlob(snapshotDirectory, densityBlob));
                        loaded.AddDetailDensity(detailTemplate, densityMap);
                    }

                    for (int i = 0; i < metadata.detailInstances.Count; ++i)
                    {
                        DetailInstanceMetadata detailMetadata = metadata.detailInstances[i];
                        if (detailMetadata == null || detailMetadata.template == null)
                            throw new InvalidDataException($"Detail instance metadata {i} is invalid.");
                        BlobMetadata instanceBlob = GetReferencedBlob(
                            blobsById,
                            detailMetadata.instanceBlobId,
                            BlobKind.DetailInstances,
                            $"Detail instance {i}");

                        DetailTemplate detailTemplate = RestoreDetailTemplate(detailMetadata.template, out bool isTransient);
                        if (isTransient)
                            RegisterReconstructedObject(transientResources, reconstructedObjects, detailTemplate);
                        ComputeBuffer instances = RestoreBuffer(
                            instanceBlob,
                            ReadBlob(snapshotDirectory, instanceBlob));
                        loaded.AddDetailInstance(detailTemplate, instances);
                    }
                    data = loaded;
                    return true;
                }
                catch
                {
                    loaded.Dispose();
                    ReleaseReconstructedObjects(transientResources, reconstructedObjects);
                    throw;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool TryDelete(Key key, out string error)
        {
            error = null;
            try
            {
                string directory = GetSnapshotDirectory(key);
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool TrySetThumbnail(Key key, Texture2D thumbnail, out string error)
        {
            error = null;
            if (thumbnail == null)
            {
                error = "Thumbnail cannot be null.";
                return false;
            }
            try
            {
                string directory = GetSnapshotDirectory(key);
                Directory.CreateDirectory(directory);
                byte[] jpg = ImageConversion.EncodeToJPG(thumbnail, THUMBNAIL_JPG_QUALITY);
                if (jpg == null || jpg.Length == 0)
                    throw new InvalidOperationException("Unity could not encode the thumbnail as JPG.");
                File.WriteAllBytes(Path.Combine(directory, THUMBNAIL_FILE_NAME), jpg);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool TryGetThumbnail(Key key, out Texture2D thumbnail, out string error)
        {
            thumbnail = null;
            error = null;
            try
            {
                string path = Path.Combine(GetSnapshotDirectory(key), THUMBNAIL_FILE_NAME);
                if (!File.Exists(path))
                    return true;

                Texture2D loaded = new Texture2D(2, 2, TextureFormat.RGB24, false);
                if (!ImageConversion.LoadImage(loaded, File.ReadAllBytes(path), true))
                {
                    DestroyTexture(loaded);
                    throw new InvalidDataException($"Thumbnail '{path}' is not a valid JPG image.");
                }
                loaded.name = $"Tile Snapshot Thumbnail {key.region}";
                thumbnail = loaded;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool TryDeleteThumbnail(Key key, out string error)
        {
            error = null;
            try
            {
                string path = Path.Combine(GetSnapshotDirectory(key), THUMBNAIL_FILE_NAME);
                if (File.Exists(path))
                    File.Delete(path);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static void DestroyTexture(Texture2D texture)
        {
            if (texture == null)
                return;
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(texture);
            else
                UnityEngine.Object.DestroyImmediate(texture);
        }

        private BlobMetadata AddTexture(
            Metadata metadata,
            string directory,
            BlobKind kind,
            RenderTexture texture,
            string blobId = null)
        {
            if (texture == null)
                return null;
            if (!texture.IsCreated())
                throw new InvalidOperationException($"Snapshot texture '{kind}' has not been created.");
            if (!SystemInfo.IsFormatSupported(texture.graphicsFormat, FormatUsage.ReadPixels))
            {
                throw new InvalidOperationException(
                    $"Snapshot texture '{kind}' uses format '{texture.graphicsFormat}', which this platform " +
                    "does not support for synchronous pixel readback.");
            }
            if (texture.descriptor.useMipMap && !texture.descriptor.autoGenerateMips)
            {
                throw new NotSupportedException(
                    $"Snapshot texture '{kind}' has manually managed mip levels. The snapshot format stores only " +
                    "the base mip level.");
            }
            int expectedByteLength = GetTextureByteLength(
                texture.width,
                texture.height,
                texture.graphicsFormat,
                kind.ToString());

            Texture2D readableTexture = new Texture2D(
                texture.width,
                texture.height,
                texture.graphicsFormat,
                TextureCreationFlags.None);
            byte[] bytes;
            try
            {
                GraphicsUtils.ReadRenderTexture(texture, readableTexture, false);
                bytes = readableTexture.GetRawTextureData<byte>().ToArray();
                if (bytes.Length != expectedByteLength)
                {
                    throw new InvalidOperationException(
                        $"Snapshot texture '{kind}' produced {bytes.Length} bytes; expected {expectedByteLength}.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(readableTexture);
            }
            // A leading dot keeps raw blobs out of Unity's import pipeline and Project window.
            // The visible metadata file remains the authoritative map to each blob.
            if (string.IsNullOrEmpty(blobId))
                blobId = kind.ToString().ToLowerInvariant();
            string fileName = "." + blobId + BLOB_EXTENSION;
            WriteCompressedBlob(Path.Combine(directory, fileName), bytes);

            BlobMetadata blob = new BlobMetadata
            {
                id = blobId,
                kind = kind,
                fileName = fileName,
                width = texture.width,
                height = texture.height,
                graphicsFormat = (int)texture.graphicsFormat,
                filterMode = texture.filterMode,
                wrapMode = texture.wrapMode,
                useMipMap = texture.descriptor.useMipMap,
                autoGenerateMips = texture.descriptor.autoGenerateMips,
                enableRandomWrite = texture.enableRandomWrite,
                uncompressedLength = bytes.Length,
                checksum = ComputeChecksumHex(bytes),
                compression = BlobCompression.GZip
            };
            metadata.blobs.Add(blob);
            return blob;
        }

        private TerrainLayerMetadata CaptureTerrainLayer(TerrainLayer layer)
        {
            return new TerrainLayerMetadata
            {
                asset = GetAssetIdentity(layer),
                name = layer.name,
                diffuseTexture = GetAssetIdentity(layer.diffuseTexture),
                normalMapTexture = GetAssetIdentity(layer.normalMapTexture),
                maskMapTexture = GetAssetIdentity(layer.maskMapTexture),
                tileSize = layer.tileSize,
                tileOffset = layer.tileOffset,
                specular = layer.specular,
                metallic = layer.metallic,
                smoothness = layer.smoothness,
                normalScale = layer.normalScale,
                diffuseRemapMin = layer.diffuseRemapMin,
                diffuseRemapMax = layer.diffuseRemapMax,
                maskMapRemapMin = layer.maskMapRemapMin,
                maskMapRemapMax = layer.maskMapRemapMax
            };
        }

        private TreeTemplateMetadata CaptureTreeTemplate(TreeTemplate template)
        {
            return new TreeTemplateMetadata
            {
                asset = GetAssetIdentity(template),
                name = template.name,
                prefab = GetAssetIdentity(template.prefab),
                navMeshLod = template.navMeshLod,
                bendFactor = template.bendFactor,
                billboard = GetAssetIdentity(template.billboard),
                shadowCastingMode = template.shadowCastingMode,
                receiveShadow = template.receiveShadow,
                billboardShadowCastingMode = template.billboardShadowCastingMode,
                billboardReceiveShadow = template.billboardReceiveShadow,
                layer = template.layer,
                keepPrefabLayer = template.keepPrefabLayer,
                pivotOffset = template.pivotOffset,
                baseRotation = template.baseRotation,
                baseScale = template.baseScale
            };
        }

        private DetailTemplateMetadata CaptureDetailTemplate(DetailTemplate template)
        {
            return new DetailTemplateMetadata
            {
                asset = GetAssetIdentity(template),
                name = template.name,
                renderMode = template.renderMode,
                texture = GetAssetIdentity(template.texture),
                prefab = GetAssetIdentity(template.prefab),
                primaryColor = template.primaryColor,
                secondaryColor = template.secondaryColor,
                minHeight = template.minHeight,
                maxHeight = template.maxHeight,
                minWidth = template.minWidth,
                maxWidth = template.maxWidth,
                noiseSpread = template.noiseSpread,
                holeEdgePadding = template.holeEdgePadding,
#if UNITY_2021_2_OR_NEWER
                useInstancing = template.useInstancing,
#endif
                pivotOffset = template.pivotOffset,
                bendFactor = template.bendFactor,
                layer = template.layer,
                alignToSurface = template.alignToSurface,
                castShadow = template.castShadow,
                receiveShadow = template.receiveShadow,
                density = template.density,
                textureBasedGrassShape = template.textureBasedGrassShape
            };
        }

        private void AddBuffer(
            Metadata metadata,
            string directory,
            BlobKind kind,
            ComputeBuffer buffer,
            string blobId)
        {
            if (buffer.stride != sizeof(float))
            {
                throw new InvalidOperationException(
                    $"Snapshot buffer '{blobId}' has unsupported stride {buffer.stride}; expected {sizeof(float)}.");
            }
            if (buffer.count <= 0 || buffer.count % InstanceSample.SIZE != 0)
            {
                throw new InvalidOperationException(
                    $"Snapshot buffer '{blobId}' has {buffer.count} float elements; expected a positive multiple " +
                    $"of {InstanceSample.SIZE}.");
            }
            int expectedByteLength = GetBufferByteLength(buffer.count, buffer.stride, blobId);

            float[] values = new float[buffer.count];
            buffer.GetData(values);
            byte[] bytes = new byte[expectedByteLength];
            Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
            string fileName = "." + blobId + BLOB_EXTENSION;
            WriteCompressedBlob(Path.Combine(directory, fileName), bytes);

            metadata.blobs.Add(new BlobMetadata
            {
                id = blobId,
                kind = kind,
                fileName = fileName,
                elementCount = buffer.count,
                stride = buffer.stride,
                uncompressedLength = bytes.Length,
                checksum = ComputeChecksumHex(bytes),
                compression = BlobCompression.GZip
            });
        }

        private TerrainLayer RestoreTerrainLayer(TerrainLayerMetadata metadata, out bool isTransient)
        {
            TerrainLayer layer = ResolveAsset<TerrainLayer>(metadata.asset);
            if (layer != null)
            {
                isTransient = false;
                return layer;
            }

            layer = new TerrainLayer
            {
                name = metadata.name,
                diffuseTexture = ResolveAsset<Texture2D>(metadata.diffuseTexture),
                normalMapTexture = ResolveAsset<Texture2D>(metadata.normalMapTexture),
                maskMapTexture = ResolveAsset<Texture2D>(metadata.maskMapTexture),
                tileSize = metadata.tileSize,
                tileOffset = metadata.tileOffset,
                specular = metadata.specular,
                metallic = metadata.metallic,
                smoothness = metadata.smoothness,
                normalScale = metadata.normalScale,
                diffuseRemapMin = metadata.diffuseRemapMin,
                diffuseRemapMax = metadata.diffuseRemapMax,
                maskMapRemapMin = metadata.maskMapRemapMin,
                maskMapRemapMax = metadata.maskMapRemapMax
            };
            isTransient = true;
            return layer;
        }

        private TreeTemplate RestoreTreeTemplate(TreeTemplateMetadata metadata, out bool isTransient)
        {
            TreeTemplate template = ResolveAsset<TreeTemplate>(metadata.asset);
            if (template != null)
            {
                isTransient = false;
                return template;
            }

            template = ScriptableObject.CreateInstance<TreeTemplate>();
            template.name = metadata.name;
            template.prefab = ResolveAsset<GameObject>(metadata.prefab);
            template.navMeshLod = metadata.navMeshLod;
            template.bendFactor = metadata.bendFactor;
            template.billboard = ResolveAsset<BillboardAsset>(metadata.billboard);
            template.shadowCastingMode = metadata.shadowCastingMode;
            template.receiveShadow = metadata.receiveShadow;
            template.billboardShadowCastingMode = metadata.billboardShadowCastingMode;
            template.billboardReceiveShadow = metadata.billboardReceiveShadow;
            template.layer = metadata.layer;
            template.keepPrefabLayer = metadata.keepPrefabLayer;
            template.pivotOffset = metadata.pivotOffset;
            template.baseRotation = metadata.baseRotation;
            template.baseScale = metadata.baseScale;
            isTransient = true;
            return template;
        }

        private DetailTemplate RestoreDetailTemplate(DetailTemplateMetadata metadata, out bool isTransient)
        {
            DetailTemplate template = ResolveAsset<DetailTemplate>(metadata.asset);
            if (template != null)
            {
                isTransient = false;
                return template;
            }

            template = ScriptableObject.CreateInstance<DetailTemplate>();
            template.name = metadata.name;
            template.renderMode = metadata.renderMode;
            template.texture = ResolveAsset<Texture2D>(metadata.texture);
            template.prefab = ResolveAsset<GameObject>(metadata.prefab);
            template.primaryColor = metadata.primaryColor;
            template.secondaryColor = metadata.secondaryColor;
            template.minHeight = metadata.minHeight;
            template.maxHeight = metadata.maxHeight;
            template.minWidth = metadata.minWidth;
            template.maxWidth = metadata.maxWidth;
            template.noiseSpread = metadata.noiseSpread;
            template.holeEdgePadding = metadata.holeEdgePadding;
#if UNITY_2021_2_OR_NEWER
            template.useInstancing = metadata.useInstancing;
#endif
            template.pivotOffset = metadata.pivotOffset;
            template.bendFactor = metadata.bendFactor;
            template.layer = metadata.layer;
            template.alignToSurface = metadata.alignToSurface;
            template.castShadow = metadata.castShadow;
            template.receiveShadow = metadata.receiveShadow;
            template.density = metadata.density;
            template.textureBasedGrassShape = metadata.textureBasedGrassShape;
            isTransient = true;
            return template;
        }

        private static ComputeBuffer RestoreBuffer(BlobMetadata blob, byte[] bytes)
        {
            if (blob.elementCount <= 0 ||
                blob.elementCount % InstanceSample.SIZE != 0 ||
                blob.stride != sizeof(float))
            {
                throw new InvalidDataException(
                    $"Snapshot buffer '{blob.fileName}' has invalid count or stride.");
            }
            int expectedByteLength = GetBufferByteLength(blob.elementCount, blob.stride, blob.fileName);
            if (bytes.Length != expectedByteLength)
                throw new InvalidDataException($"Snapshot buffer '{blob.fileName}' has inconsistent metadata.");

            float[] values = new float[blob.elementCount];
            Buffer.BlockCopy(bytes, 0, values, 0, bytes.Length);
            ComputeBuffer result = new ComputeBuffer(blob.elementCount, blob.stride);
            try
            {
                result.SetData(values);
                return result;
            }
            catch
            {
                result.Release();
                throw;
            }
        }

        private static Dictionary<string, BlobMetadata> BuildBlobLookup(Metadata metadata)
        {
            Dictionary<string, BlobMetadata> result = new Dictionary<string, BlobMetadata>(StringComparer.Ordinal);
            for (int i = 0; i < metadata.blobs.Count; ++i)
            {
                BlobMetadata blob = metadata.blobs[i];
                if (blob == null)
                    throw new InvalidDataException($"Snapshot blob metadata {i} is null.");
                if (string.IsNullOrEmpty(blob.id))
                    throw new InvalidDataException($"Snapshot blob metadata {i} has no ID.");
                if (result.ContainsKey(blob.id))
                    throw new InvalidDataException($"Snapshot contains duplicate blob ID '{blob.id}'.");
                result.Add(blob.id, blob);
            }
            return result;
        }

        private static BlobMetadata GetReferencedBlob(
            Dictionary<string, BlobMetadata> blobsById,
            string blobId,
            BlobKind expectedKind,
            string owner)
        {
            if (!blobsById.TryGetValue(blobId, out BlobMetadata blob))
                throw new InvalidDataException($"{owner} references missing blob '{blobId}'.");
            if (blob.kind != expectedKind)
                throw new InvalidDataException($"{owner} references blob '{blobId}' of kind '{blob.kind}', expected '{expectedKind}'.");
            return blob;
        }

        private Metadata LoadMetadataOrThrow(Key expectedKey)
        {
            string directory = GetSnapshotDirectory(expectedKey);
            string path = Path.Combine(directory, METADATA_FILE_NAME);
            if (!File.Exists(path))
                throw new FileNotFoundException($"No tile snapshot metadata exists for '{expectedKey}'.", path);

            Metadata metadata = JsonUtility.FromJson<Metadata>(File.ReadAllText(path, Encoding.UTF8));
            if (metadata == null || metadata.blobs == null)
                throw new InvalidDataException($"Tile snapshot metadata for '{expectedKey}' is invalid.");
            if (metadata.layers == null)
                metadata.layers = new List<LayerMetadata>();
            if (metadata.trees == null)
                metadata.trees = new List<TreeMetadata>();
            if (metadata.detailDensities == null)
                metadata.detailDensities = new List<DetailDensityMetadata>();
            if (metadata.detailInstances == null)
                metadata.detailInstances = new List<DetailInstanceMetadata>();
            if (!metadata.key.Equals(expectedKey))
                throw new InvalidDataException($"Tile snapshot key mismatch. Expected '{expectedKey}', found '{metadata.key}'.");
            if (metadata.formatVersion != FORMAT_VERSION)
                throw new InvalidDataException($"Tile snapshot '{expectedKey}' uses unsupported format version {metadata.formatVersion}.");
            return metadata;
        }

        private static void WriteMetadata(string directory, Metadata metadata)
        {
            string json = JsonUtility.ToJson(metadata, true);
            File.WriteAllText(Path.Combine(directory, METADATA_FILE_NAME), json, new UTF8Encoding(false));
        }

        private static byte[] ReadBlob(string directory, BlobMetadata blob)
        {
            ValidateBlobLength(blob);
            string path = GetBlobPath(directory, blob);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Snapshot blob '{blob.fileName}' is missing.", path);

            byte[] bytes;
            switch (blob.compression)
            {
                case BlobCompression.None:
                    if (new FileInfo(path).Length != blob.uncompressedLength)
                        throw new InvalidDataException($"Snapshot blob '{blob.fileName}' has an invalid length.");
                    bytes = File.ReadAllBytes(path);
                    break;
                case BlobCompression.GZip:
                    using (FileStream input = File.OpenRead(path))
                    using (GZipStream gzip = new GZipStream(input, CompressionMode.Decompress))
                    using (MemoryStream output = new MemoryStream(Math.Min(blob.uncompressedLength, 81920)))
                    {
                        CopyExactLength(gzip, output, blob.uncompressedLength, blob.fileName);
                        bytes = output.ToArray();
                    }
                    break;
                default:
                    throw new InvalidDataException(
                        $"Snapshot blob '{blob.fileName}' uses unsupported compression '{blob.compression}'.");
            }
            if (bytes.Length != blob.uncompressedLength)
                throw new InvalidDataException($"Snapshot blob '{blob.fileName}' has an invalid length.");
            if (!string.Equals(ComputeChecksumHex(bytes), blob.checksum, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Snapshot blob '{blob.fileName}' failed checksum validation.");
            return bytes;
        }

        private static void ValidateBlobLength(BlobMetadata blob)
        {
            if (blob.uncompressedLength <= 0 || blob.uncompressedLength > MAX_BLOB_UNCOMPRESSED_BYTES)
            {
                throw new InvalidDataException(
                    $"Snapshot blob '{blob.fileName}' declares an invalid uncompressed length of " +
                    $"{blob.uncompressedLength} bytes.");
            }
        }

        private static void CopyExactLength(Stream input, Stream output, int expectedLength, string fileName)
        {
            byte[] buffer = new byte[81920];
            int total = 0;
            while (true)
            {
                int remaining = expectedLength - total;
                int read = input.Read(buffer, 0, Math.Min(buffer.Length, remaining + 1));
                if (read == 0)
                    break;
                total = checked(total + read);
                if (total > expectedLength)
                {
                    throw new InvalidDataException(
                        $"Snapshot blob '{fileName}' decompresses beyond its declared length.");
                }
                output.Write(buffer, 0, read);
            }
        }

        private static string GetBlobPath(string directory, BlobMetadata blob)
        {
            if (blob == null || string.IsNullOrWhiteSpace(blob.fileName))
                throw new InvalidDataException("Snapshot blob has no file name.");
            if (Path.GetFileName(blob.fileName) != blob.fileName)
                throw new InvalidDataException($"Snapshot blob file name '{blob.fileName}' is invalid.");

            string root = Path.GetFullPath(directory);
            string path = Path.GetFullPath(Path.Combine(root, blob.fileName));
            string rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            if (!path.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Snapshot blob path '{blob.fileName}' is outside its snapshot directory.");
            return path;
        }

        private static void WriteCompressedBlob(string path, byte[] bytes)
        {
            using (FileStream output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (GZipStream gzip = new GZipStream(output, System.IO.Compression.CompressionLevel.Optimal))
            {
                gzip.Write(bytes, 0, bytes.Length);
            }
        }

        private static RenderTexture RestoreTexture(BlobMetadata blob, byte[] bytes)
        {
            GraphicsFormat format = (GraphicsFormat)blob.graphicsFormat;
            int expectedByteLength = GetTextureByteLength(blob.width, blob.height, format, blob.fileName);
            if (bytes.Length != expectedByteLength)
            {
                throw new InvalidDataException(
                    $"Snapshot texture '{blob.fileName}' contains {bytes.Length} bytes; expected " +
                    $"{expectedByteLength} for its dimensions and format.");
            }
            if ((SystemInfo.copyTextureSupport & CopyTextureSupport.TextureToRT) == 0)
            {
                throw new NotSupportedException(
                    $"This platform does not support the exact Texture-to-RenderTexture copy required to restore " +
                    $"'{blob.fileName}'.");
            }
            Texture2D source = new Texture2D(blob.width, blob.height, format, TextureCreationFlags.None);
            RenderTexture result = null;
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                source.LoadRawTextureData(bytes);
                source.Apply(false, false);
                RenderTextureDescriptor descriptor = new RenderTextureDescriptor(blob.width, blob.height)
                {
                    graphicsFormat = format,
                    depthBufferBits = 0,
                    enableRandomWrite = blob.enableRandomWrite,
                    useMipMap = blob.useMipMap,
                    autoGenerateMips = blob.autoGenerateMips,
                    msaaSamples = 1
                };
                result = new RenderTexture(descriptor)
                {
                    filterMode = blob.filterMode,
                    wrapMode = blob.wrapMode
                };
                if (!result.Create())
                {
                    throw new InvalidOperationException(
                        $"Failed to create the render texture for snapshot blob '{blob.fileName}'.");
                }
                UnityEngine.Graphics.CopyTexture(source, 0, 0, result, 0, 0);
                if (blob.useMipMap && blob.autoGenerateMips)
                    result.GenerateMips();
                return result;
            }
            catch
            {
                if (result != null)
                {
                    result.Release();
                    UnityEngine.Object.DestroyImmediate(result);
                }
                throw;
            }
            finally
            {
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private static int GetTextureByteLength(int width, int height, GraphicsFormat format, string label)
        {
            if (width <= 0 || height <= 0 ||
                width > SystemInfo.maxTextureSize || height > SystemInfo.maxTextureSize ||
                format == GraphicsFormat.None || GraphicsFormatUtility.IsDepthFormat(format))
            {
                throw new InvalidDataException(
                    $"Snapshot texture '{label}' has invalid dimensions or format: {width}x{height}, {format}.");
            }

            long byteLength;
            try
            {
                byteLength = GraphicsFormatUtility.ComputeMipmapSize(width, height, format);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException(
                    $"Snapshot texture '{label}' uses unsupported format '{format}'.",
                    ex);
            }
            return ValidateCalculatedByteLength(byteLength, $"Snapshot texture '{label}'");
        }

        private static int GetBufferByteLength(int elementCount, int stride, string label)
        {
            long byteLength = checked((long)elementCount * stride);
            return ValidateCalculatedByteLength(byteLength, $"Snapshot buffer '{label}'");
        }

        private static int ValidateCalculatedByteLength(long byteLength, string label)
        {
            if (byteLength <= 0 || byteLength > MAX_BLOB_UNCOMPRESSED_BYTES)
            {
                throw new InvalidDataException(
                    $"{label} requires an unsupported uncompressed size of {byteLength} bytes.");
            }
            return checked((int)byteLength);
        }

        private string GetSnapshotDirectory(Key key)
        {
            if (string.IsNullOrWhiteSpace(key.vistaManagerId))
                throw new ArgumentException("Snapshot key has no Vista Manager ID.", nameof(key));
            if (string.IsNullOrWhiteSpace(rootDirectory))
                throw new InvalidOperationException("Tile snapshot root directory is not configured.");

            string root = Path.GetFullPath(rootDirectory);
            string managerDirectory = MakeVistaManagerDirectoryName(key.vistaManagerId);
            string regionDirectory = GetRegionDirectoryName(key.region);
            string path = Path.GetFullPath(Path.Combine(root, managerDirectory, regionDirectory));
            string rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!path.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Resolved snapshot path is outside the configured root.");
            return path;
        }

        private static string GetRegionDirectoryName(RectXZ region)
        {
            return $"{region.xMinCM}_{region.zMinCM}_{region.xSizeCM}_{region.zSizeCM}";
        }

        private static string MakeVistaManagerDirectoryName(string vistaManagerId)
        {
            string readable = vistaManagerId;
            foreach (char invalid in Path.GetInvalidFileNameChars())
                readable = readable.Replace(invalid, '_');
            readable = readable.Trim();
            if (readable.Length > 48)
                readable = readable.Substring(0, 48);
            if (readable.Length == 0)
                readable = "VistaManager";
            return readable + "_" + ComputeChecksumHex(Encoding.UTF8.GetBytes(vistaManagerId)).Substring(0, 12);
        }

        private static string ComputeChecksumHex(byte[] bytes)
        {
            using (MD5 algorithm = MD5.Create())
            {
                byte[] hash = algorithm.ComputeHash(bytes);
                StringBuilder builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; ++i)
                    builder.Append(hash[i].ToString("x2"));
                return builder.ToString();
            }
        }

        private static void EnsurePairCount(int firstCount, int secondCount, string firstName, string secondName)
        {
            if (firstCount != secondCount)
            {
                throw new InvalidOperationException(
                    $"Biome data contains {firstCount} {firstName} but {secondCount} {secondName}.");
            }
        }

        private static void RegisterReconstructedObject(
            TransientResourceRegistry transientResources,
            List<UnityEngine.Object> reconstructedObjects,
            UnityEngine.Object obj)
        {
            try
            {
                transientResources.RegisterDestroyLater(obj);
                reconstructedObjects.Add(obj);
            }
            catch
            {
                if (obj != null)
                    UnityEngine.Object.DestroyImmediate(obj);
                throw;
            }
        }

        private static void ReleaseReconstructedObjects(
            TransientResourceRegistry transientResources,
            List<UnityEngine.Object> reconstructedObjects)
        {
            for (int i = reconstructedObjects.Count - 1; i >= 0; --i)
            {
                UnityEngine.Object obj = reconstructedObjects[i];
                if (obj != null && transientResources.Unregister(obj))
                    UnityEngine.Object.DestroyImmediate(obj);
            }
        }

        private static void TryDeleteDirectory(string directory)
        {
            if (string.IsNullOrEmpty(directory))
                return;
            try
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
            catch
            {
                // Staging and rollback cleanup must not invalidate an otherwise successful atomic replacement.
            }
        }

        private static void ReleaseTexture(RenderTexture texture)
        {
            if (texture == null)
                return;
            texture.Release();
            UnityEngine.Object.DestroyImmediate(texture);
        }

    }
}
#endif
