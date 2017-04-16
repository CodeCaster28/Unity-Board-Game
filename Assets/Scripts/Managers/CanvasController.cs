using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CanvasController : MonoBehaviour {

	private List<Transform> Controls;
	private string CurrentDiceVal;
	private Transform DiceRoll;
	private Transform DiceRollButton;
	private Transform DiceResult;
	private Transform StartTurnText;
	private Transform TurnButton;

	// Mono Methods

	private void Start() {
		CurrentDiceVal = "00";
		Controls = new List<Transform>();
		foreach (Transform child in transform) {
			Controls.Add(child);
		}

		DiceRoll = Controls.Where(obj => obj.name == "DiceRoll").SingleOrDefault();
		DiceRollButton = Controls.Where(obj => obj.name == "DiceRollButton").SingleOrDefault();
		DiceResult = Controls.Where(obj => obj.name == "DiceResult").SingleOrDefault();
		StartTurnText = Controls.Where(obj => obj.name == "StartTurnText").SingleOrDefault();
		TurnButton = Controls.Where(obj => obj.name == "TurnButton").SingleOrDefault();

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
		if (DiceRollButton.GetComponent<Button>().interactable == false) {
			DiceRollButton.GetComponent<Button>().interactable = true;
			DiceRoll.GetComponent<Animator>().SetTrigger("ResetDice");
		}
	}

	private void ShowResult() {
		CurrentDiceVal = PlayerManager.Master.RollMovementPoints();
		DiceResult.GetComponent<Text>().text = CurrentDiceVal;
		DiceResult.GetComponent<Text>().enabled = true;
		DiceResult.GetComponent<Animator>().SetTrigger("ShowResult");
	}

	private void UpdateResult() {
		string str = CurrentDiceVal;

		if (str.Length == 2) {
			int b = Int32.Parse(str[1].ToString());
			if (b > 1) {
				b--;
				str = (str[0].ToString()) + (b.ToString());
			}
			else {
				str = str[0].ToString();
				DiceResult.GetComponent<Animator>().SetTrigger("Tilt");
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
		DiceResult.GetComponent<Text>().text = str;
	}

	private void ShowNewTurnText() {
		StartTurnText.GetComponent<Animator>().SetTrigger("FadeOut");
	}

	private void NewTurnButtonEnabled() {
		TurnButton.GetComponent<Button>().interactable = true;
	}

	private void NewTurnButtonEnabled(bool enabled) {
		TurnButton.GetComponent<Button>().interactable = enabled;
	}

	// Public Methods (from event system)

	public void HideResult() {		
		AnimatorStateInfo Animation = DiceResult.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0);
		if (Animation.IsName("Result") || Animation.IsName("DiceTilt")) {
			DiceResult.GetComponent<Text>().enabled = false;
			DiceResult.GetComponent<Animator>().SetTrigger("EndTurn");
		}
	}
}
