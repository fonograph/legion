using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour {

	[Range(0,1)]
	public float volume;

	[Range(0,1)]
	public float fadeSpeed;

	public AudioSource noAttackers;
	public AudioSource oneAttacker;
	public AudioSource multipleAttackers;

	protected AudioSource currentSource;
	protected AudioSource lastSource;


	// Use this for initialization
	void Start () {
		noAttackers.loop = true;
		oneAttacker.loop = true;
		multipleAttackers.loop = true;
	}

	public void Reset() {
		noAttackers.Stop();
		oneAttacker.Stop();
		multipleAttackers.Stop();
		currentSource = null;
		lastSource = null;
	}

	public void StartGame() {
		noAttackers.volume = 0;
		noAttackers.Play();

		oneAttacker.volume = 0;
		oneAttacker.Play();

		multipleAttackers.volume = 0;
		multipleAttackers.Play();
	}

	public void SetAttackers(int attackers) {
		lastSource = currentSource;

		if ( attackers == 0 ) {
			currentSource = noAttackers;
		}
		else if ( attackers == 1 ) {
			currentSource = oneAttacker;
		}
		else {
			currentSource = multipleAttackers;
		}
	}

	void Update() {
		if ( lastSource != null ) {
			lastSource.volume -= Time.deltaTime * fadeSpeed;
			if ( lastSource.volume <= 0 ) {
				lastSource.volume = 0;
				lastSource = null;
			}
		}

		if ( currentSource != null && currentSource.volume < volume ) {
			currentSource.volume += Time.deltaTime * fadeSpeed;
			if ( currentSource.volume > volume ) {
				currentSource.volume = volume;
			}
		} 
	}
	
}
