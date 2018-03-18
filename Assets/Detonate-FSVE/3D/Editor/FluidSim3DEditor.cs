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
            DrawCustomInspector();
            GUIEnd();
        }


        private void DrawCustomInspector()
        {
            DrawSimParametersGroup();
            DrawFluidSimModuleGroup();
            DrawOutputGroup();
            DrawInteractablesGroup();
            DrawDebugControlsGroup();
        }


        private void GUIStart()
        {
            serializedObject.Update();
            sim = (FluidSmoke3D)target;//get targeted fluid sim
        }


        private void GUIEnd()
        {
            serializedObject.ApplyModifiedProperties();//apply the changed properties
            SceneView.RepaintAll();//to update draw bounds gizmo
        }


        private void StartGroup(string _group_name)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("Box");//create a box field in the inspector
            EditorGUILayout.LabelField(_group_name, EditorStyles.boldLabel);
        }


        private void EndGroup()
        {
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();//end current box field
        }


        private void DrawSimParametersGroup()
        {
            StartGroup("Simulation Parameters");

            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sim_params"), true);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("smoke_params"), true);
            --EditorGUI.indentLevel;

            EndGroup();
        }


        private void DrawFluidSimModuleGroup()
        {
            StartGroup("Simulation Modules");

            ++EditorGUI.indentLevel;//indent as they have an arrow
            DrawModuleProperties();
            --EditorGUI.indentLevel;

            EndGroup();
        }


        private void DrawModuleProperties()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("advection_module"), true);//display all modules
            EditorGUILayout.PropertyField(serializedObject.FindProperty("divergence_module"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("jacobi_module"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("buoyancy_module"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("impulse_module"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("projection_module"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("obstacle_module"), true);
        }


        private void DrawOutputGroup()
        {
            StartGroup("Simulation Output");

            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("output_module"), true);//has arrow so it is indented
            --EditorGUI.indentLevel;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("grid_to_output"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("output_renderer"), true);

            EndGroup();
        }


        private void DrawInteractablesGroup()
        {
            StartGroup("Current Interactables");

            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("emitters"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sphere_colliders"), true);
            --EditorGUI.indentLevel;

            EndGroup();
        }


        private void DrawDebugControlsGroup()
        {
            StartGroup("Simulation Debug Controls");

            EditorGUILayout.PropertyField(serializedObject.FindProperty("draw_bounds"), true);//debug paramaters to display
            EditorGUILayout.PropertyField(serializedObject.FindProperty("velocity_debug"), true); 
            EditorGUILayout.PropertyField(serializedObject.FindProperty("velocity_debug_resolution"), true); 
            EditorGUILayout.PropertyField(serializedObject.FindProperty("velocity_debug_colour_threshold"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("velocity_debug_normalise"), true);
            if (GUILayout.Button("Reset Simulation") && Application.isPlaying)//button for reseting simulation
            {
                sim.ResetSim();
            }

            EndGroup();
        }
    }
}
