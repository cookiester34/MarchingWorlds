using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(Chunk))]
public class GridEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();


        if (GUILayout.Button("GenerateGrid"))
        {
        }
    }
}
