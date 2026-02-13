using System;
using DatastoresDX.Runtime;
using UnityEngine.InputSystem;

namespace BetterInputSystem.Runtime
{
    public abstract class BetterActionMap
    {
        public Uid ElementId { get; protected set; }
        public InputActionMap ActionMap { get; protected set; }
        public Action OnEnableChanged;
        
        private bool m_isEnabled = false;
        private bool m_isAllowedToBeEnabled = true;
        
        public bool IsEnabled => ActionMap.enabled;
        public bool IsAllowedToBeEnabled
        {
            get => m_isAllowedToBeEnabled;
            set
            {
                m_isAllowedToBeEnabled = value;
                Enable(m_isEnabled);
            }
        }

        public void Enable(bool enabled)
        {
            bool wasEnabled = ActionMap.enabled;
            
            m_isEnabled = enabled;
            if (m_isAllowedToBeEnabled && m_isEnabled)
            {
                ActionMap.Enable();
            }
            else
            {
                ActionMap.Disable();   
            }

            if (wasEnabled != ActionMap.enabled)
            {
                OnEnableChanged?.Invoke();
            }
        }
    }
}