using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerManager : GenericSingletonClass<PlayerManager>
{
	public delegate void TurnStartDelegate();
	public static event TurnStartDelegate TurnStarted;

	public List<Player> Players;
	private static Player CurrentPlayer;
	private static List<Field> CurrentPath;
	private static int PlayerIndex;
	private static bool IsSubscribedStartWalk;
	private int MovementPoints;
	private int PlayerCount;

	void Start () {
		PlayerCount = Players.Count;
		PlayerIndex = 0;
		TurnStart();
	}

	// Turn Chain

	private void TurnStart() {
		MovementPoints = 5;
		
		if (TurnStarted != null) {
			TurnStarted();
		}
		CycleNextPlayer();
		FieldManager.Master.ClearAllFields();
		TurnPathMark();
	}

	private void CycleNextPlayer() {
		CurrentPlayer = Players[PlayerIndex];
		PlayerIndex++;
		if (PlayerIndex > Players.Count - 1) {
			PlayerIndex = 0;
		}
	}

	private void TurnPathMark() {           // Marking path stage
		IsSubscribedStartWalk = false;
		InputManager.MousePressed += TurnPathMarked;
	}

	private void TurnPathMarked(Field targetField) {	// Wait for confirmation stage (path can be redefinied)
		FieldManager.Master.MarkPath(targetField, MovementPoints);
		if (CurrentPath != null && IsSubscribedStartWalk == false) {    // Before allowing to go down this path, check if it's correct
			Debug.Log("Entered" + CurrentPath);
			IsSubscribedStartWalk = true;
			InputManager.KeySpace += TurnPathStartWalk;
		}
	}
	
	private void TurnPathStartWalk () {		// Path travel stage, animate movement (can't mark new path/start new walk)
		InputManager.MousePressed -= TurnPathMarked;
		InputManager.KeySpace -= TurnPathStartWalk;
		List<Field> walkablePath = new List<Field>();
		walkablePath = CurrentPath.Take(MovementPoints).ToList();	// Travel only walkable path
		StartCoroutine(PathTick(walkablePath));
	}

	private IEnumerator PathTick(List<Field> path) {

		//List<Field> path = currentPath;  
		float movementSpeed = path.Count < 4 ? 0.325f : 1.30f / path.Count;

		for (int i = 0; i < path.Count; i++) {
			MovementPoints--;
			CurrentPlayer.SetPosition(path[i]);
			yield return new WaitForSeconds(movementSpeed);
			FieldManager.Master.ClearField(path[i]);
		}

		CurrentPath = null;                 // Remove current path from memory, so it has to be marked again in new turn
		if (MovementPoints == 0)
			TurnStart();
		else
			TurnPathMark();
		yield return null;
	}

	// Global Methods
	
	public static Player GetCurrentPlayer() {
		return CurrentPlayer;
	}

	public int GetPlayerCount() {
		return PlayerCount;
	}

	public static void SetCurrentPath(List<Field> path) {
		CurrentPath = path;
	}

	// Destroy

	private void OnDestroy() {
	}
}