using System;
using System.Collections.Generic;
using DatastoresDX.Runtime;
using DatastoresDX.Runtime.DataCollections;
using UnityEngine.InputSystem;

namespace BetterInputSystem.Runtime
{
    public abstract class ABetterInputActionsController
    {
        protected List<BetterActionMap> m_betterActionMaps { get; } = new();
        
        public abstract void Initialize(BetterInputActions betterInputActions, 
            Action<InputAction.CallbackContext> onAnyActionTriggered, Action onAnyInputActionEnabledChanged);
        
        public void EnableActionMap(Uid actionMapId, bool enable)
        {
            BetterActionMap actionMap = m_betterActionMaps.Find(x => x.ElementId.Equals(actionMapId));
            if (actionMap != null)
            {
                actionMap.Enable(enable);
            }
        }
        
        public void AllowInputActionsToBeEnabled(bool isAllowed)
        {
            foreach (BetterActionMap actionMap in m_betterActionMaps)
            {
                actionMap.IsAllowedToBeEnabled = isAllowed;
            }
        }
    }
}

            
// TO BE GENERATED
namespace BetterInputSystem.Runtime
{
    public class BetterInputActionsController : ABetterInputActionsController
    {
        public DebugActionMap DebugActionMap { get; private set; }
        public Map Map { get; private set; }

        public override void Initialize(BetterInputActions betterInputActions, Action<InputAction.CallbackContext> onAnyActionTriggered, Action onAnyInputActionEnabledChanged)
        {
            // Debug Action Map
            DebugActionMap = new DebugActionMap(betterInputActions.DebugInputActions);
            DebugActionMap.Enable(true);
            DebugActionMap.OnEnableChanged += () => { onAnyInputActionEnabledChanged?.Invoke(); };

            // cache data by name
            Dictionary<string, BetterInputActionsData> actionsData = new();
            foreach (DataCollectionElement element in betterInputActions.GetAllElements())
            {
                BetterInputActionsData actionMap = element as BetterInputActionsData;
                actionsData.Add(actionMap.DisplayName.Replace(" ", ""), actionMap);
            }
            
            // Register all generated maps
            Map = new Map(actionsData[""]);
            m_betterActionMaps.Add(Map);

            foreach (BetterActionMap betterActionMap in m_betterActionMaps)
            {
                betterActionMap.Enable(false);
                betterActionMap.OnEnableChanged += () => { onAnyInputActionEnabledChanged?.Invoke(); };
                betterActionMap.ActionMap.actionTriggered += context => { onAnyActionTriggered?.Invoke(context); };
            }
        }
    }

    public class DebugActionMap : BetterActionMap
    {
        public DebugActionMap(InputActionMap actionMap)
        {
            ActionMap = actionMap;
            ElementId = Uid.Invalid;
        }
    }

    public class Map : BetterActionMap
    {
        public BetterInputActionsData BetterInputActionsData { get; }
        public Map(BetterInputActionsData actionsData)
        {
            BetterInputActionsData = actionsData;
            ActionMap = actionsData.ActionMap;
            ElementId = actionsData.Id;
        }
    }
}