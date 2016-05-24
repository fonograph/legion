using UnityEngine;
using System;
using System.Collections;

public class Node : MonoBehaviour {

	public static Color LIVECOLOR = Color.green;
	public static Color DEADCOLOR = Color.red;

	public UniMoveController controller;
//	private Vector3 lastAccel = Vector3.zero;

	public int type;

	public bool inGame;

	private bool calibrated = false;
	private float mxMin;
	private float mxMax;
	private float myMin;
	private float myMax;
	private float mzMin;
	private float mzMax;

	private bool calibrationAllowed = false;
	private bool calibrating = false;
	private float calibratingXMin;
	private float calibratingXMax;
	private float calibratingYMin;
	private float calibratingYMax;
	private float calibratingZMin;
	private float calibratingZMax;


	private IEnumerator setLEDRoutine;
	private IEnumerator setRumbleRoutine;

	public event Action<Node> HitEvent;

	void Awake() {
	}

	void Start() {
	}

	public void Init(UniMoveController c) {
		controller = c;
		LoadCalibration();
		ResetLEDAndRumble();
	}


	// Update is called once per frame
	void Update () {

		if ( controller == null ) {
			return;
		}

		if ( calibrationAllowed ) {
			// Button presses
			if ( controller.GetButtonDown(PSMoveButton.Circle) ) {
				if ( !calibrating ) {
					calibrating = true;
					calibratingXMax = -2048;
					calibratingXMin = 2048;
					calibratingYMax = -2048;
					calibratingYMin = 2048;
					calibratingZMax = -2048;
					calibratingZMin = 2048;
				}
				else {
					calibrating = false;
					PlayerPrefs.SetInt(controller.Serial + "Calibrated", 1);
					PlayerPrefs.SetFloat(controller.Serial + "XMax", calibratingXMax);
					PlayerPrefs.SetFloat(controller.Serial + "XMin", calibratingXMin);
					PlayerPrefs.SetFloat(controller.Serial + "YMax", calibratingYMax);
					PlayerPrefs.SetFloat(controller.Serial + "YMin", calibratingYMin);
					PlayerPrefs.SetFloat(controller.Serial + "ZMax", calibratingZMax);
					PlayerPrefs.SetFloat(controller.Serial + "ZMin", calibratingZMin);
					LoadCalibration();
					SetLED(Color.blue);
				}
			}
			if ( controller.GetButtonDown(PSMoveButton.Square) ) {
				if ( type == 1 ) {
					type = 2;
					PlayerPrefs.SetInt(controller.Serial + "Type", 2);
				}
				else {
					type = 1;
					PlayerPrefs.SetInt(controller.Serial + "Type", 1);
				}
				ResetLEDAndRumble();
			}

			// accelerometer test
//			if ( !calibrating && (controller.Acceleration-lastAccel).magnitude > Game.Instance.attackAccelThreshold )  {
//				SetLED(Color.cyan);
//				SetLED(Color.black, 1f);
//			}

		}

		float mx = controller.Magnetometer.x;
		float my = controller.Magnetometer.y;
		float mz = controller.Magnetometer.z;		
//		Debug.Log (mx.ToString() + " " + my.ToString() + " " + mz.ToString());

		if ( inGame ) {
			float d = Game.Instance.magnetThreshold;
			if ( (mx!=0 || my!=0 || mz!=0 ) && ( mx < mxMin-d || mx > mxMax+d || my < myMin-d || my > myMax+d || mz < mzMin-d || mz > mzMax+d ) ) {
//					alive = false;
//					SetLED(DEADCOLOR);
//					SetRumble(0);
				HitEvent(this);
			}
		}

		if ( calibrating ) {
			if ( mx > calibratingXMax ) calibratingXMax = mx;
			if ( mx < calibratingXMin ) calibratingXMin = mx;
			if ( my > calibratingYMax ) calibratingYMax = my;
			if ( my < calibratingYMin ) calibratingYMin = my;
			if ( mz > calibratingZMax ) calibratingZMax = mz;
			if ( mz < calibratingZMin ) calibratingZMin = mz;
			float target = 360;
			float px = Math.Min(1, ( calibratingXMax - calibratingXMin ) / target);
			float py = Math.Min(1, ( calibratingYMax - calibratingYMin ) / target);
			float pz = Math.Min(1, ( calibratingZMax - calibratingZMin ) / target);
			Debug.Log(px.ToString() + " " + py.ToString() + " " + pz.ToString());
			float p = Mathf.Min(px, Mathf.Min(py, pz));
			SetLED(new Color(1-p, p, 0));
		}


//		lastAccel = controller.Acceleration;

		// DEBUG
		//		if ( hasBall ) {
		//			Debug.Log(controller.Orientation.eulerAngles);
		//			Debug.Log((controller.Orientation.eulerAngles - startOrientation));
		//			Debug.Log((controller.Orientation.eulerAngles - startOrientation).magnitude);
		//		}
		//		if ( Game.Instance.HoldingPlayer != null && Game.Instance.HoldingPlayer != this ) {
		//			Debug.Log (Game.Instance.HoldingPlayer.GetOrientationDifference(this));
		//		}

	}

	public void Reset() {
	}

	public void SetCalibrationAllowed(bool value) {
		calibrationAllowed = value;
		if ( calibrationAllowed ) {
			SetLED(Color.blue);
		}
		else {
			ResetLEDAndRumble();
		}
	}

	public float GetOrientationDifference(Node node) {
		Vector3 diff = normalizedOrientationDifference(normalizedOrientation(controller.Orientation.eulerAngles), normalizedOrientation(node.controller.Orientation.eulerAngles));
		Vector2 diff2 = new Vector2(diff.x, diff.y);

		return diff2.magnitude;
	}

	public void ResetLEDAndRumble() {
		if ( !calibrated ) {
			SetLED(Color.red);
		}
		else {
			SetLED(type==1 ? Color.yellow : Color.green);
		}
		SetRumble(0);
	}

	public void SetLED(Color color) {
		SetLED(color, null);
	}

	public void SetLED(Color color, float? delay) {
		if ( setLEDRoutine != null) 
			StopCoroutine(setLEDRoutine);

		if ( delay != null ) {
			setLEDRoutine = SetLEDCoroutine(color, (float)delay);
			StartCoroutine(setLEDRoutine);
		}
		else {
			controller.SetLED(color);
		}
	}

	private IEnumerator SetLEDCoroutine(Color color, float seconds) {
		yield return new WaitForSeconds(seconds); 
		SetLED(color); 
	}

	
	public void SetRumble(float rumble) {
		SetRumble(rumble, null);
	}
	
	public void SetRumble(float rumble, float? delay) {
		if ( setRumbleRoutine != null) 
			StopCoroutine(setRumbleRoutine);
		
		if ( delay != null ) {
			setRumbleRoutine = SetRumbleCoroutine(rumble, (float)delay);
			StartCoroutine(setRumbleRoutine);
		}
		else {
			controller.SetRumble(rumble);
		}
	}
	
	private IEnumerator SetRumbleCoroutine(float rumble, float seconds) {
		yield return new WaitForSeconds(seconds); 
		SetRumble(rumble); 
	}

	public void Flash(Color color1, Color color2) {
		if ( setLEDRoutine != null) 
			StopCoroutine(setLEDRoutine);

		SetLED(color1);
		setLEDRoutine = FlashCoroutine(color1, color2, 0.2f);
		StartCoroutine(setLEDRoutine);
	}

	private IEnumerator FlashCoroutine(Color color1, Color color2, float delay) {
		yield return new WaitForSeconds(delay);
		Flash(color2, color1);
	}

	private void LoadCalibration() {
		calibrated = PlayerPrefs.GetInt(controller.Serial + "Calibrated", 0) == 1;
		mxMax = PlayerPrefs.GetFloat(controller.Serial + "XMax");
		mxMin = PlayerPrefs.GetFloat(controller.Serial + "XMin");
		myMax = PlayerPrefs.GetFloat(controller.Serial + "YMax");
		myMin = PlayerPrefs.GetFloat(controller.Serial + "YMin");
		mzMax = PlayerPrefs.GetFloat(controller.Serial + "ZMax");
		mzMin = PlayerPrefs.GetFloat(controller.Serial + "ZMin");
		type = PlayerPrefs.GetInt(controller.Serial + "Type", 1);

		if ( !calibrated ) {
			Debug.LogWarning("Magnet not calibrated!");
		}
	}
	



	private Vector3 normalizedOrientation(Vector3 o) {
		Vector3 orientation = o;
		orientation.x -= orientation.x > 180 ? 360 : 0;
		orientation.y -= orientation.y > 180 ? 360 : 0;
		orientation.z -= orientation.z > 180 ? 360 : 0;
		return orientation;
	}

	private Vector3 normalizedOrientationDifference(Vector3 normalizedOrientation1, Vector3 normalizedOrientation2) {
		Vector3 orientationDiff = normalizedOrientation1 - normalizedOrientation2;
		orientationDiff.x += (orientationDiff.x>180) ? -360 : (orientationDiff.x<-180) ? 360 : 0;
		orientationDiff.y += (orientationDiff.y>180) ? -360 : (orientationDiff.y<-180) ? 360 : 0;
		orientationDiff.z += (orientationDiff.z>180) ? -360 : (orientationDiff.z<-180) ? 360 : 0;
		return orientationDiff;
	}
}
