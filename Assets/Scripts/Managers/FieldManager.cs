using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FieldManager : GenericSingletonClass<FieldManager> 
{
	private List<Field> gameFields;
	public Material Dot;            // Path "dot" material to display on marked path
	public Material Cross;          // Path "cross" material to display on last field in marked path

	// Mono Methods

	void Start() {
		gameFields = GetComponentsInChildren<Field>().ToList();      // Store all fields
		ClearAllFields();
		Initialize();
	}

	// Check if all fields haveno empty adjacted nodes

	void Initialize() {
		foreach (Field field in gameFields) {
			foreach (Field adjacted in field.adjactedNodes) {
				if (adjacted == null) {
					Debug.Log("Missing Adjacted Node in " + field.gameObject.name + ", the game won't work properly!");
				}
			}
		}
	}

	// Global Methods

	private Field currentPosition;

	public List<Field> MarkPath(Field clickedField, int movementPoints) {
		currentPosition = PlayerManager.GetCurrentPlayer().GetPosition();
		if (!(currentPosition == clickedField))
			return HighlightPath(currentPosition, movementPoints, clickedField);
		else return null;
	}

	// Private Methods

	private List<Field> HighlightPath(Field Start, int movementPoints, Field End) {

		List<Field> path = SearchPath.FindPath(Start, End).ToList();
		List<Field> walkablePath = null;
		if (movementPoints != 0) {
			walkablePath = path.Take(movementPoints).ToList();
		}
		ClearAllFields();
		foreach (Field field in path) {
			ShowField(field);
		}
		if (walkablePath != null) {
			foreach (Field field in walkablePath) {
				HighlightField(field);
			}
		}
		CrossField(path[path.Count - 1]);
		if (walkablePath != null)
			CrossFieldWalkable(walkablePath[walkablePath.Count - 1]);
		return path;
	}
	
	// Field highlighting methods

	public void ClearField(Field field) {
		field.GetComponent<MeshRenderer>().material = Dot;
		field.GetComponent<MeshRenderer>().material.color = Color.clear;
	}

	public void ClearAllFields(List<Field> path) {
		foreach (Field field in path) {
			ClearField(field);
		}
	}

	public void ClearAllFields() {
		foreach (Field field in gameFields) {
			ClearField(field);
		}
	}

	private void ShowField(Field field) {
		field.GetComponent<MeshRenderer>().material.color = Color.white;
	}

	private void HighlightField(Field field) {
		field.GetComponent<MeshRenderer>().material.color = PlayerManager.GetCurrentPlayer().playerColor;
	}

	private void CrossFieldWalkable(Field field) {
		field.GetComponent<MeshRenderer>().material = Cross;
		field.GetComponent<MeshRenderer>().material.color = PlayerManager.GetCurrentPlayer().playerColor;
	}

	private void CrossField(Field field) {
		field.GetComponent<MeshRenderer>().material = Cross;
		field.GetComponent<MeshRenderer>().material.color = Color.white;
	}

	// Editor GUI button function, called from each field
	// Allows to create new path from editor inspector, assigning name, id and adjacencies automatically (very handy)
	public static GameObject AddNew(Field source) {
		List<string> childNames = new List<string>();
		Field newField = Instantiate(source);
		newField.transform.parent = source.transform.parent;
		newField.transform.position = source.transform.position;
		int nextNameNumber = 0;
		string tempName = "Field";

		foreach (Transform child in source.transform.parent) {
			childNames.Add(child.name);
		}
		while (childNames.Contains(tempName)) {
			nextNameNumber++;
			tempName = "Field (" + nextNameNumber + ")";
		}

		newField.name = tempName;
		newField.ID = nextNameNumber;
		newField.adjactedNodes.Clear();
		newField.adjactedNodes.Add(source);
		source.adjactedNodes.Add(newField);
		return newField.gameObject;
	}

	// Editor GUI button function, called from each field
	// Allows to delete field and remove any references to it from adjacted fields
	public static void Remove(Field caller, List<Field> adjactedNodes) {

		for (int i = 0; i < adjactedNodes.Count; i++) {
			for (int j = 0; j < adjactedNodes[i].adjactedNodes.Count; j++) {
				if (adjactedNodes[i].adjactedNodes[j].name == caller.gameObject.transform.name) {
					Debug.Log("Removed adjacted node [" + adjactedNodes[i].adjactedNodes[j].name + "] of " + caller.gameObject.transform.name);
					adjactedNodes[i].adjactedNodes.RemoveAt(j);
					
				}
			}
		}
		DestroyImmediate(caller.gameObject);
	}
}
