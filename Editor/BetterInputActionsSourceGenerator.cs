using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BetterInputSystem.Runtime;
using DatastoresDX.Runtime.DataCollections;
using UnityEditor;
using UnityEngine.InputSystem;

namespace BetterInputSystem.Editor
{
    public class BetterInputActionsSourceGenerator
    {
        private static string GeneratedFileName = "BetterInputActions_Generated.cs";
        
        public static void GenerateAndSaveCode()
        {
            BetterInputActions betterInputActions = AssetDatabase.LoadAssetAtPath<BetterInputActions>(
                AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:BetterInputActions").First()));
            
            List<TopLevelActionMapData> topLevelActionMaps = new();
            topLevelActionMaps.Add(new TopLevelActionMapData(betterInputActions, BetterInputActions.DebugActionMap_VarName));
            
            List<BetterInputActionMapData> betterActionMaps = new();
            foreach (DataCollectionElement element in betterInputActions.GetAllElements())
            {
                BetterInputActionsData actionsData = element as BetterInputActionsData;
                betterActionMaps.Add(new BetterInputActionMapData(actionsData));
            }
            
            MonoScript script = MonoScript.FromScriptableObject(betterInputActions);
            string scriptPath = AssetDatabase.GetAssetPath(script);
            string directory = Path.GetDirectoryName(scriptPath);
            string fullGeneratedFilePath = Path.Combine(directory, GeneratedFileName);
            
            File.WriteAllText(fullGeneratedFilePath, GenerateSource(topLevelActionMaps, betterActionMaps));
            AssetDatabase.SaveAssets();
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
        }
        
        private static string GenerateSource(List<TopLevelActionMapData> topLevelActionMaps, List<BetterInputActionMapData> betterActionMaps)
        {
            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine($@"using System;
using System.Collections.Generic;
using DatastoresDX.Runtime;
using DatastoresDX.Runtime.DataCollections;
using UnityEngine.InputSystem;
            
namespace BetterInputSystem.Runtime
{{
    public partial class BetterInputActions
    {{
        public List<BetterActionMap> BetterActionMaps {{ get; }} = new();
        public Action OnAnyInputActionEnabledChanged;
");
            foreach (TopLevelActionMapData topLevelActionMapData in topLevelActionMaps)
            {
                stringBuilder.AppendLine($"        public {topLevelActionMapData.DisplayName} {topLevelActionMapData.DisplayName} {{ get; private set; }}");
            }
            if (topLevelActionMaps.Count > 0)
            {
                stringBuilder.AppendLine();
            }
            
            foreach (BetterInputActionMapData actionMapData in betterActionMaps)
            {
                stringBuilder.AppendLine($"        public {actionMapData.DisplayName}Map {actionMapData.DisplayName}Map {{ get; private set; }}");
            }
            if (betterActionMaps.Count > 0)
            {
                stringBuilder.AppendLine();
            }
            
            stringBuilder.AppendLine($@"        private void GeneratedInitialize()
        {{
            Dictionary<string, BetterInputActionsData> actionsData = new();
            foreach (DataCollectionElement element in GetAllElements())
            {{
                BetterInputActionsData actionMap = element as BetterInputActionsData;
                actionsData.Add(actionMap.DisplayName.Replace("" "", """"), actionMap);
            }}
");
            
            foreach (TopLevelActionMapData topLevelActionMapData in topLevelActionMaps)
            {
                stringBuilder.AppendLine($@"            {topLevelActionMapData.DisplayName} = new {topLevelActionMapData.DisplayName}({topLevelActionMapData.FieldName});
            {topLevelActionMapData.DisplayName}.Enable(true);
            {topLevelActionMapData.DisplayName}.OnEnableChanged += () => {{ OnAnyInputActionEnabledChanged?.Invoke(); }};");
            }
            if (topLevelActionMaps.Count > 0)
            {
                stringBuilder.AppendLine();
            }
            
            foreach (BetterInputActionMapData actionMapData in betterActionMaps)
            {
                stringBuilder.AppendLine($"            {actionMapData.DisplayName}Map = new {actionMapData.DisplayName}Map(actionsData[\"{actionMapData.DisplayName}\"]);");
                stringBuilder.AppendLine($"            BetterActionMaps.Add({actionMapData.DisplayName}Map);");
            }
            
            stringBuilder.AppendLine($@"
            foreach (BetterActionMap betterActionMap in BetterActionMaps)
            {{
                betterActionMap.Enable(false);
                betterActionMap.OnEnableChanged += () => {{ OnAnyInputActionEnabledChanged?.Invoke(); }};
            }}
        }}
    }}");

            foreach (TopLevelActionMapData topLevelActionMapData in topLevelActionMaps)
            {
                stringBuilder.AppendLine(GenerateTopLevelActionMapClass(topLevelActionMapData));
            }
            
            foreach (BetterInputActionMapData actionMapData in betterActionMaps)
            {
                stringBuilder.AppendLine(GenerateBetterActionMapClass(actionMapData));
            }

            stringBuilder.Append($@"}}");
            return stringBuilder.ToString();
        }
        
        private static string GenerateTopLevelActionMapClass(TopLevelActionMapData topLevelActionMapData)
        {
            StringBuilder stringBuilder = new();
            
            stringBuilder.AppendLine($@"
    public class {topLevelActionMapData.DisplayName} : BetterActionMap
    {{");

            foreach (var actionName in topLevelActionMapData.ActionNames)
            {
                stringBuilder.AppendLine($@"        public InputAction {actionName} {{ get; }}");
            }

            if (topLevelActionMapData.ActionNames.Count > 0)
            {
                stringBuilder.AppendLine();
            }
            stringBuilder.AppendLine($@"        public {topLevelActionMapData.DisplayName}(InputActionMap actionMap)
        {{
            ActionMap = actionMap;
            ElementId = Uid.Invalid;");

            foreach (var actionName in topLevelActionMapData.ActionNames)
            {
                stringBuilder.AppendLine($@"            {actionName} = ActionMap.FindAction(""{actionName}"");");
            }

            stringBuilder.Append($@"        }}
    }}");

            return stringBuilder.ToString();
        }

        private static string GenerateBetterActionMapClass(BetterInputActionMapData actionMapData)
        {
            StringBuilder stringBuilder = new();
            
            stringBuilder.AppendLine($@"
    public class {actionMapData.DisplayName}Map : BetterActionMap
    {{
        public BetterInputActionsData BetterInputActionsData {{ get; }}");

            foreach (var actionName in actionMapData.ActionNames)
            {
                stringBuilder.AppendLine($@"        public InputAction {actionName} {{ get; }}");
            }

            if (actionMapData.ActionNames.Count > 0)
            {
                stringBuilder.AppendLine();
            }
            stringBuilder.AppendLine($@"        public {actionMapData.DisplayName}Map(BetterInputActionsData actionsData)
        {{
            BetterInputActionsData = actionsData;
            ActionMap = actionsData.ActionMap;
            ElementId = actionsData.Id;");

            foreach (var actionName in actionMapData.ActionNames)
            {
                stringBuilder.AppendLine($@"            {actionName} = BetterInputActionsData.ActionMap.FindAction(""{actionName}"");");
            }

            stringBuilder.Append($@"        }}
    }}");

            return stringBuilder.ToString();
        }
        
        private class TopLevelActionMapData
        {
            public string DisplayName { get; }
            public string FieldName { get; }
            public List<string> ActionNames { get; }

            public TopLevelActionMapData(BetterInputActions betterInputActions, string fieldName)
            {
                FieldName = fieldName;
                DisplayName = FormatTopLevelInputActionMapName(fieldName);
                ActionNames = GetActionNames(betterInputActions, fieldName);
            }
            
            private string FormatTopLevelInputActionMapName(string mapName)
            {
                string formatted = mapName.Replace("m_", "");
                formatted = formatted[0].ToString().ToUpper() + formatted.Substring(1);
                if (formatted == mapName)
                {
                    formatted = "_" + formatted;
                }

                return formatted;
            }
            
            private List<string> GetActionNames(BetterInputActions betterInputActions, string topLevelActionMapName)
            {
                List<string> actionNames = new();
                FieldInfo actionMapField = typeof(BetterInputActions).GetField(topLevelActionMapName, BindingFlags.NonPublic | BindingFlags.Instance);
                InputActionMap inputActionMap = actionMapField.GetValue(betterInputActions) as InputActionMap;
                foreach (var action in inputActionMap.actions)
                {
                    actionNames.Add(action.name.Replace(" ", ""));
                }

                return actionNames;
            }
        }

        private class BetterInputActionMapData
        {
            public string DisplayName { get; }
            public List<string> ActionNames { get; }

            public BetterInputActionMapData(BetterInputActionsData actionsData)
            {
                DisplayName = actionsData.DisplayName.Replace(" ", "");

                ActionNames = new();
                foreach (InputAction action in actionsData.ActionMap.actions)
                {
                    ActionNames.Add(action.name.Replace(" ", ""));
                }
            }
        }
    }
}