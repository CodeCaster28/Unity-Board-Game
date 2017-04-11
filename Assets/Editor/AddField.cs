using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Field))]
public class AddField : Editor {

	public override void OnInspectorGUI() {
		DrawDefaultInspector();
		Field myScript = (Field)target;
		if (GUILayout.Button("Add Field")) {
			Selection.activeGameObject = myScript.AddNew();
		}
		Field myScript2 = (Field)target;
		if (GUILayout.Button("Remove Field")) {
			myScript2.Remove();
			GUIUtility.ExitGUI();
		}
	}
}
