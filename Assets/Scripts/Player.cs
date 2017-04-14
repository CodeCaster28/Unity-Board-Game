using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

	public Color playerColor;

	private bool playerIsMoving;
	private Field startPos;
	private Field Destination;
	private Vector3 Offset;
	private List<Field> CurrentPath;

	// Mono Methods

	private void Start () {
		CurrentPath = null;
		Offset = Vector3.zero;
		startPos = GameObject.FindGameObjectWithTag("StartPoint").GetComponent<Field>();
		SetPosition(startPos);
	}

	private void Update() {
		if (playerIsMoving == true) {
			transform.position = Vector3.Lerp(transform.position, Destination.transform.position + Offset, 0.08f);
		}
	}

	// Global Methods

	private Field Position;

	public Field GetPosition() {
		return Position.GetComponent<Field>();
	}

	public void SetPosition(Field target) {

		if (Position != null) {
			Position.DropPlayer(this);
			Position.PlayersResidingCount--;
		}
		Position = target;
		Offset = Position.AddPlayer(this);
		Position.PlayersResidingCount++;
		if (playerIsMoving == false) {
			playerIsMoving = true;
			StartCoroutine(CheckMoving());
		}

		Destination = target;

	}

	public void SetPath(List<Field> path) {
		CurrentPath = path;
	}

	public List<Field> GetPath() {
		return CurrentPath;
	}

	// Private Methods

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
