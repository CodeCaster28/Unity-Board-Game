using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CanvasController : MonoBehaviour {

	private List<Transform> Controls;
	private List<Transform> Controls2;

	private void Start() {
		Controls = new List<Transform>();
		foreach (Transform child in transform) {
			Controls.Add(child);
		}
		PlayerManager.TurnStarted += ShowNewTurnText;
	}

	private void ShowNewTurnText() {
		Transform text = Controls.Where(obj => obj.name == "StartTurnText").SingleOrDefault();
		text.GetComponent<Animator>().SetTrigger("FadeOut");
	}
	
}
