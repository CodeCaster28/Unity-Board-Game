﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FieldManager : GenericSingletonClass<FieldManager> 
{
	private Field[] gameFields;
	private Field currentPosition;

	public Material Dot;            // Path "dot" material to display on marked path
	public Material Cross;          // Path "cross" material to display on last field in marked path

	void Start() {
		gameFields = GetComponentsInChildren<Field>();		// Store all fields
		foreach (Field field in gameFields) {
			ClearField(field);                              // Clear fields (make unmarked)
		}
	}

	// Global Methods

	public void MarkPath(Field clickedField) {
		currentPosition = PlayerManager.GetCurrentPlayer().GetPosition();
		if (!(currentPosition == clickedField))
			PlayerManager.SetCurrentPath(HighlightPath(currentPosition, clickedField));
	}

	private List<Field> HighlightPath(Field Start, Field End) {

		List<Field> path = SearchPath.FindPath(Start, End).ToList();
		foreach (Field field in gameFields) {
			ClearField(field);
		}
		foreach (Field field in path) {
			HighlightField(field);
		}
		CrossField(path[path.Count - 1]);
		return path;
	}

	// Used by HighlightPath(), clear field also used by Start() and PlayerManager

	public void ClearField(Field field) {
		field.GetComponent<MeshRenderer>().material = Dot;
		field.GetComponent<MeshRenderer>().material.color = Color.clear;
	}

	private void HighlightField(Field field) {
		field.GetComponent<MeshRenderer>().material.color = PlayerManager.GetCurrentPlayer().playerColor;
	}

	private void CrossField(Field field) {
		field.GetComponent<MeshRenderer>().material = Cross;
		field.GetComponent<MeshRenderer>().material.color = PlayerManager.GetCurrentPlayer().playerColor;
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

	// TODO: add remove feature which will clear this field from other's fields adjacted list
	/*public void RemoveReferences() {
		for (int i = 0; i < adjactedNodes.Count; i++) {
			for (int j = 0; j < adjactedNodes[i].adjactedNodes.Count; j++) {
				if (adjactedNodes[i].adjactedNodes[j] == null) {
					adjactedNodes[i].adjactedNodes.RemoveAt(j);
				}
			}
		}
	}*/
}