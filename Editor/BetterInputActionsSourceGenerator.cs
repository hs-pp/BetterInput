using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BetterInputSystem.Runtime;
using DatastoresDX.Runtime.DataCollections;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BetterInputSystem.Editor
{
    /// <summary>
    /// Writes typed access to the Better Input collection's maps. Runs whenever the collection asset is imported
    /// (saved or reimported) and only rewrites the script when its contents change, so editing bindings doesn't
    /// trigger a recompile. The script is created next to the collection and can be moved anywhere under Assets.
    /// Before generating, a collection with an empty UI map gets the Input System's default UI actions.
    /// </summary>
    public class BetterInputActionsSourceGenerator : AssetPostprocessor
    {
        private const string FILE_NAME = "BetterInputActions.Generated.cs";
        private const string ROOT_CLASS = "GeneratedActions";

        private class MapCode
        {
            public string Name;
            public string Property;
            public string ClassName;
            public string Source;
            public bool IsGameplay;
            public readonly List<(string Property, string ActionName)> Actions = new();
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
            {
                if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(BetterInputActions))
                {
                    FillEmptyUIMap(path);
                    Generate(path);
                }
            }
        }

        // An empty UI map means no UI input at all, so new (or emptied) collections get the defaults.
        private static void FillEmptyUIMap(string collectionPath)
        {
            BetterInputActions collection = AssetDatabase.LoadAssetAtPath<BetterInputActions>(collectionPath);
            if (collection == null || (collection.UIMap != null && collection.UIMap.actions.Count > 0))
            {
                return;
            }

            collection.ResetUIMapToDefaults();
            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssetIfDirty(collection);
            Debug.Log($"BetterInput: the UI map in {collectionPath} was empty, so it was filled with the Input System's default UI actions.", collection);
        }

        public static void Generate(string collectionPath)
        {
            BetterInputActions collection = AssetDatabase.LoadAssetAtPath<BetterInputActions>(collectionPath);
            if (collection == null)
            {
                return;
            }
            if (AssetDatabase.FindAssets($"t:{nameof(BetterInputActions)}").Length > 1)
            {
                Debug.LogError("BetterInput: more than one Better Input collection exists. Keep one; code was not generated.");
                return;
            }

            string[] existing = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(FILE_NAME), new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Path.GetFileName(path) == FILE_NAME)
                .ToArray();
            if (existing.Length > 1)
            {
                Debug.LogError($"BetterInput: found more than one {FILE_NAME} ({string.Join(", ", existing)}). Keep one; code was not generated.");
                return;
            }

            List<string> errors = new();
            List<MapCode> maps = BuildModel(collection, errors);
            if (errors.Count > 0)
            {
                Debug.LogError($"BetterInput: code was not generated for {collectionPath}.\n{string.Join("\n", errors)}", collection);
                return;
            }

            string source = Emit(collectionPath, maps);
            string outputPath = existing.Length == 1
                ? existing[0]
                : $"{Path.GetDirectoryName(collectionPath)?.Replace('\\', '/')}/{FILE_NAME}";
            if (File.Exists(outputPath) && File.ReadAllText(outputPath) == source)
            {
                return;
            }

            File.WriteAllText(outputPath, source);
            AssetDatabase.ImportAsset(outputPath);
        }

        private static List<MapCode> BuildModel(BetterInputActions collection, List<string> errors)
        {
            List<MapCode> maps = new()
            {
                CreateMap("UI", collection.UIMap, "input.UIMap", false, errors),
                CreateMap("Debug", collection.DebugMap, "input.DebugMap", false, errors),
            };
            foreach (DataCollectionElement element in collection.GetAllElements())
            {
                if (element is BetterInputActionsData mapData)
                {
                    maps.Add(CreateMap(mapData.DisplayName, mapData.ActionMap,
                        $"input.GetMap(new Uid({mapData.Id.Value}))", true, errors));
                }
            }

            ReportDuplicates(maps.Select(map => map.Property).Append(ROOT_CLASS), "map", errors);
            ReportDuplicates(maps.Select(map => map.ClassName).Append(ROOT_CLASS), "map class", errors);
            return maps;
        }

        private static MapCode CreateMap(string name, InputActionMap map, string source, bool isGameplay, List<string> errors)
        {
            MapCode code = new() { Name = name, Source = source, IsGameplay = isGameplay };
            code.Property = ToIdentifier(name);
            if (code.Property == null)
            {
                errors.Add($"Map \"{name}\" needs a name with at least one letter or digit.");
                code.Property = "_";
            }
            code.ClassName = code.Property + "Actions";

            if (map != null)
            {
                foreach (InputAction action in map.actions)
                {
                    string property = ToIdentifier(action.name);
                    if (property == null)
                    {
                        errors.Add($"Action \"{action.name}\" in map \"{name}\" needs a name with at least one letter or digit.");
                        continue;
                    }
                    code.Actions.Add((property, action.name));
                }
            }

            IEnumerable<string> members = code.Actions.Select(action => action.Property).Append(code.ClassName);
            if (isGameplay)
            {
                members = members.Append("Map");
            }
            ReportDuplicates(members, $"action in map \"{name}\"", errors);
            return code;
        }

        private static void ReportDuplicates(IEnumerable<string> identifiers, string what, List<string> errors)
        {
            foreach (IGrouping<string, string> group in identifiers.GroupBy(identifier => identifier).Where(group => group.Count() > 1))
            {
                errors.Add($"More than one {what} would be named \"{group.Key}\" in code. Rename one.");
            }
        }

        // "move left" -> "MoveLeft", "2d aim" -> "_2dAim". Returns null if nothing usable is left.
        private static string ToIdentifier(string name)
        {
            StringBuilder builder = new();
            bool capitalizeNext = true;
            foreach (char c in name ?? string.Empty)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    builder.Append(capitalizeNext ? char.ToUpperInvariant(c) : c);
                    capitalizeNext = false;
                }
                else
                {
                    capitalizeNext = true;
                }
            }

            if (builder.Length == 0)
            {
                return null;
            }
            if (char.IsDigit(builder[0]))
            {
                builder.Insert(0, '_');
            }
            return builder.ToString();
        }

        private static string Literal(string value)
        {
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        private static string Emit(string collectionPath, List<MapCode> maps)
        {
            StringBuilder sb = new();
            sb.AppendLine("// <auto-generated>");
            sb.AppendLine($"// Generated by BetterInput from {collectionPath} and rewritten whenever that asset is saved.");
            sb.AppendLine("// Don't edit it by hand. You can move this file anywhere under Assets.");
            sb.AppendLine("// </auto-generated>");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using DatastoresDX.Runtime;");
            sb.AppendLine("using UnityEngine.InputSystem;");
            sb.AppendLine();
            sb.AppendLine("namespace BetterInputSystem.Runtime");
            sb.AppendLine("{");
            sb.AppendLine("    public static class BetterInputGeneratedExtensions");
            sb.AppendLine("    {");
            sb.AppendLine("        private static InputActionAsset s_asset;");
            sb.AppendLine($"        private static {ROOT_CLASS} s_actions;");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>Typed access to every action map in the Better Input collection.</summary>");
            sb.AppendLine($"        public static {ROOT_CLASS} Actions(this BetterInput input)");
            sb.AppendLine("        {");
            sb.AppendLine("            // Keyed on the runtime asset, which is rebuilt on every initialize, so a reset never returns old maps.");
            sb.AppendLine("            // Compared by reference: Unity's == treats a destroyed asset as null.");
            sb.AppendLine("            if (s_actions == null || !ReferenceEquals(s_asset, input.Asset))");
            sb.AppendLine("            {");
            sb.AppendLine($"                s_actions = new {ROOT_CLASS}(input);");
            sb.AppendLine("                s_asset = input.Asset;");
            sb.AppendLine("            }");
            sb.AppendLine("            return s_actions;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine($"    public sealed class {ROOT_CLASS}");
            sb.AppendLine("    {");
            foreach (MapCode map in maps)
            {
                sb.AppendLine($"        public {map.ClassName} {map.Property} {{ get; }}");
            }
            sb.AppendLine();
            sb.AppendLine($"        internal {ROOT_CLASS}(BetterInput input)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (input.Asset == null)");
            sb.AppendLine("            {");
            sb.AppendLine("                throw new InvalidOperationException(\"BetterInput has no actions loaded. Check the error it logged at startup.\");");
            sb.AppendLine("            }");
            sb.AppendLine();
            foreach (MapCode map in maps)
            {
                sb.AppendLine($"            {map.Property} = new {map.ClassName}({map.Source});");
            }
            sb.AppendLine("        }");
            sb.AppendLine("    }");

            foreach (MapCode map in maps)
            {
                sb.AppendLine();
                EmitMapClass(sb, map);
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        private static void EmitMapClass(StringBuilder sb, MapCode map)
        {
            sb.AppendLine($"    public sealed class {map.ClassName}");
            sb.AppendLine("    {");
            if (map.IsGameplay)
            {
                sb.AppendLine("        public BetterActionMap Map { get; }");
            }
            foreach ((string property, string _) in map.Actions)
            {
                sb.AppendLine($"        public InputAction {property} {{ get; }}");
            }
            if (map.IsGameplay || map.Actions.Count > 0)
            {
                sb.AppendLine();
            }

            if (map.IsGameplay)
            {
                sb.AppendLine($"        internal {map.ClassName}(BetterActionMap map)");
                sb.AppendLine("        {");
                sb.AppendLine($"            Map = map ?? throw new InvalidOperationException({Literal($"Input map \"{map.Name}\" wasn't loaded. Save the Better Input collection to regenerate this file.")});");
                foreach ((string property, string actionName) in map.Actions)
                {
                    sb.AppendLine($"            {property} = map.Actions.FindAction({Literal(actionName)}, true);");
                }
            }
            else
            {
                sb.AppendLine($"        internal {map.ClassName}(InputActionMap map)");
                sb.AppendLine("        {");
                foreach ((string property, string actionName) in map.Actions)
                {
                    sb.AppendLine($"            {property} = map.FindAction({Literal(actionName)}, true);");
                }
            }
            sb.AppendLine("        }");
            sb.AppendLine("    }");
        }
    }
}
