using UnityEngine;
using System.Collections;
using System;

public class Attacker : MonoBehaviour {

	public event Action<Attacker> HitEvent;
	public event Action<Attacker> TimeoutEvent;

	private Node node;

	private bool alive;

	void Start() {
		node = GetComponent<Node>();
		node.HitEvent += OnNodeHit;
	}

	void OnNodeHit(Node node) {
		if ( alive ) {
			HitEvent(this);
		}
	}

	public void Activate() {
		alive = true;
		CancelInvoke("Timeout");

		node.SetRumble(1f);
		node.SetRumble(0, 1f);

		node.SetLED(Color.green);

		if ( Game.Instance.countdownEnabled ) { 
			Invoke("Timeout", Game.Instance.countdownTime);
		}
	}

	public void Reset() {
		alive = false;
		CancelInvoke("Timeout");

		node.SetLED(Color.black);
	}

	public void Kill() {
		alive = false;
		CancelInvoke("Timeout");

		node.SetRumble(1f);
		node.SetRumble(0, 2f);

		node.SetLED(Color.red);
		node.SetLED(Color.black, 1f);
	}

	private void Timeout() {
		if ( alive ) {
			TimeoutEvent(this);
		}
	}

	public bool IsAlive() {
		return alive;
	}

	public bool IsActive() {
		return node.active;
	}
		
}
