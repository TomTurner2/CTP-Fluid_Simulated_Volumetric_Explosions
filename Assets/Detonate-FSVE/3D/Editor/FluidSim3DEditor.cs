using UnityEngine;
using UnityEditor;


namespace Detonate
{
    [CustomEditor(typeof(FluidSmoke3D))]
    public class FluidSim3DEditor : Editor
    {
        FluidSmoke3D sim = null;


        public override void OnInspectorGUI()
        {
            GUIStart();

            SimParametersGroup();
            FluidSimModuleGroup();
            OutputGroup();
            InteractablesGroup();
            DebugControlsGroup();

            GUIEnd();
        }


        private void GUIStart()
        {
            serializedObject.Update();
            sim = (FluidSmoke3D)target;
        }


        private void GUIEnd()
        {
            serializedObject.ApplyModifiedProperties();
            SceneView.RepaintAll();//to update draw bounds gizmo
        }


        private void SimParametersGroup()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Simulation Parameters", EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sim_params"), true);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("smoke_params"), true);
            --EditorGUI.indentLevel;
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
        }


        private void FluidSimModuleGroup()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Simulation Modules", EditorStyles.boldLabel);

            ++EditorGUI.indentLevel;//indent as they have an arrow
            DrawModuleProperties();
            --EditorGUI.indentLevel;

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
        }


        private void DrawModuleProperties()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("advection_module"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("divergence_module"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("jacobi_module"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("buoyancy_module"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("impulse_module"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("projection_module"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("obstacle_module"), true);
        }


        private void OutputGroup()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Simulation Output", EditorStyles.boldLabel);

            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("output_module"), true);
            --EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("grid_to_output"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("output_renderer"), true);

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
        }


        private void InteractablesGroup()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Current Interactables", EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("emitters"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sphere_colliders"), true);
            --EditorGUI.indentLevel;
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
        }


        private void DebugControlsGroup()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("Box"); 
            EditorGUILayout.LabelField("Simulation Debug Controls", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("draw_bounds"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("velocity_debug"), true); 
            EditorGUILayout.PropertyField(serializedObject.FindProperty("velocity_debug_resolution"), true); 
            EditorGUILayout.PropertyField(serializedObject.FindProperty("velocity_debug_colour_threshold"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("velocity_debug_normalise"), true);

            if (GUILayout.Button("Reset Simulation") && Application.isPlaying)
            {
                sim.ResetSim();
            }
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
        }
    }
}
