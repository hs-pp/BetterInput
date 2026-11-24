using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DatastoresDX.Runtime;
using DatastoresDX.Runtime.DataCollections;
using SystemCoreSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace BetterInputSystem.Runtime
{
    public enum InputDeviceType
    {
        Unknown,
        KeyboardAndMouse,
        Gamepad, // One day expand to support specific like xbox and ps5
    }

    public class BetterInput : AKeySystem
    {
        public const string UI_MAP_NAME = "UI";
        public const string DEBUG_MAP_NAME = "Debug";

        private readonly Dictionary<Uid, BetterActionMap> m_maps = new();
        private bool m_isUIMode;

        /// <summary>Runtime asset holding the UI, debug and every gameplay map, copied from the data at startup.</summary>
        public InputActionAsset Asset { get; private set; }
        public InputActionMap UIMap { get; private set; }
        public InputActionMap DebugMap { get; private set; }
        public EventSystem EventSystem { get; private set; }
        public InputSystemUIInputModule UIInputModule { get; private set; }

        public InputDeviceType CurrentDeviceType { get; private set; } = InputDeviceType.KeyboardAndMouse;
        public event Action<InputDeviceType> DeviceTypeChanged;

        public override Task OnInitialize()
        {
            List<BetterInputActions> collections = Datastores.GetDataCollectionsOfType<BetterInputActions>();
            if (collections.Count != 1)
            {
                Debug.LogError($"BetterInput needs exactly one Better Input data collection but found {collections.Count}. Input is disabled.");
                return Task.CompletedTask;
            }

            BuildAsset(collections[0]);
            CreateEventSystem();

            // Until SetUIMode is first called, the UI map and any requested gameplay maps are both live.
            UIMap.Enable();
            DebugMap.Enable();

            InputSystem.onActionChange += OnActionChange;
            return Task.CompletedTask;
        }

        public override Task OnDeinitialize()
        {
            InputSystem.onActionChange -= OnActionChange;
            if (EventSystem != null)
            {
                Destroy(EventSystem.gameObject);
            }
            if (Asset != null)
            {
                Asset.Disable();
                Destroy(Asset);
            }

            m_maps.Clear();
            m_isUIMode = false;
            Asset = null;
            UIMap = null;
            DebugMap = null;
            EventSystem = null;
            UIInputModule = null;
            return Task.CompletedTask;
        }

        /// <summary>Returns the gameplay map with this Uid, or null if there isn't one.</summary>
        public BetterActionMap GetMap(Uid id)
        {
            return m_maps.TryGetValue(id, out BetterActionMap map) ? map : null;
        }

        /// <summary>
        /// UI mode turns the UI map on and holds every gameplay map off. Leaving it turns the UI map off and lets each
        /// gameplay map return to whatever it was last asked for. The debug map is never affected.
        /// </summary>
        public void SetUIMode(bool isUIMode)
        {
            if (Asset == null)
            {
                return;
            }

            m_isUIMode = isUIMode;
            if (isUIMode)
            {
                UIMap.Enable();
            }
            else
            {
                UIMap.Disable();
            }

            foreach (BetterActionMap map in m_maps.Values)
            {
                ApplyGate(map);
            }
        }

        internal void ApplyGate(BetterActionMap map)
        {
            if (map.IsRequested && !m_isUIMode)
            {
                map.Actions.Enable();
            }
            else
            {
                map.Actions.Disable();
            }
        }

        private void BuildAsset(BetterInputActions data)
        {
            Asset = ScriptableObject.CreateInstance<InputActionAsset>();
            Asset.name = data.name;
            UIMap = AddMap(data.UIMap, UI_MAP_NAME, Uid.Invalid);
            DebugMap = AddMap(data.DebugMap, DEBUG_MAP_NAME, Uid.Invalid);

            foreach (DataCollectionElement element in data.GetAllElements())
            {
                if (element is BetterInputActionsData mapData)
                {
                    InputActionMap actions = AddMap(mapData.ActionMap, mapData.DisplayName, mapData.Id);
                    m_maps.Add(mapData.Id, new BetterActionMap(this, mapData.Id, actions));
                }
            }
        }

        // Copies a map from the data into the runtime asset, so the data asset is never touched at runtime.
        // Binding IDs are kept, so saved rebinds still line up with the data.
        private InputActionMap AddMap(InputActionMap source, string name, Uid id)
        {
            if (string.IsNullOrEmpty(name) || Asset.FindActionMap(name) != null)
            {
                name = string.IsNullOrEmpty(name) ? id.ToString() : $"{name} ({id})";
            }

            InputActionMap map = Asset.AddActionMap(name);
            if (source == null)
            {
                return map;
            }

            foreach (InputAction action in source.actions)
            {
                InputAction copy = map.AddAction(action.name, action.type, interactions: action.interactions,
                    processors: action.processors, expectedControlLayout: action.expectedControlType);
                copy.wantsInitialStateCheck = action.wantsInitialStateCheck;
            }
            foreach (InputBinding binding in source.bindings)
            {
                map.AddBinding(binding);
            }
            return map;
        }

        private void CreateEventSystem()
        {
            // Built inactive so the UI module's OnEnable finds our actions instead of assigning its own defaults.
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.SetActive(false);
            eventSystemObject.transform.SetParent(transform, false);

            EventSystem = eventSystemObject.AddComponent<EventSystem>();
            UIInputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            UIInputModule.actionsAsset = Asset;
            UIInputModule.point = UIAction("Point");
            UIInputModule.move = UIAction("Navigate");
            UIInputModule.submit = UIAction("Submit");
            UIInputModule.cancel = UIAction("Cancel");
            UIInputModule.leftClick = UIAction("Click");
            UIInputModule.middleClick = UIAction("MiddleClick");
            UIInputModule.rightClick = UIAction("RightClick");
            UIInputModule.scrollWheel = UIAction("ScrollWheel");
            UIInputModule.trackedDevicePosition = UIAction("TrackedDevicePosition");
            UIInputModule.trackedDeviceOrientation = UIAction("TrackedDeviceOrientation");

            eventSystemObject.SetActive(true);
        }

        private InputActionReference UIAction(string actionName)
        {
            InputAction action = UIMap.FindAction(actionName);
            return action != null ? InputActionReference.Create(action) : null;
        }

        private void OnActionChange(object actionOrMap, InputActionChange change)
        {
            if (change != InputActionChange.ActionStarted && change != InputActionChange.ActionPerformed)
            {
                return;
            }

            InputDeviceType type = (actionOrMap as InputAction)?.activeControl?.device switch
            {
                Gamepad => InputDeviceType.Gamepad,
                Keyboard or Mouse => InputDeviceType.KeyboardAndMouse,
                _ => InputDeviceType.Unknown,
            };
            if (type == InputDeviceType.Unknown || type == CurrentDeviceType)
            {
                return;
            }

            CurrentDeviceType = type;
            DeviceTypeChanged?.Invoke(type);
        }
    }
}
