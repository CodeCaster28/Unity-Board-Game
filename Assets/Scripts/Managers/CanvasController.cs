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
		PlayerManager.TurnStarted += ShowDice;
		PlayerManager.GameBusy += NewTurnButtonEnabled;
		AEDice.DiceAnimEnd += NewTurnButtonEnabled;
	}

	private void ShowDice () {
		Transform image = Controls.Where(obj => obj.name == "DiceRoll").SingleOrDefault();
		Transform button = Controls.Where(obj => obj.name == "DiceRollButton").SingleOrDefault();

		if (button.GetComponent<Button>().interactable == false) {
			button.GetComponent<Button>().interactable = true;
			image.GetComponent<Animator>().SetTrigger("ResetDice");
		}
	}

	private void ShowNewTurnText() {
		Transform text = Controls.Where(obj => obj.name == "StartTurnText").SingleOrDefault();
		text.GetComponent<Animator>().SetTrigger("FadeOut");
	}

	private void NewTurnButtonEnabled() {
		Transform button = Controls.Where(obj => obj.name == "TurnButton").SingleOrDefault();
		button.GetComponent<Button>().interactable = true;
	}

	private void NewTurnButtonEnabled(bool enabled) {
		Transform button = Controls.Where(obj => obj.name == "TurnButton").SingleOrDefault();
		button.GetComponent<Button>().interactable = enabled;
	}

	private void OnDestroy() {
		PlayerManager.TurnStarted -= ShowNewTurnText;
		PlayerManager.GameBusy -= NewTurnButtonEnabled;
	}

}
