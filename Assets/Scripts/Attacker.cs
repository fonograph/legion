using UnityEngine;
using System.Collections;
using System;

public class Attacker : MonoBehaviour {

	public event Action<Attacker> HitEvent;

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

		node.SetRumble(1f);
		node.SetRumble(0, 1f);

		node.SetLED(Color.green);
	}

	public void Reset() {
		alive = false;

		node.SetLED(Color.black);
	}

	public void Kill() {
		alive = false;

		node.SetRumble(1f);
		node.SetRumble(0, 2f);

		node.SetLED(Color.red);
		node.SetLED(Color.black, 1f);
	}

	public bool IsAlive() {
		return alive;
	}
}
