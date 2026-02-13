using System;
using DatastoresDX.Runtime.DataCollections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BetterInputSystem.Runtime
{
    [DataCollection(true, true, "Better Input")]
    public partial class BetterInputActions : DataCollection
    {
        public UIInputActions UIInputActionMap { get; private set; }
        public UIInputActions.UIActions UIActions => UIInputActionMap.UI;

        [SerializeField]
        private InputActionMap m_debugActionMap;
        
        public void Initialize(Action<InputAction.CallbackContext> onAnyActionTriggered)
        {
            UIInputActionMap = new();
            GeneratedInitialize();
            
            foreach (BetterActionMap bam in BetterActionMaps)
            {
                bam.ActionMap.actionTriggered += context =>
                {
                    onAnyActionTriggered?.Invoke(context);
                };
            }
            UIInputActionMap.UI.Get().actionTriggered += context =>
            {
                onAnyActionTriggered?.Invoke(context);
            };
        }

        public static string DebugActionMap_VarName = "m_debugActionMap";
    }
}