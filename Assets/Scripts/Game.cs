using UnityEngine;
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

	[Range(1, 5)]
	public int startTimeBetweenAttacks;

	[Range(0, 1)]
	public float reduceTimeBeweenAttacks;

	[Range(1, 10)]
	public int startTimeForOverlappedAttacks;

	[Range(0, 1)]
	public float reduceTimeForOverlappedAttacks;

	[Range(4, 10)]
	public int countdownTime;


	public static Game Instance;

	private AudioSource audioSource1;
	private AudioSource audioSource2;

	private MusicManager music;

	private List<Node> nodes;

	private List<Attacker> attackers;
	private List<Target> targets;

	private float timeBetweenAttacks;
	private float timeForOverlappedAttacks;

	private DateTime mostRecentAttackerStartTime;

	private Phase phase;
//	private bool isPractice;

	private bool invincible;
	private int hp;

	private Stack<Attacker> attackerSchedule;

	private int activeAttackerCount;

	public AudioClip nodeHitSound;
	public List<AudioClip> attackerKilledSounds;
	public AudioClip attackerKilledScream;
	public AudioClip attackerTimeoutSound;
	public AudioClip gameOverSound;

//	private IEnumerator ballCycleRoutine;

	void Awake() {
		Instance = this;
		AudioSource[] audioSources = gameObject.GetComponents<AudioSource>();
		audioSource1 = audioSources[0];
		audioSource2 = audioSources[1];
		music = GameObject.FindObjectOfType<MusicManager>();
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
//			debugContainer.SetActive(!debugContainer.activeSelf);
		}
	}

	void StartGame(bool practice) {
		phase = Phase.Playing;
//		isPractice = practice;

		invincible = false;
		hp = hpCount;

		timeBetweenAttacks = startTimeBetweenAttacks;
		timeForOverlappedAttacks = startTimeForOverlappedAttacks;

		activeAttackerCount = 0;

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

		Invoke("SendAttacker", timeBetweenAttacks);

		music.StartGame();
		music.SetAttackers(activeAttackerCount);
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

		CancelInvoke("SendAttacker");
		CancelInvoke("SendOverlappedAttacker");
		CancelInvoke("StopInvincible");

		music.Reset();
	}

	void EndGame() {
		phase = Phase.Waiting;

		audioSource2.clip = gameOverSound;
		audioSource2.PlayDelayed(1f);

		music.Reset();

		foreach ( Target target in targets ) {
			target.SignalDead();
		}
		foreach ( Attacker attacker in attackers ) {
			attacker.Reset();
		}
	}

	void SendAttacker() {
		attackerSchedule.Pop().Activate();

		music.SetAttackers(++activeAttackerCount);
	} 

	void SendOverlappedAttacker() {
		SendAttacker();
	}

	void OnTargetHit(Target target) {
		if ( !invincible ) {
			audioSource1.PlayOneShot(nodeHitSound);

			hp--;
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
	}

	void OnAttackerTimeout(Attacker attacker) {
		audioSource1.PlayOneShot(attackerTimeoutSound);

		EndAttacker(attacker);
	}

	void EndAttacker(Attacker attacker) {
		attacker.Kill();

		foreach ( Target t in targets ) {
			t.SignalDidDamage();
		}
		StartInvincible();
		Invoke("StopInvincible", invincibleLengthOnKill);

		music.SetAttackers(--activeAttackerCount);

		bool allDead = true;
		foreach ( Attacker a in attackers ) {
			if ( a.IsAlive() ) {
				allDead = false;
			}
		}

		CancelInvoke("SendOverlappedAttacker");
		bool sendOverlap = false;

		if ( allDead ) {
			if ( timeBetweenAttacks < 0 ) { // check this first, so a 0-count timeBetweenAttacks gets to execute before overlap begins
				timeBetweenAttacks = 0;
				sendOverlap = true;
			}
			Invoke("SendAttacker", timeBetweenAttacks);
			timeBetweenAttacks -= reduceTimeBeweenAttacks;
		}
		else {
			sendOverlap = true; // there's a still an attacker in there who was overlapping, so queue another overlap
		}

		if ( sendOverlap ) {
			Invoke("SendOverlappedAttacker", timeForOverlappedAttacks);
			timeForOverlappedAttacks -= reduceTimeForOverlappedAttacks;
			if ( timeForOverlappedAttacks < 0 ) {
				timeForOverlappedAttacks = 0;
			} 
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


//	IEnumerator WaitAndPlayAnnouncerEnding(float seconds) {
//		yield return new WaitForSeconds(seconds);
//		announcer.PlayEnding(scores[1] > scores[2] ? TeamName1 : TeamName2);
//	}
	

//	IEnumerator WaitAndTimeoutPass(float seconds) {
//		yield return new WaitForSeconds(seconds);
//		passTimeoutRoutine = null;
//
//		foreach ( Player p in players ) {
//			p.PassBallOff();
//		}
//
//		playerCycle = GetRandomizedPlayers();
//		playerCycle.Remove(lastHoldingPlayer);
//		
//		playerCycleIdx = 0;
//		playerCycleCount = 0;
//		CycleBall(false);
//	}



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