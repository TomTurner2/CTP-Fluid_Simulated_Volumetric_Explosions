using UnityEditor;
using UnityEngine;


namespace FSVE
{
    [CustomEditor(typeof(VolumeRenderer))]
    public class VolumeRendererEditor : Editor
    {
        VolumeRenderer renderer;

        private void GUIStart()
        {
            serializedObject.Update();
            renderer = (VolumeRenderer)target;
        }


        private void GUIEnd()
        {
            serializedObject.ApplyModifiedProperties();//apply the changed properties
        }


        public override void OnInspectorGUI()
        {
            GUIStart();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("texture"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("randomise_colour"), true);

            if (renderer.randomise_colour && Application.isPlaying)// If randomise colour is enabled
            {
                if (GUILayout.Button("Randomise Colour"))// Add a color re-roll button
                    renderer.RandomiseColour();
            }
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("on_colour_change"), true);
            GUIEnd();
        }

    }
}
