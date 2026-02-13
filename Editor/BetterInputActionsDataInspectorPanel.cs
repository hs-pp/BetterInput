using BetterInputSystem.Runtime;
using DatastoresDX.Editor;
using DatastoresDX.Editor.DataCollections;
using UnityEngine.UIElements;

namespace BetterInputSystem.Editor
{
    [InspectorPanel(typeof(BetterInputActionsData))]
    public class BetterInputActionsDataInspectorPanel : AInspectorPanel
    {
        private DefaultDataCollectionInspectorPanel m_defaultPanel;
        
        protected override VisualElement CreatePanel()
        {
            VisualElement root = new VisualElement();
            
            m_defaultPanel = new DefaultDataCollectionInspectorPanel();
            root.Add(m_defaultPanel.GetPanel());
            
            Button button = new Button();
            button.text = "Regenerate Code";
            button.clicked += () =>
            {
                BetterInputActionsSourceGenerator.GenerateAndSaveCode();
            };
            root.Add(button);
            
            return root;
        }

        protected override void OnSetElement(WorkflowElementKey elementKey)
        {
            DataCollectionElementWrapper element = elementKey.GetElement() as DataCollectionElementWrapper;
            if (element == null)
            {
                return;
            }
            
            m_defaultPanel.SetElement(elementKey);
        }
    }
}