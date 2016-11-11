﻿using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

public class Game : MonoBehaviour {

	public enum Phase { Connecting, Waiting, Playing, Ended, Calibrating };

	public Node nodePrefab;

	[Range(0, 300)]
	public float magnetThreshold;

	[Range(0, 3)]
	public float invincibleLengthOnKill;

	[Range(0, 5)]
	public float invincibleLengthOnDamage;

	[Range(1, 5)]
	public int hpCount;

	[Range(0, 5)]
	public int startMinTimeBetweenAttacks;

	[Range(0, 1)]
	public float reduceMinTimeBeweenAttacks;

	[Range(0, 5)]
	public int startMaxTimeBetweenAttacks;

	[Range(0, 1)]
	public float reduceMaxTimeBeweenAttacks;

	[Range(1,10)]
	public int startAttackerCount;

	[Range(1,5)]
	public int increaseAttackerCount;

	[Range(1,10)]
	public int breakTime;

	[Range(1, 10)]
	public int countdownTime;


	public static Game Instance;

	private AudioSource audioSource1;
	private AudioSource audioSource2;

	private MusicManager music;
	private Display display;

	private List<Node> nodes;

	private List<Attacker> attackers;
	private List<Target> targets;

	private Stack<Attacker> attackerSchedule;

	private Phase phase;
	private bool isPractice;
	private int wave;
	private int positionInWave;
	private float minTimeBetweenAttacks;
	private float maxTimeBetweenAttacks;
	private int attackerCount;
	private bool invincible;
	private int hp;
	private int score;

	public AudioClip nodeHitSound;
	public AudioClip healSound;
	public List<AudioClip> attackerKilledSounds;
	public AudioClip attackerKilledScream;
	public AudioClip attackerTimeoutSound;
	public AudioClip waveStartSound;
	public AudioClip waveEndSound;
	public AudioClip gameStartSound;
	public AudioClip gameOverSound;

//	private IEnumerator ballCycleRoutine;

	void Awake() {
		Instance = this;
		AudioSource[] audioSources = gameObject.GetComponents<AudioSource>();
		audioSource1 = audioSources[0];
		audioSource2 = audioSources[1];
		music = GameObject.FindObjectOfType<MusicManager>();
		display = GameObject.FindObjectOfType<Display>();
	}

	void Start() {
		nodes = new List<Node>();
		attackers = new List<Attacker>();
		targets = new List<Target>();

		int count = UniMoveController.GetNumConnected();
		Debug.Log("Controllers connected: " + count);

		for (int i = 0; i < count; i++)
		{
			Node node = Instantiate(nodePrefab);
			nodes.Add(node);

			node.transform.SetParent(this.transform);
		}

		phase = Phase.Connecting;
	}

	// Update is called once per frame
	void Update () {
		if ( phase == Phase.Connecting ) {
			bool allConnected = true;
			for ( int i=0; i<nodes.Count; i++ ) {
				if ( nodes[i].controller == null ) {
					UniMoveController controller = nodes[i].gameObject.AddComponent<UniMoveController>();
					if ( controller.Init(i) ) {
						controller.SetLED(Color.white);
						controller.InitOrientation();
						controller.ResetOrientation();
						nodes[i].Init(controller);

						if ( nodes[i].type == 1 ) {
							Target target = nodes[i].gameObject.AddComponent<Target>();
							target.HitEvent += OnTargetHit;
							targets.Add(target);
						}
						else {
							Attacker attacker = nodes[i].gameObject.AddComponent<Attacker>();
							attacker.HitEvent += OnAttackerHit;
							attacker.TimeoutEvent += OnAttackerTimeout;
							attackers.Add(attacker);
						}

					} else {
						Destroy(controller);
						allConnected = false;
					}
				} 
			}
			if ( allConnected ) {
				phase = Phase.Waiting;
			}
		}

		else if ( phase == Phase.Waiting ) {
			// start game
			if ( Input.GetKeyDown(KeyCode.Space) ) {
				bool practice = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
				StartGame(practice);
			}
			if ( Input.GetKeyDown(KeyCode.C) ) {
				phase = Phase.Calibrating;
				foreach ( Node node in nodes ) {
					node.SetCalibrationAllowed(true);
				}
			}
		}

		else if ( phase == Phase.Playing ) {
			// stop game
			if ( Input.GetKeyDown(KeyCode.Space) ) {
				StopGame();
			}
		}

		else if ( phase == Phase.Ended ) {
			// stop game
			if ( Input.GetKeyDown(KeyCode.Space) ) {
				StopGame();
			}
		}

		else if ( phase == Phase.Calibrating ) {
			if ( Input.GetKeyDown(KeyCode.C) ) {
				phase = Phase.Waiting;
				foreach ( Node node in nodes ) {
					node.SetCalibrationAllowed(false);
				}
			}
		}

		if ( Input.GetKeyDown(KeyCode.D) ) {
			ControllerDebug.Active = !ControllerDebug.Active;
//			debugContainer.SetActive(!debugContainer.activeSelf);
		}
	}

	void StartGame(bool practice) {
		phase = Phase.Playing;
		isPractice = practice;

		invincible = false;
		hp = hpCount;
		score = 0;
		wave = 0;
		minTimeBetweenAttacks = startMinTimeBetweenAttacks;
		maxTimeBetweenAttacks = startMaxTimeBetweenAttacks;
		attackerCount = startAttackerCount;

		foreach ( Node node in nodes ) {
			node.ResetLEDAndRumble();
			node.inGame = true;
		}
		foreach ( Target target in targets ) {
			target.Reset();
		}
		foreach ( Attacker attacker in attackers ) {
			attacker.Reset();
		}

		// create a schedule of attackers. just keep shuffling the list and putting the results on top -- this ensures randomization with an equal distribution
		attackerSchedule = new Stack<Attacker>();
		for ( int i=0; i<999; i++ ) {
			attackers.Shuffle();

			// make sure 2 in a row doesn't happen
			while ( attackerSchedule.Count > 1 && attackerSchedule.Peek() == attackers[0] ) {
				attackers.Shuffle();
			}

			foreach ( Attacker a in attackers ) {
				attackerSchedule.Push(a);
			}
		}

		Invoke("StartNextWave", 3f);

		audioSource1.PlayOneShot(gameStartSound);
		display.ShowStart();
		display.SetLife(hp);
		display.SetScore(0);

		if ( !isPractice ) {
			music.StartGame();
		}
	}

	void KillProcesses() {
		CancelInvoke("SendAttacker");
		CancelInvoke("SendOverlappedAttacker");
		CancelInvoke("StopInvincible");
		CancelInvoke("Heal");
		CancelInvoke("StartNextWave");
	}


	void StopGame() {
		phase = Phase.Waiting;

		foreach ( Node node in nodes ) {
			node.inGame = false;
			node.ResetLEDAndRumble();
		}
		foreach ( Target target in targets ) {
			target.Reset();
		}
		foreach ( Attacker attacker in attackers ) {
			attacker.Reset();
		}

		KillProcesses();

		music.Reset();
		display.Reset(0, hpCount);
	}

	void EndGame() {
		phase = Phase.Ended;

		audioSource2.clip = gameOverSound;
		audioSource2.PlayDelayed(1f);

		foreach ( Target target in targets ) {
			target.SignalDead();
		}
		foreach ( Attacker attacker in attackers ) {
			attacker.Reset();
		}

		KillProcesses();

		music.Reset();
		display.ShowGameOver();
	}

	void StartNextWave() {
		wave++;
		positionInWave = 0;

		Invoke("SendAttacker", 2f);

		audioSource2.clip = waveStartSound;
		audioSource2.PlayDelayed(1f);
		display.ShowWaveStart(wave);

		if ( !isPractice ) {
			StartCoroutine(music.SetWave(wave, 0.5f));
		}
	}

	void EndWave() {
		Invoke("Heal", 3f);
		Invoke("StartNextWave", (float)breakTime);

		// increase difficulty
		attackerCount++;

		minTimeBetweenAttacks -= reduceMinTimeBeweenAttacks;
		if ( minTimeBetweenAttacks < 0 ) {
			minTimeBetweenAttacks = 0;
		}

		maxTimeBetweenAttacks -= reduceMaxTimeBeweenAttacks;
		if ( maxTimeBetweenAttacks < 0 ) {
			maxTimeBetweenAttacks = 0;
		}

		audioSource2.clip = waveStartSound;
		audioSource2.PlayDelayed(1f);
		display.ShowWaveEnd(wave);

		if ( !isPractice ) {
			StartCoroutine(music.SetBreak(0));
		}
	}

	void Heal() {
		if ( hp < hpCount ) {
			hp++;
			display.SetLife(hp);
			audioSource1.PlayOneShot(healSound);
		}
	}

	void SendAttacker() {
		attackerSchedule.Pop().Activate();
	} 

	void OnTargetHit(Target target) {
		if ( !invincible ) {
			audioSource1.PlayOneShot(nodeHitSound);

			hp--;

			display.SetLife(hp);
			display.ShowHit();

			if ( hp == 0 ) {
				EndGame();
				return;
			}

			foreach ( Target t in targets ) {
				t.SignalTookDamage();
			}
			StartInvincible();
			Invoke("StopInvincible", invincibleLengthOnDamage);

			// end current attackers
			foreach ( Attacker attacker in attackers ) {
				if ( attacker.IsAlive() ) {
					EndAttacker(attacker);
				}
			}
		}
	}

	void OnAttackerHit(Attacker attacker) {
		audioSource1.PlayOneShot(attackerKilledSounds[UnityEngine.Random.Range(0, attackerKilledSounds.Count-1)]);
		audioSource2.clip = attackerKilledScream;
		audioSource2.PlayDelayed(0.3f);

		EndAttacker(attacker);

		StartInvincible();
		Invoke("StopInvincible", invincibleLengthOnKill);

		score++;

		display.SetScore(score);
		display.ShowKill();
	}

	void OnAttackerTimeout(Attacker attacker) {
		audioSource1.PlayOneShot(attackerTimeoutSound);

		StartInvincible();
		Invoke("StopInvincible", invincibleLengthOnKill);

		EndAttacker(attacker);

		display.ShowTimeout();
	}

	void EndAttacker(Attacker attacker) {
		attacker.Kill();

		foreach ( Target t in targets ) {
			t.SignalDidDamage();
		}

		positionInWave++;
		if ( isPractice ) {
			Invoke("SendAttacker", 1f);
		}
		else if ( positionInWave < attackerCount ) {
			float time = UnityEngine.Random.Range(minTimeBetweenAttacks, maxTimeBetweenAttacks);
			Invoke("SendAttacker", time);
		}
		else {
			Invoke("EndWave", 1f);
		}
	}

	void StartInvincible() {
		CancelInvoke("StopInvincible");
		invincible = true;
	}

	void StopInvincible() {
		invincible = false;
		foreach ( Target t in targets ) {
			t.StopSignal();
		}
	}

}

static class MyExtensions
{
	private static System.Random rng = new System.Random();  

	public static void Shuffle<T>(this IList<T> list)  
	{  
		int n = list.Count;  
		while (n > 1) {  
			n--;  
			int k = rng.Next(n + 1);  
			T value = list[k];  
			list[k] = list[n];  
			list[n] = value;  
		}  
	}
}