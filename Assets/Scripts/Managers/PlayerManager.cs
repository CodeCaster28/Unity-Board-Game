using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : GenericSingletonClass<PlayerManager>
{
	public delegate void TurnStartDelegate();
	public static event TurnStartDelegate TurnStarted;

	public List<Player> Players;
	private static Player CurrentPlayer;
	private static List<Field> currentPath;
	private static int PlayerIndex;
	private static bool IsSubscribedStartWalk;

	void Start () {

		PlayerIndex = 0;
		TurnStart();
	}

	// Turn Chain

	private void TurnStart() {
		IsSubscribedStartWalk = false;
		if (TurnStarted != null) {
			TurnStarted();
		}
		CycleNextPlayer();
		TurnPathMark();
	}

	private void CycleNextPlayer() {
		CurrentPlayer = Players[PlayerIndex];
		PlayerIndex++;
		if (PlayerIndex > Players.Count - 1) {
			PlayerIndex = 0;
		}
	}

	private void TurnPathMark() {			// Marking path stage
		InputManager.MousePressed += TurnPathMarked;
	}

	private void TurnPathMarked(Field targetField) {	// Wait for confirmation stage (path can be redefinied)
		FieldManager.Master.MarkPath(targetField);
		if (currentPath != null && IsSubscribedStartWalk == false) {            // Before allowing to go down this path, check if it's correct
			IsSubscribedStartWalk = true;
			InputManager.KeySpace += TurnPathStartWalk;
		}
	}
	
	private void TurnPathStartWalk () {		// Path travel stage, animate movement (can't mark new path/start new walk)
		InputManager.MousePressed -= TurnPathMarked;
		InputManager.KeySpace -= TurnPathStartWalk;
		StartCoroutine(PathTick(currentPath));
	}

	private IEnumerator PathTick(List<Field> currentPath) {

		List<Field> path = currentPath;  
		float movementSpeed = path.Count < 4 ? 0.325f : 1.30f / path.Count;

		for (int i = 0; i < path.Count; i++) {
			CurrentPlayer.SetPosition(path[i]);
			yield return new WaitForSeconds(movementSpeed);
			FieldManager.Master.ClearField(path[i]);
		}

		currentPath = null;                 // Remove current path from memory, so it has to be marked again in new turn
		TurnStart();
		yield return null;
	}

	// Global Methods
	
	public static Player GetCurrentPlayer() {
		return CurrentPlayer;
	}

	public static void SetCurrentPath(List<Field> path) {
		currentPath = path;
	}

	// Destroy

	private void OnDestroy() {
	}
}