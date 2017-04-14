using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CanvasController : MonoBehaviour {

	private List<Transform> Controls;
	private string CurrentDiceVal;

	// Mono Methods

	private void Start() {
		CurrentDiceVal = "00";
		Controls = new List<Transform>();
		foreach (Transform child in transform) {
			Controls.Add(child);
		}
		PlayerManager.TurnStarted += ShowNewTurnText;
		PlayerManager.TurnStarted += ShowDice;
		PlayerManager.GameBusy += NewTurnButtonEnabled;
		PlayerManager.PathTicked += UpdateResult;
		AEDice.DiceAnimEnd += NewTurnButtonEnabled;
		AEDice.DiceAnimResult += ShowResult;
	}

	private void OnDestroy() {
		PlayerManager.TurnStarted -= ShowNewTurnText;
		PlayerManager.TurnStarted -= ShowDice;
		PlayerManager.GameBusy -= NewTurnButtonEnabled;
		PlayerManager.PathTicked -= UpdateResult;
		AEDice.DiceAnimEnd -= NewTurnButtonEnabled;
		AEDice.DiceAnimResult -= ShowResult;
	}

	// Private Methods

	private void ShowDice () {
		Transform image = Controls.Where(obj => obj.name == "DiceRoll").SingleOrDefault();
		Transform button = Controls.Where(obj => obj.name == "DiceRollButton").SingleOrDefault();
		if (button.GetComponent<Button>().interactable == false) {
			button.GetComponent<Button>().interactable = true;
			image.GetComponent<Animator>().SetTrigger("ResetDice");
		}
	}

	private void ShowResult() {
		Transform text = Controls.Where(obj => obj.name == "DiceResult").SingleOrDefault();
		CurrentDiceVal = PlayerManager.Master.RollMovementPoints();
		text.GetComponent<Text>().text = CurrentDiceVal;
		text.GetComponent<Text>().enabled = true;
		text.GetComponent<Animator>().SetTrigger("ShowResult");
	}

	private void UpdateResult() {
		Transform text = Controls.Where(obj => obj.name == "DiceResult").SingleOrDefault();
		string str = CurrentDiceVal;

		if (str.Length == 2) {
			int b = Int32.Parse(str[1].ToString());
			if (b > 1) {
				b--;
				str = (str[0].ToString()) + (b.ToString());
			}
			else {
				str = str[0].ToString();
					text.GetComponent<Animator>().SetTrigger("Tilt");
			}
		}
		else if (str.Length == 1) {

			int a = Int32.Parse(str[0].ToString());
			if (a > 1) {
				a--;
				str = a.ToString();
			}
			else str = "";
		}
		CurrentDiceVal = str;
		text.GetComponent<Text>().text = str;
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

	/*public void HideResult() {
		Transform text = Controls.Where(obj => obj.name == "DiceResult").SingleOrDefault();
		AnimatorStateInfo Animation = text.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0);
		if (Animation.IsName("Result") || Animation.IsName("DiceTilt")) {
			text.GetComponent<Text>().enabled = false;
			text.GetComponent<Animator>().SetTrigger("EndTurn");
		}
	}*/
}
