using UnityEditor;


namespace FSVE
{
    [CustomEditor(typeof(FluidSmoke3D))]
    public class FluidSmoke3DEditor : FluidSimulation3DEditor
    {
        FluidSmoke3D sim = null;


        public override void OnInspectorGUI()
        {
            GUIStart();
            DrawCustomInspector();
            GUIEnd();
        }


        private void DrawCustomInspector()
        {
            DrawSimParametersGroup();
            DrawFluidSimModuleGroup();
            DrawOutputGroup();
            DrawInteractablesGroup();
            DrawDebugControlsGroup(sim);// Debug may need sim functions
        }


        protected override void GUIStart()
        {
            base.GUIStart();
            sim = (FluidSmoke3D)target;// Get targeted fluid sim
        }


        protected override void DrawSimParametersGroup()
        {
            base.DrawSimParametersGroup();

            StartGroup("Smoke Parameters");

            ++EditorGUI.indentLevel;        
            EditorGUILayout.PropertyField(serializedObject.FindProperty("smoke_params"), true);
            --EditorGUI.indentLevel;

            EndGroup();
        }


        private void DrawInteractablesGroup()
        {
            StartGroup("Current Interactables");

            DrawBaseInteractables();// Draw interactable common to all fluid sims

            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("smoke_emitters"), true);
            --EditorGUI.indentLevel;

            EndGroup();
        }

    }
}
