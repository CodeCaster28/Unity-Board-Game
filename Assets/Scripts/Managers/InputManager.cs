using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InputManager : MonoBehaviour {

	public delegate void MouseMovedDelegate(float xMovement, float yMovement);
	public static event MouseMovedDelegate MouseMoved;
	public delegate void MousePressedDelegate(Field clickedField);
	public static event MousePressedDelegate MousePressed;
	public delegate void KeyForwardDelegate(bool forwardPan);
	public static event KeyForwardDelegate KeyForward;
	public delegate void KeySideDelegate(bool sidePan);
	public static event KeySideDelegate KeySide;
	public delegate void MouseWheelDelegate(float zoom);
	public static event MouseWheelDelegate ScrollWheel;
	public delegate void KeySpacelDelegate();
	public static event KeySpacelDelegate KeySpace;

	private float xMovement;
	private float yMovement;
	private float zoom;
	private bool forwardPan;
	private bool sidePan;
	private Field clickedField;
	private List<Field> currentPath;
	private Ray ray;
	private RaycastHit hit;

	// Mono Methods

	void Update() {
		InvokeActionOnInput();
	}

	// Check for pressed keys and conditions met

	private void InvokeActionOnInput() {

		if (Input.GetMouseButtonDown(0)) {
			ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, out hit)) {
				if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) { 
					if (hit.collider.GetComponent<Field>() != null) {
						clickedField = hit.collider.GetComponent<Field>();
						OnMousePressed(clickedField);
					}
				}
			}
		}
		if (Input.GetKey(KeyCode.Space)) {
			OnKeySpace();
		}
		
		if (Input.GetMouseButton(1)) {
			xMovement = Input.GetAxis("Mouse X");
			yMovement = Input.GetAxis("Mouse Y");
			OnMouseMoved(xMovement, yMovement);
		}
		if (Input.GetKey(KeyCode.W)) {
			forwardPan = true;
			OnKeyForward(forwardPan);
		}
		if (Input.GetKey(KeyCode.S)) {
			forwardPan = false;
			OnKeyForward(forwardPan);
		}
		if (Input.GetKey(KeyCode.A)) {
			sidePan = true;
			OnKeySide(sidePan);
		}
		if (Input.GetKey(KeyCode.D)) {
			sidePan = false;
			OnKeySide(sidePan);
		}
		if (Input.GetAxis("Mouse ScrollWheel") != 0) {
			zoom = Input.GetAxis("Mouse ScrollWheel");
			OnScrollWheel(zoom);
		}
	}

	// Notify subscribers

	private static void OnMouseMoved(float xMovement, float yMovement) {
		if (MouseMoved != null)
			MouseMoved(xMovement, yMovement);
	}
	private static void OnMousePressed(Field clickedField) {
		if (MousePressed != null)
			MousePressed(clickedField);
	}
	private static void OnKeyForward(bool forwardPan) {
		if (KeyForward != null)
			KeyForward(forwardPan);
	}
	private static void OnKeySide(bool sidePan) {
		if (KeySide != null)
			KeySide(sidePan);
	}
	private static void OnScrollWheel(float zoom) {
		if (ScrollWheel != null)
			ScrollWheel(zoom);
	}
	private static void OnKeySpace() {
		if (KeySpace != null)
			KeySpace();
	}
}
