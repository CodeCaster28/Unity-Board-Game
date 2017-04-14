using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerManager : GenericSingletonClass<PlayerManager> {
	public delegate void TurnStartDelegate();
	public static event TurnStartDelegate TurnStarted;
	public delegate void GameBusyDelegate(bool enabled);
	public static event GameBusyDelegate GameBusy;
	public delegate void PathTickedDelegate();
	public static event PathTickedDelegate PathTicked;

	public List<Player> Players;
	private static Player CurrentPlayer;
	private static List<Field> CurrentPath;
	private static int PlayerIndex;
	private static bool IsSubscribedStartWalk;
	private int MovementPoints;
	private int PlayerCount;
	private bool WaitForStop;

	// Mono Methods

	void Start() {
		WaitForStop = false;
		PlayerCount = Players.Count;
		PlayerIndex = 0;
		TurnStart();
	}

	// Turn Chain

	private void TurnStart() {

		MovementPoints = 0;        // Not rolled yet
		if (TurnStarted != null)
			TurnStarted();
		CycleNextPlayer();
		FieldManager.Master.ClearAllFields();
		if (CurrentPlayer.GetPath() != null)
			CurrentPath = FieldManager.Master.MarkPath(CurrentPlayer.GetPath().Last(), 0);
		TunrDiceRoll();
	}

	private void TunrDiceRoll() {   // Wait for dice roll, load previously selected path (if any)
		AEDice.DiceAnimEnd += TurnPathMark;

	}

	private void TurnPathMark() {           // Marking path stage

		IsSubscribedStartWalk = false;
		if (CurrentPath != null)
			TurnPathMarked(CurrentPath.Last());
		InputManager.MouseClickField += TurnPathMarked;
	}

	private void TurnPathMarked(Field targetField) {    // Wait for confirmation stage (path can be redefinied)

		CurrentPath = FieldManager.Master.MarkPath(targetField, MovementPoints);
		if (CurrentPath != null && IsSubscribedStartWalk == false) {    // Before allowing to go down this path, check if it's correct
			IsSubscribedStartWalk = true;
			InputManager.KeySpace += TurnPathStartWalk;
		}
	}

	private void TurnPathStartWalk() {     // Path travel stage, animate movement (can't mark new path/start new walk)

		List<Field> walkablePath = new List<Field>();

		if (GameBusy != null)
			GameBusy(false);
		InputManager.MouseClickField -= TurnPathMarked;
		InputManager.KeySpace -= TurnPathStartWalk;

		walkablePath = CurrentPath.Take(MovementPoints).ToList();   // Travel only walkable path
		StartCoroutine(PathTick(walkablePath));
	}

	// Private Methods

	private void CycleNextPlayer() {

		CurrentPlayer = Players[PlayerIndex];
		PlayerIndex++;
		if (PlayerIndex > Players.Count - 1)
			PlayerIndex = 0;
	}

	private IEnumerator PathTick(List<Field> path) {

		float movementSpeed = path.Count < 4 ? 0.325f : 1.30f / path.Count;
		InputManager.MousePressed += PathStopWalk;
		for (int i = 0; i < path.Count; i++) {
			MovementPoints--;
			if (PathTicked != null)
				PathTicked();
			CurrentPlayer.SetPosition(path[i]);
			yield return new WaitForSeconds(movementSpeed);
			FieldManager.Master.ClearField(path[i]);
			if (WaitForStop == true) {
				WaitForStop = false;
				break;
			}
		}
		InputManager.MousePressed -= PathStopWalk;
		if (GameBusy != null)
			GameBusy(true);
		TurnPathMark();
		yield return null;
	}

	private void PathStopWalk() {   // Indicate that user want to stop moving
		WaitForStop = true;
	}

	// Global Methods

	public void EndTurn() {

		AEDice.DiceAnimEnd -= TurnPathMark;
		if (CurrentPath != null)
			CurrentPlayer.SetPath(CurrentPath);
		InputManager.MouseClickField -= TurnPathMarked;
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

	public string RollMovementPoints() {
		int a = Random.Range(1, 7);
		int b = Random.Range(1, 7);
		MovementPoints = a + b;
		return a.ToString() + b.ToString();
	}
}