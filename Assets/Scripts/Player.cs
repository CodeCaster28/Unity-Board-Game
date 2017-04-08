using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

	public Color playerColor;
	private Field currentPosition;
	private bool playerIsMoving;
	private Field startPos;
	private Field CurrentDestination;

	void Start () {                         // On Start set position to startpoint
		startPos = GameObject.FindGameObjectWithTag("StartPoint").GetComponent<Field>();
		SetPosition(startPos);
	}

	private void Update() {
		if (playerIsMoving == true) {		// Lineary smooth player go to position
			transform.position = Vector3.Lerp(transform.position, CurrentDestination.transform.position, 0.08f);
		}
	}

	public Field GetPosition() {			// Get current position of this pawn
		return currentPosition.GetComponent<Field>();
	}

	public void SetPosition(Field target) {	// Set current position and unlock animation in update
		currentPosition = target;
		if (playerIsMoving == false) {
			playerIsMoving = true;
			StartCoroutine(CheckMoving());	// Loop checking if player stopped moving, then lock update animation
		}
		CurrentDestination = target;
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
