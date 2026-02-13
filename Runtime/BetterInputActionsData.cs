using DatastoresDX.Runtime;
using DatastoresDX.Runtime.DataCollections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BetterInputSystem.Runtime
{
    [DataElement(typeof(BetterInputActions), "InputActionMap")]
    public class BetterInputActionsData : DataCollectionElement
    {
        [SerializeField]
        private InputActionMap m_actionMap;
        public InputActionMap ActionMap => m_actionMap;
        
        public static readonly string Map_VarName = "m_actionMap";
    }
}