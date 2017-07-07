using UnityEngine;
using System;
using System.Collections;

public class Node : MonoBehaviour {


	public UniMoveController controller;
//	private Vector3 lastAccel = Vector3.zero;

	public int type;
	public bool active; // inactive means fully disabled, i.e. not participating in game

	public bool inGame;

	private float mx;
	private float my;
	private float mz;

	public float mxMin;
	public float mxMax;
	public float myMin;
	public float myMax;
	public float mzMin;
	public float mzMax;

	private bool canIBeCalibrating;
	private int mxMinDriftTicks;
	private int mxMaxDriftTicks;
	private int myMinDriftTicks;
	private int myMaxDriftTicks;
	private int mzMinDriftTicks;
	private int mzMaxDriftTicks;

//	private bool calibrated = false;
//	private bool calibrationAllowed = false;
//	private bool calibrating = false;
//	private float calibratingXMin;
//	private float calibratingXMax;
//	private float calibratingYMin;
//	private float calibratingYMax;
//	private float calibratingZMin;
//	private float calibratingZMax;

	private bool lostConnection = false;

	private IEnumerator setLEDRoutine;
	private IEnumerator setRumbleRoutine;

	public event Action<Node> HitEvent;

	void Awake() {
	}

	void Start() {
	}

	public void Init(UniMoveController c) {
		controller = c;

		canIBeCalibrating = true;
		mxMax = -2048;
		mxMin = 2048;
		myMax = -2048;
		myMin = 2048;
		mzMax = -2048;
		mzMax = 2048;
		mxMinDriftTicks = 0;
		mxMaxDriftTicks = 0;
		myMinDriftTicks = 0;
		myMaxDriftTicks = 0;
		mzMinDriftTicks = 0;
		mzMaxDriftTicks = 0;

		active = true;

		LoadCalibration();
		ResetLEDAndRumble();

		Invoke("CalibrateForever", 0);
	}


	// Update is called once per frame
	void Update () {

		if ( controller == null ) {
			return;
		}

		// to switch node type, press all buttons while holding trigger
		// to turn an attacker on/off, press all buttons without trigger

		if ( (controller.GetButton(PSMoveButton.Square) && controller.GetButton(PSMoveButton.Circle) && controller.GetButton(PSMoveButton.Cross) && controller.GetButton(PSMoveButton.Square)) && 
			(controller.GetButtonDown(PSMoveButton.Square) || controller.GetButtonDown(PSMoveButton.Circle) || controller.GetButtonDown(PSMoveButton.Cross) || controller.GetButtonDown(PSMoveButton.Square))
		) {
			if ( controller.Trigger > 0.5f ) {
				if ( type == 1 ) {
					type = 2;
					PlayerPrefs.SetInt(controller.Serial + "Type", 2);
				}
				else {
					type = 1;
					PlayerPrefs.SetInt(controller.Serial + "Type", 1);
				}
			} else {
				active = !active;
			}
			ResetLEDAndRumble();
		}

//		if ( calibrationAllowed ) {
//			// Button presses
//			if ( controller.GetButtonDown(PSMoveButton.Circle) ) {
//				if ( !calibrating ) {
//					calibrating = true;
//					calibratingXMax = -2048;
//					calibratingXMin = 2048;
//					calibratingYMax = -2048;
//					calibratingYMin = 2048;
//					calibratingZMax = -2048;
//					calibratingZMin = 2048;
//				}
//				else {
//					calibrating = false;
//					PlayerPrefs.SetInt(controller.Serial + "Calibrated", 1);
//					PlayerPrefs.SetFloat(controller.Serial + "XMax", calibratingXMax);
//					PlayerPrefs.SetFloat(controller.Serial + "XMin", calibratingXMin);
//					PlayerPrefs.SetFloat(controller.Serial + "YMax", calibratingYMax);
//					PlayerPrefs.SetFloat(controller.Serial + "YMin", calibratingYMin);
//					PlayerPrefs.SetFloat(controller.Serial + "ZMax", calibratingZMax);
//					PlayerPrefs.SetFloat(controller.Serial + "ZMin", calibratingZMin);
//					LoadCalibration();
//					ResetLEDAndRumble();
//				}
//			}

			// accelerometer test
//			if ( !calibrating && (controller.Acceleration-lastAccel).magnitude > Game.Instance.attackAccelThreshold )  {
//				SetLED(Color.cyan);
//				SetLED(Color.black, 1f);
//			}

//		}

		mx = controller.Magnetometer.x;
		my = controller.Magnetometer.y;
		mz = controller.Magnetometer.z;		
//		Debug.Log (mx.ToString() + " " + my.ToString() + " " + mz.ToString());

		if ( inGame ) {
			float d = type == 1 ? Game.Instance.magnetThresholdTarget : Game.Instance.magnetThresholdAttacker;
			if ( (mx!=0 || my!=0 || mz!=0 ) && ( mx < mxMin-d || mx > mxMax+d || my < myMin-d || my > myMax+d || mz < mzMin-d || mz > mzMax+d ) ) {
//					alive = false;
//					SetLED(DEADCOLOR);
//					SetRumble(0);
				HitEvent(this);
			}
		}

//		if ( calibrating ) {
//			if ( mx > calibratingXMax ) calibratingXMax = mx;
//			if ( mx < calibratingXMin ) calibratingXMin = mx;
//			if ( my > calibratingYMax ) calibratingYMax = my;
//			if ( my < calibratingYMin ) calibratingYMin = my;
//			if ( mz > calibratingZMax ) calibratingZMax = mz;
//			if ( mz < calibratingZMin ) calibratingZMin = mz;
//			float target = 360;
//			float px = Math.Min(1, ( calibratingXMax - calibratingXMin ) / target);
//			float py = Math.Min(1, ( calibratingYMax - calibratingYMin ) / target);
//			float pz = Math.Min(1, ( calibratingZMax - calibratingZMin ) / target);
//			Debug.Log(px.ToString() + " " + py.ToString() + " " + pz.ToString());
//			float p = Mathf.Min(px, Mathf.Min(py, pz));
//			p = Mathf.Pow(p, 4f); // skew color curve
//			SetLED(new Color(1-p, p, 0));
//		}

		if ( mx==0 && my==0 && mz==0 ) {
			ResetLEDAndRumble();
			SetLED(Color.red);
			lostConnection = true;
		} else if (lostConnection) {
			lostConnection = false;
			ResetLEDAndRumble();
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

//		float D = Game.Instance.magnetThreshold;
//		Debug.Log(mx.ToString() + " " + my.ToString() + " " + mz.ToString() + " / " + mxMin.ToString()+"-"+mxMax.ToString() + " " + myMin.ToString()+"-"+myMax.ToString() + " " + mzMin.ToString()+"-"+mzMax.ToString());
//		if ( (mx!=0 || my!=0 || mz!=0 ) && ( mx < mxMin-D || mx > mxMax+D || my < myMin-D || my > myMax+D || mz < mzMin-D || mz > mzMax+D ) ) {
//			SetLED(Color.red);
//			GameObject.FindObjectOfType<AudioSource>().PlayOneShot(GameObject.FindObjectOfType<Game>().nodeHitSound);
//		} else {
//			SetLED(Color.green);			
//		}
	}

	public void AllowCalibration() {
		canIBeCalibrating = true;
	}
	public void UnallowCalibration() {
		canIBeCalibrating = false;
	}

	public void CalibrateForever() {
		int thresholdTicks = 2;
		if ( canIBeCalibrating ) {
			if ( mx < mxMin ) {
				mxMinDriftTicks++;
				if ( mxMinDriftTicks >= thresholdTicks ) {
					mxMin = mx;
					mxMax = mxMin+360;
					mxMinDriftTicks = 0;
					PlayerPrefs.SetFloat(controller.Serial + "XMax", mxMax);
					PlayerPrefs.SetFloat(controller.Serial + "XMin", mxMin);
				}
			}
			if ( mx > mxMax ) {
				mxMaxDriftTicks++;
				if ( mxMaxDriftTicks >= thresholdTicks ) {
					mxMax = mx;
					mxMin = mxMax-360;
					mxMaxDriftTicks = 0;
					PlayerPrefs.SetFloat(controller.Serial + "XMax", mxMax);
					PlayerPrefs.SetFloat(controller.Serial + "XMin", mxMin);
				}
			}
			if ( my < myMin ) {
				myMinDriftTicks++;
				if ( myMinDriftTicks >= thresholdTicks ) {
					myMin = my;
					myMax = myMin+360;
					myMinDriftTicks = 0;
					PlayerPrefs.SetFloat(controller.Serial + "YMax", myMax);
					PlayerPrefs.SetFloat(controller.Serial + "YMin", myMin);
				}
			}
			if ( my > myMax ) {
				myMaxDriftTicks++;
				if ( myMaxDriftTicks >= thresholdTicks ) {
					myMax = my;
					myMin = myMax-360;
					myMaxDriftTicks = 0;
					PlayerPrefs.SetFloat(controller.Serial + "YMax", myMax);
					PlayerPrefs.SetFloat(controller.Serial + "YMin", myMin);
				}
			}
			if ( mz < mzMin ) {
				mzMinDriftTicks++;
				if ( mzMinDriftTicks >= thresholdTicks ) {
					mzMin = mz;
					mzMax = mzMin+360;
					mzMinDriftTicks = 0;
					PlayerPrefs.SetFloat(controller.Serial + "ZMax", mzMax);
					PlayerPrefs.SetFloat(controller.Serial + "ZMin", mzMin);
				}
			}
			if ( mz > mzMax ) {
				mzMaxDriftTicks++;
				if ( mzMaxDriftTicks >= thresholdTicks ) {
					mzMax = mz;
					mzMin = mzMax-360;
					mzMaxDriftTicks = 0;
					PlayerPrefs.SetFloat(controller.Serial + "ZMax", mzMax);
					PlayerPrefs.SetFloat(controller.Serial + "ZMin", mzMin);
				}
			}
		}
		Invoke("CalibrateForever", 0.5f);
	}

	public void SetCalibrationAllowed(bool value) {
//		calibrationAllowed = value;
//		if ( calibrationAllowed ) {
//			SetLED(Color.white);
//		}
//		else {
//			ResetLEDAndRumble();
//		}
	}

	public float GetOrientationDifference(Node node) {
		Vector3 diff = normalizedOrientationDifference(normalizedOrientation(controller.Orientation.eulerAngles), normalizedOrientation(node.controller.Orientation.eulerAngles));
		Vector2 diff2 = new Vector2(diff.x, diff.y);

		return diff2.magnitude;
	}

	public void ResetLEDAndRumble() {
		if ( type == 1 ) {
			SetLED(Color.yellow);
		} else {
			if ( active ) {
				SetLED(Color.blue);
			} else {
				SetLED(Color.black);
			}
		}
		SetRumble(0);
	}

	public void SetLED(Color color) {
		if ( lostConnection ) {
			return;
		}

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
//			rumble = 0;
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
//		calibrated = PlayerPrefs.GetInt(controller.Serial + "Calibrated", 0) == 1;
		mxMax = PlayerPrefs.GetFloat(controller.Serial + "XMax");
		mxMin = PlayerPrefs.GetFloat(controller.Serial + "XMin");
		myMax = PlayerPrefs.GetFloat(controller.Serial + "YMax");
		myMin = PlayerPrefs.GetFloat(controller.Serial + "YMin");
		mzMax = PlayerPrefs.GetFloat(controller.Serial + "ZMax");
		mzMin = PlayerPrefs.GetFloat(controller.Serial + "ZMin");
		type = PlayerPrefs.GetInt(controller.Serial + "Type", 1);

//		if ( !calibrated ) {
//			Debug.LogWarning("Magnet not calibrated!");
//		}
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
