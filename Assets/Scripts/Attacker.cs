using UnityEngine;
using System.Collections;
using System;

public class Attacker : MonoBehaviour {

	public event Action<Attacker> HitEvent;
	public event Action<Attacker> TimeoutEvent;
	public event Action<Attacker> TimeoutWarningEvent;

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
		CancelInvoke("TimeoutWarning");

		node.SetRumble(1f);
		node.SetRumble(0, 1f);

		node.SetLED(Color.green);

		if ( Game.Instance.countdownEnabled && !Game.Instance.isPractice ) { 
			Invoke("Timeout", Game.Instance.countdownTime);
			Invoke("TimeoutWarning", Game.Instance.countdownTime - 5);
		}
	}

	public void Reset() {
		alive = false;
		CancelInvoke("Timeout");
		CancelInvoke("TimeoutWarning");

		node.SetLED(Color.black);
	}

	public void Kill() {
		alive = false;
		CancelInvoke("Timeout");
		CancelInvoke("TimeoutWarning");

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

	private void TimeoutWarning() {
		if ( alive ) {
			TimeoutWarningEvent(this);
		}
	}

	public bool IsAlive() {
		return alive;
	}

	public bool IsActive() {
		return node.active;
	}
		
}
