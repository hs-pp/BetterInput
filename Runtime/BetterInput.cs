using System;
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
        private Action OnAnyInputActionEnabledChanged;

        public InputDeviceType CurrentInputDeviceType { get; private set; } = InputDeviceType.KeyboardAndMouse;
        public Action<InputDeviceType> OnInputDeviceTypeChanged;

        public override Task OnInitialize()
        {
            // UserInput instance
            GameObject prefab = Resources.Load<GameObject>(PREFAB_PATH);
            if (prefab == null)
            {
                LogError($"Failed to load UserInput prefab.");
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

            BetterInputActions.Initialize(OnAnyActionTriggered);
            BetterInputActions.OnAnyInputActionEnabledChanged += () => { OnAnyInputActionEnabledChanged?.Invoke(); };
            
            InputSystemUIInputModule.actionsAsset = BetterInputActions.UIInputActionMap.asset;
            return Task.CompletedTask;
        }

        public void EnableUIInputActions()
        {
            BetterInputActions.UIActions.Enable();
            AllowInputActionsToBeEnabled(false);
            Log("Enabled UI Action Map.");
        }

        public void EnableGameInputActions()
        {
            BetterInputActions.UIActions.Disable();
            AllowInputActionsToBeEnabled(true);
            Log("Enabled Game Action Maps.");
        }

        public void EnableActionMap(Uid actionMapId, bool enable)
        {
            BetterActionMap actionMap = BetterInputActions.BetterActionMaps.Find(x => x.ElementId.Equals(actionMapId));
            actionMap.Enable(enable);
        }
        
        private void AllowInputActionsToBeEnabled(bool isAllowed)
        {
            foreach (BetterActionMap actionMap in BetterInputActions.BetterActionMaps)
            {
                actionMap.IsAllowedToBeEnabled = isAllowed;
            }
        }

        private void OnAnyActionTriggered(InputAction.CallbackContext context)
        {
            if (!context.performed && !context.started)
            {
                return;
            }
            Log($"{context.control.device.name}::{context.action.name}");
            
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

        private const string LOG_HEADER = "[BetterInput]";
        internal static void Log(string log)
        {
            //Debug.Log($"{LOG_HEADER} {log}");
        }

        internal static void LogWarning(string log)
        {
            //Debug.LogWarning($"{LOG_HEADER} {log}");
        }

        internal static void LogError(string log)
        {
            //Debug.LogError($"{LOG_HEADER} {log}");
        }

        // [RemedyCommand]
        // private static string PrintToScreen_BetterInputs()
        // {
        //     BetterInput betterInput = SystemCore.GetKeySystem<BetterInput>();
        //     BetterInputActions betterInputActions = betterInput.BetterInputActions;
        //
        //     PrintToScreenItem item = new PrintToScreenItem();
        //     item.Title = "BetterInput Action States";
        //     betterInput.OnAnyInputActionEnabledChanged += () => // does this need to get unbinded when we clear this printtoscreenitem?
        //     {
        //         item.RefreshContentTrigger?.Invoke();
        //     };
        //     item.GetContentFunc = () =>
        //     {
        //         StringBuilder stringBuilder = new();
        //         stringBuilder.AppendLine($"UI Input Actions: {betterInputActions.UIInputActionMap.UI.enabled}");
        //         stringBuilder.AppendLine($"Debug Input Actions: {betterInputActions.DebugActionMap.IsEnabled}");
        //         foreach (BetterActionMap actionMap in betterInputActions.BetterActionMaps)
        //         {
        //             stringBuilder.AppendLine($"{actionMap.GetType().Name}: {actionMap.IsEnabled}");
        //         }
        //
        //         return stringBuilder.ToString();
        //     };
        //     
        //     SystemCore.GetKeySystem<Remedy>().AddPrintToScreenItem(item);
        //     
        //     return null;
        // }
    }
}