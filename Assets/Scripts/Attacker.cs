using UnityEngine;
using System.Collections;

public class Attacker : MonoBehaviour {

	public AudioClip announceSound;

	protected AudioSource _audioSource;

	void Start() {
		_audioSource = GetComponent<AudioSource>();
	}

	public void Announce() {
		_audioSource.PlayOneShot(announceSound);
	}

}
