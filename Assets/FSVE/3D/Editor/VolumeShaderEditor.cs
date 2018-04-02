using UnityEditor;


public class VolumeShaderEditor : MaterialEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (!isVisible)// Only show cheat sheet 
            return;
        
        EditorGUILayout.HelpBox("Standard Blend Combinations:\n" +
            "Normal:\t\tSrcAlpha OneMinusSrcAlpha\n" +
            "Soft Additive:\tOneMinusDstColor One\n" +
            "Multiply:\t\tDstColor Zero\n" +
            "2x Multiply:\tDstColor SrcColor\n" +
            "Screen:\t\tOneMinusDstColor One\n" +
            "Screen (alt):\tOne OneMinusSrcColor\n" +
            "Linear Dodge:\tOne One\n", MessageType.Info);
    }
}

