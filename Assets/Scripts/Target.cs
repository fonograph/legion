using UnityEngine;
using System.Collections;
using System;

public class Target : MonoBehaviour {

	// We only need these for durational signals, i.e. flashing
	public enum SignalType {TookOrDidDamage, Heal};

	public event Action<Target> HitEvent;

	public Node node;

	private SignalType? currentSignal = null;

	void Start() {
		node = GetComponent<Node>();
		node.HitEvent += OnNodeHit;
	}

	void OnNodeHit(Node node) {
		HitEvent(this);
	}

	public void Reset() {
		this.currentSignal = null;
		node.SetLED(Color.white);
	}

	public void SignalDidDamage() {
		this.currentSignal = SignalType.TookOrDidDamage;
		node.Flash(Color.white);	
	}

	public void SignalTookDamage() {
		this.currentSignal = SignalType.TookOrDidDamage;
		node.SetRumble(1f);
		node.SetRumble(0, 1f);
		node.Flash(Color.red);
	}

	public void SignalDead() {
		this.currentSignal = null;
		node.SetRumble(0);
		node.SetLED(Color.red);
	}

	public void SignalHeal() {
		this.currentSignal = SignalType.Heal;
		node.Flash(Color.green);
		Invoke("StopSignalHeal", 1);
	}

	private void StopSignalHeal() {
		this.StopSignal(SignalType.Heal);
	}

	public void StopSignal(SignalType type) {
		if (this.currentSignal == type) {
			this.currentSignal = null;
			node.SetLED(Color.white);
		}
	}

}
