using DatastoresDX.Runtime.DataCollections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BetterInputSystem.Runtime
{
    /// <summary>
    /// The authored input data. Each element is a gameplay action map with its own Uid; the UI and debug maps are
    /// fixed fields because there is exactly one of each.
    /// </summary>
    [DataCollection(true, true, "Better Input")]
    public class BetterInputActions : DataCollection
    {
        [SerializeField]
        private InputActionMap m_uiMap;
        public InputActionMap UIMap => m_uiMap;

        [SerializeField]
        private InputActionMap m_debugMap;
        public InputActionMap DebugMap => m_debugMap;

#if UNITY_EDITOR
        public const string UIMap_VarName = nameof(m_uiMap);
        public const string DebugMap_VarName = nameof(m_debugMap);

        /// <summary>Replaces the UI map with a copy of the Input System's default UI actions.</summary>
        public void ResetUIMapToDefaults()
        {
            DefaultInputActions defaults = new DefaultInputActions();
            m_uiMap = defaults.UI.Get().Clone();
            // Not defaults.Dispose(): it calls Destroy, which errors outside Play mode.
            DestroyImmediate(defaults.asset);
        }
#endif
    }
}
