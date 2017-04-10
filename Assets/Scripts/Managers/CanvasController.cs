using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CanvasController : MonoBehaviour {

	private List<Transform> Controls;

	private void Start() {
		Controls = new List<Transform>();
		foreach (Transform child in transform) {
			Controls.Add(child);
		}
		PlayerManager.TurnStarted += ShowNewTurnText;
		PlayerManager.GameBusy += NewTurnButtonEnabled;
	}

	private void ShowNewTurnText() {
		Transform text = Controls.Where(obj => obj.name == "StartTurnText").SingleOrDefault();
		text.GetComponent<Animator>().SetTrigger("FadeOut");
	}

	private void NewTurnButtonEnabled(bool enabled) {
		Debug.Log("Toggled: " + (enabled == true ? "On" : "Off"));
		Transform button = Controls.Where(obj => obj.name == "TurnButton").SingleOrDefault();
		button.GetComponent<Button>().interactable = enabled;
	}

	private void OnDestroy() {
		PlayerManager.TurnStarted -= ShowNewTurnText;
		PlayerManager.GameBusy -= NewTurnButtonEnabled;
	}

}
