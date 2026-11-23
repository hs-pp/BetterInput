using System;
using System.Collections.Generic;
using DatastoresDX.Runtime.DataCollections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BetterInputSystem.Runtime
{
    [DataCollection(true, true, "Better Input")]
    public class BetterInputActions : DataCollection
    {
        public UIInputActions UIInputActionMap { get; private set; }
        public UIInputActions.UIActions UIActions => UIInputActionMap.UI;

        [SerializeField]
        private InputActionAsset m_uiActionMapAsset; // This needs to be an asset so we can register it to the InputSystemUIInputModule. :(

        [SerializeField]
        private InputActionMap m_debugInputActions;
        public InputActionMap DebugInputActions => m_debugInputActions;
        
        public void Initialize(Action<InputAction.CallbackContext> onAnyActionTriggered)
        {
            UIInputActionMap = new();
            UIInputActionMap.UI.Get().actionTriggered += context =>
            {
                onAnyActionTriggered?.Invoke(context);
            };
        }
        
#if UNITY_EDITOR
        public static string UIActionMapAsset_VarName = "m_uiActionMapAsset";
        public static string DebugActionMap_VarName = "m_debugInputActions";
#endif
    }
}