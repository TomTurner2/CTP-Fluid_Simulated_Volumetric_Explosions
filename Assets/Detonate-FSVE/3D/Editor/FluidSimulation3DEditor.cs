using UnityEditor;
using UnityEngine;


namespace Detonate
{
    public class FluidSimulation3DEditor : Editor
    {
        protected virtual void GUIStart()
        {
            serializedObject.Update();
        }


        protected void GUIEnd()
        {
            serializedObject.ApplyModifiedProperties();//apply the changed properties
            SceneView.RepaintAll();//to update draw bounds gizmo
        }


        protected void StartGroup(string _group_name)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("Box");//create a box field in the inspector
            EditorGUILayout.LabelField(_group_name, EditorStyles.largeLabel);
            EditorGUILayout.Space();
        }


        protected void EndGroup()
        {
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();//end current box field
        }


        protected void DrawFluidSimModuleGroup()
        {
            StartGroup("Simulation Modules");

            ++EditorGUI.indentLevel;//indent as they have an arrow
            DrawModuleProperties();
            --EditorGUI.indentLevel;

            EndGroup();
        }


        protected virtual void DrawModuleProperties()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("advection_module"), true);//display all modules
            EditorGUILayout.PropertyField(serializedObject.FindProperty("divergence_module"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("jacobi_module"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("buoyancy_module"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("impulse_module"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("projection_module"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("obstacle_module"), true);
        }


        protected void DrawOutputGroup()
        {
            StartGroup("Simulation Output");

            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("output_module"), true);//has arrow so it is indented
            --EditorGUI.indentLevel;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("grid_to_output"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("output_renderer"), true);

            EndGroup();
        }


        protected virtual void DrawDebugControlsGroup(FluidSimulation3D _sim)
        {
            StartGroup("Simulation Debug Controls");

            EditorGUILayout.PropertyField(serializedObject.FindProperty("draw_bounds"), true);//debug paramaters to display
            EditorGUILayout.PropertyField(serializedObject.FindProperty("velocity_debug"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("velocity_debug_resolution"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("velocity_debug_colour_threshold"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("velocity_debug_normalise"), true);
            if (GUILayout.Button("Reset Simulation") && Application.isPlaying)//button for reseting simulation
            {
                _sim.ResetSim();
            }

            EndGroup();
        }


        protected virtual void DrawSimParametersGroup()
        {
            StartGroup("Simulation Parameters");
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sim_params"), true);
            --EditorGUI.indentLevel;
            EndGroup();
        }


        protected void DrawBaseInteractables()
        {          
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sphere_colliders"), true);
            --EditorGUI.indentLevel;
        }

    }
}
