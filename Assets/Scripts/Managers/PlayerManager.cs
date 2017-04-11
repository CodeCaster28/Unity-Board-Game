using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerManager : GenericSingletonClass<PlayerManager>
{
	public delegate void TurnStartDelegate();
	public static event TurnStartDelegate TurnStarted;
	public delegate void GameBusyDelegate(bool enabled);
	public static event GameBusyDelegate GameBusy;
	// public delegate void PathWalkStartedDelegate();
	// public static event PathWalkStartedDelegate PathWalkStarted;
	public delegate void PathTickedDelegate();
	public static event PathTickedDelegate PathTicked;

	public List<Player> Players;
	private static Player CurrentPlayer;
	private static List<Field> CurrentPath;
	private static int PlayerIndex;
	private static bool IsSubscribedStartWalk;
	private int MovementPoints;
	private int PlayerCount;

	void Start () {
		// AEDice.DiceAnimResult += RollMovementPoints;
		PlayerCount = Players.Count;
		PlayerIndex = 0;
		TurnStart();
	}

	// Turn Chain

	private void TurnStart() {
		MovementPoints = -1;		// Not rolled yet

		if (TurnStarted != null) {
			TurnStarted();
		}
		CycleNextPlayer();
		FieldManager.Master.ClearAllFields();
		TunrDiceRoll();
	}

	private void CycleNextPlayer() {
		CurrentPlayer = Players[PlayerIndex];
		PlayerIndex++;
		if (PlayerIndex > Players.Count - 1) {
			PlayerIndex = 0;
		}
	}

	private void TunrDiceRoll() {   // Wait for dice roll
		AEDice.DiceAnimEnd += TurnPathMark;
	}

	private void TurnPathMark() {           // Marking path stage
		AEDice.DiceAnimEnd -= TurnPathMark;
		IsSubscribedStartWalk = false;
		InputManager.MousePressed += TurnPathMarked;
	}

	private void TurnPathMarked(Field targetField) {	// Wait for confirmation stage (path can be redefinied)
		FieldManager.Master.MarkPath(targetField, MovementPoints);
		if (CurrentPath != null && IsSubscribedStartWalk == false) {    // Before allowing to go down this path, check if it's correct
			IsSubscribedStartWalk = true;
			InputManager.KeySpace += TurnPathStartWalk;
		}
	}
	
	private void TurnPathStartWalk () {     // Path travel stage, animate movement (can't mark new path/start new walk)
		// if (PathWalkStarted != null)
		//	PathWalkStarted();
		if (GameBusy != null)
			GameBusy(false);
		InputManager.MousePressed -= TurnPathMarked;
		InputManager.KeySpace -= TurnPathStartWalk;
		List<Field> walkablePath = new List<Field>();
		walkablePath = CurrentPath.Take(MovementPoints).ToList();	// Travel only walkable path
		StartCoroutine(PathTick(walkablePath));
	}

	private IEnumerator PathTick(List<Field> path) {

		float movementSpeed = path.Count < 4 ? 0.325f : 1.30f / path.Count;

		for (int i = 0; i < path.Count; i++) {
			MovementPoints--;
			if (PathTicked != null)
				PathTicked();
			CurrentPlayer.SetPosition(path[i]);
			yield return new WaitForSeconds(movementSpeed);
			FieldManager.Master.ClearField(path[i]);
		}

		CurrentPath = null;     // Remove current path from memory, so it has to be marked again in new turn
		if (GameBusy != null) {
			GameBusy(true);
		}
		TurnPathMark();
		yield return null;
	}

	// Global Methods
	
	public void EndTurn() {
		InputManager.MousePressed -= TurnPathMarked;
		InputManager.KeySpace -= TurnPathStartWalk;
		CurrentPath = null;
		TurnStart();
	}

	public static Player GetCurrentPlayer() {
		return CurrentPlayer;
	}

	public int GetPlayerCount() {
		return PlayerCount;
	}

	public static void SetCurrentPath(List<Field> path) {
		CurrentPath = path;
	}

	public string RollMovementPoints() {
		int a = Random.Range(1, 7);
		int b = Random.Range(1, 7);
		MovementPoints = a + b;
		return a.ToString() + b.ToString();
	}

	// Destroy

	private void OnDestroy() {
		// AEDice.DiceAnimResult -= RollMovementPoints;
	}
}