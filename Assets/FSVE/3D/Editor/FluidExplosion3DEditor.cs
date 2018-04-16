using UnityEngine;
using UnityEditor;


namespace FSVE
{
    [CustomEditor(typeof(FluidExplosion3D))]
    public class FluidExplosion3DEditor : FluidSimulation3DEditor
    {
        FluidExplosion3D sim = null;


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
            DrawDebugControlsGroup(sim);
        }


        protected override void GUIStart()
        {
            base.GUIStart();
            sim = (FluidExplosion3D)target;
        }


        protected override void DrawOutputParameters()
        {
            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("output_resolution"), true);// Explosion can have different output resolution
            --EditorGUI.indentLevel;
            EditorGUI.EndDisabledGroup();
            base.DrawOutputParameters();
        }


        protected override void DrawSimParametersGroup()
        {
            base.DrawSimParametersGroup();

            StartGroup("Explosion Parameters");

            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("explosion_params"), true);
            --EditorGUI.indentLevel;

            EndGroup();
        }


        protected override void DrawModuleProperties()
        {
            base.DrawModuleProperties();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fuel_particle_module"), true);
        }


        private void DrawInteractablesGroup()
        {
            StartGroup("Current Interactables");
            DrawBaseInteractables();// Draw interactable common to all fluid sims
            EndGroup();
        }

    }
}
