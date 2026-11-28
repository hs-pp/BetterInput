using BetterInputSystem.Runtime;
using DatastoresDX.Editor;
using DatastoresDX.Editor.DataCollections;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace BetterInputSystem.Editor
{
    [System.Serializable]
    [OverviewPanel(typeof(BetterInputActions))]
    public class BetterInputActionsOverviewPanel : AOverviewPanel
    {
        private PropertyField m_uiMapField;
        private PropertyField m_debugMapField;
        private SerializedObject m_collectionSO;

        protected override VisualElement CreatePanel()
        {
            VisualElement root = new VisualElement();

            m_uiMapField = new PropertyField { label = "UI Map" };
            root.Add(m_uiMapField);

            root.Add(new Button(ResetUIMap) { text = "Reset UI Map to Defaults" });

            m_debugMapField = new PropertyField { label = "Debug Map" };
            root.Add(m_debugMapField);

            return root;
        }

        protected override void OnSetWorkflow(AWorkflow workflow)
        {
            m_collectionSO = (workflow as DataCollectionWorkflow).DataCollectionSO;
            m_uiMapField.BindProperty(m_collectionSO.FindProperty(BetterInputActions.UIMap_VarName));
            m_debugMapField.BindProperty(m_collectionSO.FindProperty(BetterInputActions.DebugMap_VarName));
        }

        private void ResetUIMap()
        {
            if (m_collectionSO?.targetObject is not BetterInputActions collection ||
                !EditorUtility.DisplayDialog("Reset UI Map?",
                    "Replace the UI map with the Input System's default UI actions? You can undo this.", "Reset", "Cancel"))
            {
                return;
            }

            // Marked dirty like any other edit here; code regenerates when the collection is saved.
            Undo.RecordObject(collection, "Reset UI Map to Defaults");
            collection.ResetUIMapToDefaults();
            EditorUtility.SetDirty(collection);
            m_collectionSO.Update();
        }
    }
}
