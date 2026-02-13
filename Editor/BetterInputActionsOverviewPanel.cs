using BetterInputSystem.Runtime;
using DatastoresDX.Editor;
using DatastoresDX.Editor.DataCollections;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace BetterInputSystem.Editor
{
    [OverviewPanel(typeof(BetterInputActions))]
    public class BetterInputActionsOverviewPanel : AOverviewPanel
    {
        protected override VisualElement CreatePanel()
        {
            return new BetterInputActionsOverviewElement();
        }

        protected override void OnSetWorkflow(AWorkflow workflow)
        {
            (m_panel as BetterInputActionsOverviewElement).SetBetterInputActions(workflow as DataCollectionWorkflow);
        }
    }

    public class BetterInputActionsOverviewElement : VisualElement
    {
        private SerializedObject m_betterInputActionsSO;

        private PropertyField m_debugActionMapPropertyField;
        private Button m_generateCodeButton;

        public BetterInputActionsOverviewElement()
        {
            CreateLayout();
        }

        private void CreateLayout()
        {
            m_debugActionMapPropertyField = new PropertyField();
            m_debugActionMapPropertyField.style.paddingBottom = 8;
            Add(m_debugActionMapPropertyField);
            
            m_generateCodeButton = new Button();
            m_generateCodeButton.text = "Regenerate Code";
            Add(m_generateCodeButton);

            m_generateCodeButton.clicked += () =>
            {
                BetterInputActionsSourceGenerator.GenerateAndSaveCode();
            };
        }
        
        public void SetBetterInputActions(DataCollectionWorkflow workflow)
        {
            m_betterInputActionsSO = workflow.DataCollectionSO;

            m_debugActionMapPropertyField.BindProperty(
                m_betterInputActionsSO.FindProperty(BetterInputActions.DebugActionMap_VarName));
        }
    }
}