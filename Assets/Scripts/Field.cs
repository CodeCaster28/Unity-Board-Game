using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Field : MonoBehaviour{

	public List<Field> adjactedNodes;
	public int ID;
	public float Cost;

	[System.NonSerialized]
	public Field Parent;

	public float F {	// To Remove
		get {
			if (Cost != -1)
				return Cost;
			else
				return -1;
		}
	}

	public GameObject AddNew() {		// Add button with method listed in field manager
		return FieldManager.AddNew(this);
	}

	private void Start() {				// Initials for shortest-path finding algorithm
		Parent = null;
		Cost = 1;
	}
}
