using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ControllerDebug : MonoBehaviour {

	public static bool Active = false;

	protected Text text;
	protected Node node;

	// Use this for initialization
	void Start () {
		text = gameObject.AddComponent<Text>();
		text.supportRichText = true;
		text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
		text.fontSize = 30;

		text.rectTransform.anchorMin = new Vector2(0, 0);
		text.rectTransform.anchorMax = new Vector2(1, 1);
		text.rectTransform.offsetMax = new Vector2(0, 0);
		text.rectTransform.offsetMin = new Vector2(0, 0);

		node = GetComponent<Node>();
	}
	
	// Update is called once per frame
	void Update () {
		text.enabled = Active;
		if ( node.controller != null ) {
			string xColor = node.controller.Magnetometer.x < node.mxMin || node.controller.Magnetometer.x > node.mxMax ? "red" : "green";
			string yColor = node.controller.Magnetometer.y < node.myMin || node.controller.Magnetometer.y > node.myMax ? "red" : "green";
			string zColor = node.controller.Magnetometer.z < node.mzMin || node.controller.Magnetometer.z > node.mzMax ? "red" : "green";

			text.text = string.Format(
				"{0} <color={10}>{1}</color> ({2} {3}) <color={11}>{4}</color> ({5} {6}) <color={12}>{7}</color> ({8} {9})",
				node.controller.Serial,
				node.controller.Magnetometer.x,
				node.mxMin,
				node.mxMax,
				node.controller.Magnetometer.y,
				node.myMin,
				node.myMax,
				node.controller.Magnetometer.z,
				node.mzMin,
				node.mzMax,
				xColor,
				yColor,
				zColor
			);
			 
		}
	}
}
