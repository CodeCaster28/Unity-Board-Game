using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RealtimeCSG.CSGNode), true)]
[CanEditMultipleObjects]
[System.Reflection.Obfuscation(Exclude = true)]
public class CSGBrushEditor : Editor
{
	public bool HasFrameBounds()			{ return RealtimeCSG.BoundsUtilities.HasFrameBounds(RealtimeCSG.CSGBrushEditorManager.FilteredSelection); }		
	public Bounds OnGetFrameBounds()		{ return RealtimeCSG.BoundsUtilities.OnGetFrameBounds(RealtimeCSG.CSGBrushEditorManager.FilteredSelection); }
	public override void OnInspectorGUI()	{ RealtimeCSG.CSGBrushEditorGUI.OnInspectorGUI(this, this.targets); }
}