using UnityEngine;
using System.Collections;
using System;

public class Target : MonoBehaviour {

	public event Action<Target> HitEvent;

	public Node node;

	void Start() {
		node = GetComponent<Node>();
		node.HitEvent += OnNodeHit;
	}

	void OnNodeHit(Node node) {
		HitEvent(this);
	}

	public void Reset() {
		node.SetLED(Color.white);
	}

	public void SignalDidDamage() {
		node.Flash(Color.white, Color.black);	
	}

	public void SignalTookDamage() {
		node.SetRumble(1f);
		node.SetRumble(0, 1f);

		node.Flash(Color.red, Color.black);
	}

	public void SignalDead() {
		node.SetRumble(0);
		node.SetLED(Color.red);
	}

	public void StopSignal() {
		node.SetLED(Color.white);
	}

}
