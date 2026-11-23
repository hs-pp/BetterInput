using System;
using DatastoresDX.Runtime;
using DatastoresDX.Runtime.DataCollections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BetterInputSystem.Runtime
{
    [Serializable]
    [DataElement(typeof(BetterInputActions), "InputActionMap")]
    public class BetterInputActionsData : DataCollectionElement
    {
        [SerializeField]
        private InputActionMap m_actionMap;
        public InputActionMap ActionMap => m_actionMap;
    }
}