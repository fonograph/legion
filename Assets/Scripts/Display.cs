using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Display : MonoBehaviour {

	void Start() {
		mainTextAnimator = mainText.GetComponent<Animator>();
	}

	public Text scoreText;
	public Image[] lifeImages;
	public Text mainText;
	private Animator mainTextAnimator;
	public Image flashImage;
	public GameObject practiceText;

	public void Reset(int score, int life) {
		SetScore(score);
		SetLife(life);
		mainTextAnimator.SetTrigger("Hide");
		SetPractice(false);
	}

	public void SetScore(int score) {
		scoreText.text = score.ToString();
	}

	public void SetLife(int life) {
		for ( int i=0; i<lifeImages.Length; i++ ) {
			lifeImages[i].gameObject.SetActive(i < life);
		}
	}

	public void ShowStart() {
		mainText.text = "START";
		mainTextAnimator.SetTrigger("Show");
	}

	public void ShowWaveStart(int wave) {
		mainText.text = "WAVE\n" + wave;
		mainTextAnimator.SetTrigger("Show");
	}

	public void ShowWaveEnd(int wave) {
		mainText.text = "WAVE\nCOMPLETE";
		mainTextAnimator.SetTrigger("Show");
	}

	public void ShowGameOver() {
		mainText.text = "GAME\nOVER";
		mainTextAnimator.SetTrigger("ShowStay");
	}

	public void ShowHit() {
		Flash(Color.red);
	}

	public void ShowKill() {
		Flash(Color.green);
	}

	public void ShowTimeout() {
		Flash(Color.blue);
	}

	public void SetPractice(bool toggle) {
		practiceText.SetActive(toggle);
	}

	void Flash(Color color) {
		flashImage.color = color;
		flashImage.gameObject.SetActive(true);
		Invoke("HideFlash", 0.4f);
	}

	void HideFlash() {
		flashImage.gameObject.SetActive(false);
	}
}
