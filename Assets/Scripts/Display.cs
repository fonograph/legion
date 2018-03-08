using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Display : MonoBehaviour {

	public Text scoreText;
	public Image[] lifeImages;
	public Text mainText;
	public Animator mainTextAnimator;
	public Animator scoreAnimator;
	public Image flashImage;
	public GameObject practiceText;

	public void Reset(int score, int life) {
		SetScore(score);
		ShowScore();
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
		scoreAnimator.SetTrigger("Hide");
		HideScore();
		//Invoke("ShowScore", 3f);
	}

	public void ShowWaveStart(int wave) {
		mainText.text = "WAVE\n" + wave;
		mainTextAnimator.SetTrigger("Show");
		HideScore();
		Invoke("ShowScore", 3f);
	}

	public void ShowWaveEnd(int wave) {
		mainText.text = "WAVE\nCOMPLETE";
		mainTextAnimator.SetTrigger("Show");
		HideScore();
		//Invoke("ShowScore", 3f);
	}

	public void ShowGameOver() {
		mainText.text = "GAME\nOVER";
		mainTextAnimator.SetTrigger("ShowStay");
		HideScore();
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

	void ShowScore() {
		scoreAnimator.SetTrigger("Show");
	}

	void HideScore() {
		scoreAnimator.SetTrigger("Hide");
	}
}
