#if VISTA
using System;
using UnityEngine;

namespace Pinwheel.Vista
{
    public enum TerrainSealWorkflow
    {
        Automatic,
        Manual
    }

    /// <summary>
    /// Project-wide Vista settings used by both editor and player code.
    /// </summary>
    public sealed class VistaRuntimeSettings : ScriptableObject
    {
        private const string RESOURCE_PATH = "Vista/RuntimeSettings";
        private static VistaRuntimeSettings s_instance;

        [SerializeField, HideInInspector]
        private TerrainSealWorkflow m_sealWorkflow = TerrainSealWorkflow.Automatic;

        public TerrainSealWorkflow sealWorkflow
        {
            get => m_sealWorkflow;
            set
            {
                if (m_sealWorkflow == value)
                    return;

                foreach (VistaManager manager in VistaManager.allInstances)
                {
                    if (manager != null && manager.isGenerating)
                    {
                        Debug.LogWarning(
                            "VISTA: Terrain Seal workflow cannot be changed while a Vista Manager is generating. " +
                            "The current workflow remains unchanged.",
                            manager);
                        return;
                    }
                }

                m_sealWorkflow = value;
            }
        }

        public static VistaRuntimeSettings Get()
        {
            if (s_instance == null)
                s_instance = Resources.Load<VistaRuntimeSettings>(RESOURCE_PATH);

            if (s_instance == null)
            {
                throw new InvalidOperationException(
                    $"VISTA: Required runtime settings asset was not found at Resources/{RESOURCE_PATH}. " +
                    "Re-import Vista or restore the missing RuntimeSettings asset.");
            }
            return s_instance;
        }
    }
}
#endif
