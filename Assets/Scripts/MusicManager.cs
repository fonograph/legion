using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour {

	[Range(0,1)]
	public float volume;

	[Range(0,1)]
	public float fadeSpeed;

	private float _volume;

	public AudioSource[] waveTracks;

	protected AudioSource currentSource;
	protected AudioSource lastSource;


	// Use this for initialization
	void Start () {
		foreach ( AudioSource src in waveTracks ) {
			src.loop = true;
		}
	}

	public void Reset() {
		foreach ( AudioSource src in waveTracks ) {
			src.Stop();
		}
		currentSource = null;
		lastSource = null;
	}

	public void StartGame() {
		foreach ( AudioSource src in waveTracks ) {
			src.volume = 0;
			src.Play();
		}
	}

	public IEnumerator SetWave(int wave, float delay) {
		yield return new WaitForSeconds(delay);

		lastSource = currentSource;

		if ( wave > waveTracks.Length ) {
			wave = waveTracks.Length;
		}

		currentSource = waveTracks[wave-1];

		_volume = volume;
	}

	public IEnumerator SetBreak(float delay) {
		yield return new WaitForSeconds(delay);

		_volume = volume * 0.3f;;
		currentSource.volume = _volume;
	}

	void Update() {
		if ( lastSource != null ) {
			lastSource.volume -= Time.deltaTime * fadeSpeed;
			if ( lastSource.volume <= 0 ) {
				lastSource.volume = 0;
				lastSource = null;
			}
		}

		if ( currentSource != null && currentSource.volume < _volume ) {
			currentSource.volume += Time.deltaTime * fadeSpeed;
			if ( currentSource.volume > _volume ) {
				currentSource.volume = _volume;
			}
		} 
	}
	
}
