#if VISTA
using System;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista.Graph;
using UnityEditor;
using UnityEngine;

namespace Pinwheel.Vista.Editor
{
    internal static class TileSnapshotStorageTestCommands
    {
        private const string MENU_PATH = "Vista Internal/Tile Snapshot Storage/Capture Selected";
        private static bool s_isRunning;

        [MenuItem(MENU_PATH)]
        private static void TestSelectedTileSnapshot()
        {
            ITileSnapshotProvider provider = GetSelectedProvider();
            if (provider == null)
            {
                Debug.LogError("Select a scene GameObject containing a tile that implements ITileSnapshotProvider.");
                return;
            }
            if (s_isRunning)
            {
                Debug.LogWarning("A tile snapshot storage test is already running.", provider.gameObject);
                return;
            }
            if (TileService.snapshotStorage == null)
            {
                Debug.LogError("No tile snapshot storage is available for the current platform.", provider.gameObject);
                return;
            }

            TransientResourceRegistry transientResources = new TransientResourceRegistry();
            try
            {
                BiomeDataRequest request = provider.CaptureSnapshot(transientResources);
                s_isRunning = true;
                CoroutineUtility.StartCoroutine(
                    RunRoundTrip(provider, request, transientResources),
                    exception =>
                    {
                        s_isRunning = false;
                        Debug.LogException(exception, provider.gameObject);
                    });
            }
            catch (Exception exception)
            {
                s_isRunning = false;
                transientResources.Dispose();
                Debug.LogException(exception, provider.gameObject);
            }
        }

        [MenuItem(MENU_PATH, true)]
        private static bool ValidateTestSelectedTileSnapshot()
        {
            return !s_isRunning && GetSelectedProvider() != null;
        }

        private static IEnumerator RunRoundTrip(
            ITileSnapshotProvider provider,
            BiomeDataRequest request,
            TransientResourceRegistry transientResources)
        {
            BiomeData capturedData = request.data;
            BiomeData loadedData = null;
            TransientResourceRegistry loadedResources = new TransientResourceRegistry();
            try
            {
                yield return request;
                if (request.isFaulted)
                    throw new InvalidOperationException("The selected tile snapshot capture failed.", request.exception);

                TileSnapshotStorage.Key key = new TileSnapshotStorage.Key(
                    provider.managerId,
                    RectXZ.FromBounds(provider.worldBounds));
                TileSnapshotStorage storage = TileService.snapshotStorage;

                if (!storage.TryStore(key, capturedData, out string storeError))
                    throw new InvalidOperationException($"Snapshot store failed: {storeError}");
                if (!storage.TryLoad(key, loadedResources, out loadedData, out string loadError))
                    throw new InvalidOperationException($"Snapshot load failed: {loadError}");

                ValidateTexture(capturedData.heightMap, loadedData.heightMap, "height");
                ValidateTexture(capturedData.holeMap, loadedData.holeMap, "holes");
                ValidateTexture(capturedData.meshDensityMap, loadedData.meshDensityMap, "mesh density");
                ValidateTexture(capturedData.albedoMap, loadedData.albedoMap, "albedo");
                ValidateTexture(capturedData.metallicMap, loadedData.metallicMap, "metallic");
                ValidateLayers(capturedData, loadedData);
                ValidateTrees(capturedData, loadedData);
                ValidateDetailDensities(capturedData, loadedData);
                ValidateDetailInstances(capturedData, loadedData);

                Debug.Log($"Tile snapshot store/load test passed for '{key}'.", provider.gameObject);
                AssetDatabase.Refresh();
            }
            finally
            {
                loadedData?.Dispose();
                loadedResources.Dispose();
                capturedData?.Dispose();
                transientResources.Dispose();
                s_isRunning = false;
            }
        }

        private static void ValidateTexture(RenderTexture expected, RenderTexture actual, string channelName)
        {
            if (expected == null && actual == null)
                return;
            if (expected == null || actual == null)
                throw new InvalidOperationException($"The loaded {channelName} channel does not match the captured channel.");
            if (expected.width != actual.width ||
                expected.height != actual.height ||
                expected.graphicsFormat != actual.graphicsFormat ||
                expected.filterMode != actual.filterMode ||
                expected.wrapMode != actual.wrapMode ||
                expected.descriptor.useMipMap != actual.descriptor.useMipMap ||
                expected.descriptor.autoGenerateMips != actual.descriptor.autoGenerateMips ||
                expected.enableRandomWrite != actual.enableRandomWrite)
            {
                throw new InvalidOperationException($"The loaded {channelName} texture descriptor does not match the capture.");
            }
        }

        private static void ValidateLayers(BiomeData expected, BiomeData actual)
        {
            List<TerrainLayer> expectedLayers = new List<TerrainLayer>();
            List<RenderTexture> expectedWeights = new List<RenderTexture>();
            List<TerrainLayer> actualLayers = new List<TerrainLayer>();
            List<RenderTexture> actualWeights = new List<RenderTexture>();
            expected.GetLayerWeights(expectedLayers, expectedWeights);
            actual.GetLayerWeights(actualLayers, actualWeights);

            if (expectedLayers.Count != actualLayers.Count)
                throw new InvalidOperationException("The loaded terrain-layer count does not match the capture.");
            for (int i = 0; i < expectedLayers.Count; ++i)
            {
                ValidateTerrainLayer(expectedLayers[i], actualLayers[i], i);
                ValidateTexture(expectedWeights[i], actualWeights[i], $"terrain layer {i} weight");
            }
        }

        private static void ValidateTerrainLayer(TerrainLayer expected, TerrainLayer actual, int index)
        {
            if (expected == null || actual == null ||
                expected.diffuseTexture != actual.diffuseTexture ||
                expected.normalMapTexture != actual.normalMapTexture ||
                expected.maskMapTexture != actual.maskMapTexture ||
                expected.tileSize != actual.tileSize ||
                expected.tileOffset != actual.tileOffset)
            {
                throw new InvalidOperationException($"The loaded terrain layer {index} does not match the capture.");
            }
        }

        private static void ValidateTrees(BiomeData expected, BiomeData actual)
        {
            List<TreeTemplate> expectedTemplates = new List<TreeTemplate>();
            List<ComputeBuffer> expectedBuffers = new List<ComputeBuffer>();
            List<TreeTemplate> actualTemplates = new List<TreeTemplate>();
            List<ComputeBuffer> actualBuffers = new List<ComputeBuffer>();
            expected.GetTrees(expectedTemplates, expectedBuffers);
            actual.GetTrees(actualTemplates, actualBuffers);

            if (expectedTemplates.Count != actualTemplates.Count)
                throw new InvalidOperationException("The loaded tree count does not match the capture.");
            for (int i = 0; i < expectedTemplates.Count; ++i)
            {
                TreeTemplate expectedTemplate = expectedTemplates[i];
                TreeTemplate actualTemplate = actualTemplates[i];
                if (expectedTemplate == null || actualTemplate == null ||
                    expectedTemplate.prefab != actualTemplate.prefab ||
                    expectedTemplate.billboard != actualTemplate.billboard ||
                    expectedTemplate.baseScale != actualTemplate.baseScale ||
                    expectedTemplate.baseRotation != actualTemplate.baseRotation)
                {
                    throw new InvalidOperationException($"The loaded tree template {i} does not match the capture.");
                }
                ValidateBuffer(expectedBuffers[i], actualBuffers[i], $"tree {i} instances");
            }
        }

        private static void ValidateDetailDensities(BiomeData expected, BiomeData actual)
        {
            List<DetailTemplate> expectedTemplates = new List<DetailTemplate>();
            List<RenderTexture> expectedMaps = new List<RenderTexture>();
            List<DetailTemplate> actualTemplates = new List<DetailTemplate>();
            List<RenderTexture> actualMaps = new List<RenderTexture>();
            expected.GetDensityMaps(expectedTemplates, expectedMaps);
            actual.GetDensityMaps(actualTemplates, actualMaps);

            if (expectedTemplates.Count != actualTemplates.Count)
                throw new InvalidOperationException("The loaded detail-density count does not match the capture.");
            for (int i = 0; i < expectedTemplates.Count; ++i)
            {
                ValidateDetailTemplate(expectedTemplates[i], actualTemplates[i], $"detail density {i}");
                ValidateTexture(expectedMaps[i], actualMaps[i], $"detail density {i}");
            }
        }

        private static void ValidateDetailInstances(BiomeData expected, BiomeData actual)
        {
            List<DetailTemplate> expectedTemplates = new List<DetailTemplate>();
            List<ComputeBuffer> expectedBuffers = new List<ComputeBuffer>();
            List<DetailTemplate> actualTemplates = new List<DetailTemplate>();
            List<ComputeBuffer> actualBuffers = new List<ComputeBuffer>();
            expected.GetDetailInstances(expectedTemplates, expectedBuffers);
            actual.GetDetailInstances(actualTemplates, actualBuffers);

            if (expectedTemplates.Count != actualTemplates.Count)
                throw new InvalidOperationException("The loaded detail-instance count does not match the capture.");
            for (int i = 0; i < expectedTemplates.Count; ++i)
            {
                ValidateDetailTemplate(expectedTemplates[i], actualTemplates[i], $"detail instance {i}");
                ValidateBuffer(expectedBuffers[i], actualBuffers[i], $"detail instance {i}");
            }
        }

        private static void ValidateDetailTemplate(
            DetailTemplate expected,
            DetailTemplate actual,
            string channelName)
        {
            if (expected == null || actual == null ||
                expected.renderMode != actual.renderMode ||
                expected.texture != actual.texture ||
                expected.prefab != actual.prefab ||
                expected.primaryColor != actual.primaryColor ||
                expected.secondaryColor != actual.secondaryColor ||
                expected.minHeight != actual.minHeight ||
                expected.maxHeight != actual.maxHeight ||
                expected.minWidth != actual.minWidth ||
                expected.maxWidth != actual.maxWidth ||
                expected.density != actual.density ||
                expected.textureBasedGrassShape != actual.textureBasedGrassShape)
            {
                throw new InvalidOperationException($"The loaded {channelName} template does not match the capture.");
            }
        }

        private static void ValidateBuffer(ComputeBuffer expected, ComputeBuffer actual, string channelName)
        {
            if (expected == null || actual == null ||
                expected.count != actual.count ||
                expected.stride != actual.stride)
            {
                throw new InvalidOperationException($"The loaded {channelName} buffer descriptor does not match the capture.");
            }

            float[] expectedValues = new float[expected.count];
            float[] actualValues = new float[actual.count];
            expected.GetData(expectedValues);
            actual.GetData(actualValues);
            for (int i = 0; i < expectedValues.Length; ++i)
            {
                if (expectedValues[i] != actualValues[i])
                    throw new InvalidOperationException($"The loaded {channelName} buffer differs at element {i}.");
            }
        }

        private static ITileSnapshotProvider GetSelectedProvider()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null || !selected.scene.IsValid())
                return null;

            MonoBehaviour[] components = selected.GetComponentsInParent<MonoBehaviour>(true);
            for (int i = 0; i < components.Length; ++i)
            {
                if (components[i] is ITileSnapshotProvider provider)
                    return provider;
            }
            return null;
        }
    }
}
#endif
