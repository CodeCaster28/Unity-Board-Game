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
	public void Remove() {        // Add button with method listed in field manager
		FieldManager.Remove(this, adjactedNodes);
	}

	private void Start() {              // Initials for shortest-path finding algorithm
		Parent = null;
		Cost = 1;
	}

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
