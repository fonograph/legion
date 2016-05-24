using UnityEngine;
using System.Collections;
using System;

public class Target : MonoBehaviour {

	public event Action<Target> HitEvent;

	public Node node;

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

	public void Reset() {
		alive = true;
	}

	public void Kill() {
		node.SetRumble(1f);
		node.SetRumble(0, 1f);
	}
}
