using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AEDice : MonoBehaviour {

	public delegate void DiceAnimEndDelegate();
	public static event DiceAnimEndDelegate DiceAnimEnd;
	public delegate void DiceResultDelegate();
	public static event DiceResultDelegate DiceAnimResult;

	public void DiceAnimationEnd() {
		if(DiceAnimEnd != null)
			DiceAnimEnd();
	}

	public void DiceAnimationResult() {
		if (DiceAnimResult != null)
			DiceAnimResult();
	}
}
