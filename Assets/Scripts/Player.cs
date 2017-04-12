using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

	public Color playerColor;
	private Field Position;
	private bool playerIsMoving;
	private Field startPos;
	private Field Destination;
	private Vector3 Offset;
	private List<Field> CurrentPath;

	void Start () {                         // On Start set position to startpoint
		CurrentPath = null;
		Offset = Vector3.zero;
		startPos = GameObject.FindGameObjectWithTag("StartPoint").GetComponent<Field>();
		SetPosition(startPos);
	}

	private void Update() {
		if (playerIsMoving == true) {		// Lineary smooth player go to position
			transform.position = Vector3.Lerp(transform.position, Destination.transform.position + Offset, 0.08f);
		}
	}

	public Field GetPosition() {			// Get current position of this pawn
		return Position.GetComponent<Field>();
	}

	public void SetPath(List<Field> path) {            // Get current position of this pawn
		CurrentPath = path;
	}

	public List<Field> GetPath() {            // Get current position of this pawn
		return CurrentPath;
	}

	public void SetPosition(Field target) { // Set current position and unlock animation in update

		if (Position != null) {
			Position.DropPlayer(this);		// Remove player from old position
			Position.PlayersResidingCount--;
		}
		Position = target;					// New position is a target
		Offset = Position.AddPlayer(this);	// Add player to new position
		Position.PlayersResidingCount++;
		if (playerIsMoving == false) {
			playerIsMoving = true;
			StartCoroutine(CheckMoving());	// Loop checking if player stopped moving, then lock update animation
		}
		
		Destination = target;

	}

	private IEnumerator CheckMoving() {
		Vector3 startPos;
		Vector3 finalPos;
		do {
			startPos = transform.position;
			yield return new WaitForSeconds(0.2f);
			finalPos = transform.position;
			if (startPos == finalPos) {
				playerIsMoving = false;
			}
			else {
			}
		}
		while (startPos != finalPos);
	}
}
