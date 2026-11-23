using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatastoresDX.Runtime;
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
        private static string PREFAB_PATH = "BetterInput/BetterInput";

        public EventSystem EventSystem { get; private set; }
        public InputSystemUIInputModule InputSystemUIInputModule { get; private set; }

        public BetterInputActions BetterInputActions { get; private set; }
        public ABetterInputActionsController InputActionsController { get; private set; }
        private Action OnAnyInputActionEnabledChanged;

        public InputDeviceType CurrentInputDeviceType { get; private set; } = InputDeviceType.KeyboardAndMouse;
        public Action<InputDeviceType> OnInputDeviceTypeChanged;

        public override Task OnInitialize()
        {
            // UserInput instance
            GameObject prefab = Resources.Load<GameObject>(PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogError($"Failed to load UserInput prefab.");
                return Task.CompletedTask;
            }
            GameObject instance = Instantiate(prefab, transform);
            
            EventSystem = instance.GetComponent<EventSystem>();
            InputSystemUIInputModule = instance.GetComponent<InputSystemUIInputModule>();
            
            // Input Actions
            BetterInputActions = Datastores.GetDataCollectionsOfType<BetterInputActions>().FirstOrDefault();
            if (BetterInputActions == null)
            {
                return Task.CompletedTask;
            }

            InputActionsController = CreateBetterInputActionsController();
            InputActionsController.Initialize(BetterInputActions, OnAnyActionTriggered, () => { OnAnyInputActionEnabledChanged?.Invoke(); });

            BetterInputActions.Initialize(OnAnyActionTriggered); // TODO: REMOVE THIS
            
            InputSystemUIInputModule.actionsAsset = BetterInputActions.UIInputActionMap.asset;
            return Task.CompletedTask;
        }

        private ABetterInputActionsController CreateBetterInputActionsController()
        {
            List<Type> types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(ABetterInputActionsController)) && !type.IsAbstract).ToList();
            if (types.Count == 0)
            {
                Debug.LogError("Could not find BetterInputActionsController implementation! Make sure to click the Generate Code button!");
                return null;
            }

            return (ABetterInputActionsController)Activator.CreateInstance(types[0]);
        }

        public void EnableUIInputActions()
        {
            BetterInputActions.UIActions.Enable();
            AllowInputActionsToBeEnabled(false);
        }

        public void EnableGameInputActions()
        {
            BetterInputActions.UIActions.Disable();
            AllowInputActionsToBeEnabled(true);
        }

        public void EnableActionMap(Uid actionMapId, bool enable)
        {
            InputActionsController.EnableActionMap(actionMapId, enable);
        }
        
        private void AllowInputActionsToBeEnabled(bool isAllowed)
        {
            InputActionsController.AllowInputActionsToBeEnabled(isAllowed);
        }

        private void OnAnyActionTriggered(InputAction.CallbackContext context)
        {
            if (!context.performed && !context.started)
            {
                return;
            }
            
            Debug.Log($"{context.control.device.name}::{context.action.name}");
            
            InputDeviceType type = GetDeviceType(context.control.device);
            if (type != CurrentInputDeviceType)
            {
                CurrentInputDeviceType = type;
                OnInputDeviceTypeChanged?.Invoke(type);
            }
        }

        private InputDeviceType GetDeviceType(InputDevice inputDevice)
        {
            switch (inputDevice.name)
            {
                case "Keyboard":
                case "Mouse":
                    return InputDeviceType.KeyboardAndMouse;
                case "Gamepad":
                    return InputDeviceType.Gamepad;
                default:
                    return InputDeviceType.Unknown;
            }
        }
    }
}