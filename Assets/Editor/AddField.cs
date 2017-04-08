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
	}
	/*public void OnDestroy() {   //Run method RemoveReferences when object is deleted
		Field myScript = (Field)target;
		myScript.RemoveReferences();
		if (Application.isEditor) {
			if (((Field)target) == null) {
				Debug.Log("Field node deleted.");
			}
		}
	}*/
}
