using System;
using System.Collections.Generic;
using DatastoresDX.Runtime;
using DatastoresDX.Runtime.DataCollections;
using UnityEngine.InputSystem;
            
namespace BetterInputSystem.Runtime
{
    public partial class BetterInputActions
    {
        public List<BetterActionMap> BetterActionMaps { get; } = new();
        public Action OnAnyInputActionEnabledChanged;

        public DebugActionMap DebugActionMap { get; private set; }

        public PlayerControlsMap PlayerControlsMap { get; private set; }

        private void GeneratedInitialize()
        {
            Dictionary<string, BetterInputActionsData> actionsData = new();
            foreach (DataCollectionElement element in GetAllElements())
            {
                BetterInputActionsData actionMap = element as BetterInputActionsData;
                actionsData.Add(actionMap.DisplayName.Replace(" ", ""), actionMap);
            }

            DebugActionMap = new DebugActionMap(m_debugActionMap);
            DebugActionMap.Enable(true);
            DebugActionMap.OnEnableChanged += () => { OnAnyInputActionEnabledChanged?.Invoke(); };

            PlayerControlsMap = new PlayerControlsMap(actionsData["PlayerControls"]);
            BetterActionMaps.Add(PlayerControlsMap);

            foreach (BetterActionMap betterActionMap in BetterActionMaps)
            {
                betterActionMap.Enable(false);
                betterActionMap.OnEnableChanged += () => { OnAnyInputActionEnabledChanged?.Invoke(); };
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

    public class PlayerControlsMap : BetterActionMap
    {
        public BetterInputActionsData BetterInputActionsData { get; }
        public InputAction Movement { get; }
        public InputAction Jump { get; }
        public InputAction Interact { get; }

        public PlayerControlsMap(BetterInputActionsData actionsData)
        {
            BetterInputActionsData = actionsData;
            ActionMap = actionsData.ActionMap;
            ElementId = actionsData.Id;
            Movement = BetterInputActionsData.ActionMap.FindAction("Movement");
            Jump = BetterInputActionsData.ActionMap.FindAction("Jump");
            Interact = BetterInputActionsData.ActionMap.FindAction("Interact");
        }
    }
}