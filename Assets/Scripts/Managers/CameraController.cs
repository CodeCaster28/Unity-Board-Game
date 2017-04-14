using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour {
	
	[SerializeField, Range(0.0f, 1.0f)]
	private float lerpRateRotation;
	[SerializeField, Range(0.0f, 3.0f)]
	private float lerpRateZoom;
	[SerializeField, Range(0.0f, 1.0f)]
	private float lerpRatePanning;
	[SerializeField, Range(0.0f, 3.0f)]
	private float rotationSpeed;
	[SerializeField, Range(0.0f, 3.0f)]
	private float zoomSpeed;
	[SerializeField, Range(0.0f, 1.0f)]
	private float panningSpeed;
	
	private float xRotation;
	private float yRotation;
	private float forwardMove;
	private float sideMove;
	private float scrollZoom;
	private Vector3 dirForward;
	private Vector3 dirSide;
	private float zRotation;
	private float height;

	// Mono Methods

	void Start() {
		InputManager.MouseMoved += Rotate;
		InputManager.KeyForward += PanForward;
		InputManager.ScrollWheel += ZoomInOut;
		InputManager.KeySide += PanSideways;
	}

	void Update() {

		xRotation = Mathf.Lerp(xRotation, 0, lerpRateRotation);
		yRotation = Mathf.Lerp(yRotation, 0, lerpRateRotation);

		zRotation = transform.eulerAngles.z;
		if (zRotation < 280 && zRotation > 135) {
			if (yRotation > 0)
				yRotation = 0;
		}
		else if (zRotation > 350 || zRotation < 135) {
			if (yRotation < 0)
				yRotation = 0;
		}
		transform.eulerAngles += new Vector3(0, xRotation * rotationSpeed, -yRotation * rotationSpeed);

		height = transform.position.y;
		if (height < 0.548f) {
			if (scrollZoom > 0)
				scrollZoom = 0;
		}
		else if (height > 7.7f) {
			if (scrollZoom < 0)
				scrollZoom = 0;
		}
		scrollZoom = Mathf.Lerp(scrollZoom, 0, lerpRateZoom);
		transform.position += -1 * transform.up * scrollZoom * zoomSpeed;

		forwardMove = Mathf.Lerp(forwardMove, 0, lerpRatePanning);
		transform.position += dirForward * forwardMove * panningSpeed;

		sideMove = Mathf.Lerp(sideMove, 0, lerpRatePanning);
		transform.position += dirSide * sideMove * panningSpeed;
	}

	void OnDestroy() {
		InputManager.MouseMoved -= Rotate;
		InputManager.KeyForward -= PanForward;
		InputManager.ScrollWheel -= ZoomInOut;
		InputManager.KeySide -= PanSideways;
	}

	// Spherical rotation

	private void Rotate(float xMovement, float yMovement) {	
		xRotation += xMovement;
		yRotation += yMovement;
	}

	// Pan back and forth

	private void PanForward(bool forwardPan) {				
		forwardMove += forwardPan ? 0.05f : -0.05f;
		dirForward = -transform.right;
		dirForward.y = 0;
		dirForward.Normalize();
	}

	// Pan left and right

	private void PanSideways(bool sidePan) {                
		sideMove += sidePan ? 0.05f : -0.05f;
		dirSide = -transform.forward;
		dirSide.y = 0;
		dirSide.Normalize();
	}

	// Zoom in and out

	private void ZoomInOut(float zoom) {                   
		scrollZoom += zoom;
	}
}
