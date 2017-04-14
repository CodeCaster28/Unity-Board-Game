using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Field : MonoBehaviour{

	public List<Field> adjactedNodes;
	public int ID;
	public float Cost;
	[System.NonSerialized]
	public int PlayersResidingCount = 0;
	[System.NonSerialized]
	public Field Parent;
	private Player[] PlayersResiding = new Player[10];

	// Mono Methods

	private void Start() {
		Parent = null;
		Cost = 1;
	}

	// Used by SearchPath to calculate best path

	public float F {	// To Remove
		get {
			if (Cost != -1)
				return Cost;
			else
				return -1;
		}
	}

	// GUI methods, adds buttons to Field inspector

	public GameObject AddNew() {
		return FieldManager.AddNew(this);
	}

	public void Remove() {
		FieldManager.Remove(this, adjactedNodes);
	}

	// Player segregation

	public Vector3 AddPlayer(Player player) {
		int k = 0;
		while (true) {
			if (PlayersResiding[k] == default(Player)) {
				PlayersResiding[k] = player;
				break;
			}
			k++;
		}

		switch (k) {
			case 0:
				return Vector3.zero;
			case 1:
				return new Vector3(0.25f, 0, 0);
			case 2:
				return new Vector3(-0.25f, 0, 0);
			case 3:
				return new Vector3(0, 0, 0.25f);
			case 4:
				return new Vector3(0, 0, -0.25f);
			case 5:
				return new Vector3(-0.25f, 0, -0.25f);
			case 6:
				return new Vector3(0.25f, 0, -0.25f);
			case 7:
				return new Vector3(0.25f, 0, 0.25f);
			case 8:
				return new Vector3(-0.25f, 0.25f);
			default:
				return Vector3.zero;
		}
	}

	public void DropPlayer(Player player) {
		for(int i=0; i < PlayersResiding.Length; i++) {
			if (PlayersResiding[i] == player) {
				PlayersResiding[i] = default(Player);
				break;
			}
		}
	}
}
