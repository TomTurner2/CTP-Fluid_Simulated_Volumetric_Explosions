using System.Collections;
using System.Collections.Generic;
using Detonate;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(FluidSim3D))]
public class FluidSim3DEditor : Editor
{
    bool draw_bounds = false;
    FluidSim3D sim = null;


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
        sim = (FluidSim3D)target;
    }


    private void GUIEnd()
    {
        serializedObject.ApplyModifiedProperties();
        SceneView.RepaintAll();
    }


    private void SimParametersGroup()
    {
        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField("Simulation Parameters", EditorStyles.boldLabel);
        ++EditorGUI.indentLevel;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("sim_params"), true);
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
        draw_bounds = EditorGUILayout.ToggleLeft(new GUIContent("Draw Bounds"), draw_bounds);
        if (GUILayout.Button("Reset Simulation") && Application.isPlaying)
        {
            sim.ResetSim();
        }
        EditorGUILayout.Space();
        EditorGUILayout.EndVertical();
    }


    public void OnSceneGUI()
    {
        if (!draw_bounds)
            return;

        Handles.color = Color.cyan;
        Handles.DrawWireCube(sim.transform.position, sim.transform.localScale);
    }
}
