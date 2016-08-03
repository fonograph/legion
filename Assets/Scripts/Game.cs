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

	[Range(1,10)]
	public int timeBetweenAttacks;

	[Range(1,10)]
	public int level1Attacks;

	[Range(1,10)]
	public int level2Attacks;

	[Range(1,100)]
	public int level3Attacks;

	public static Game Instance;

	private AudioSource audioSource;

	private List<Node> nodes;

	private List<Attacker> attackers;
	private List<Target> targets;

	private Phase phase;
//	private bool isPractice;

	private bool invincible;
	private int hp;
	private int level;
	private int attacks;

	private Stack<Attacker> attackerSchedule;

	public AudioClip nodeHitSound;
	public List<AudioClip> attackerKilledSounds;
	public AudioClip gameOverSound;

//	private IEnumerator ballCycleRoutine;

	void Awake() {
		Instance = this;
		audioSource = gameObject.GetComponent<AudioSource>();
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
			if ( Input.GetKeyDown(KeyCode.Alpha1) ) {
				attackers[0].Activate();
			}
			else if ( Input.GetKeyDown(KeyCode.Alpha2) ) {
				attackers[1].Activate();
			}
			else if ( Input.GetKeyDown(KeyCode.Alpha3) ) {
				attackers[2].Activate();
			}
			else if ( Input.GetKeyDown(KeyCode.Alpha4) ) {
				attackers[3].Activate();
			}

			// stop game
			else if ( Input.GetKeyDown(KeyCode.Space) ) {
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
		level = 1;
		attacks = 0;

		foreach ( Node node in nodes ) {
			node.Reset();
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
			foreach ( Attacker a in attackers ) {
				attackerSchedule.Push(a);
			}
		}

		Invoke("SendAttackers", timeBetweenAttacks);
	}


	void StopGame() {
		phase = Phase.Waiting;

		foreach ( Node node in nodes ) {
			node.inGame = false;
			node.ResetLEDAndRumble();
		}

		CancelInvoke("SendAttackers");
		CancelInvoke("StopInvincible");
	}

//	void EndGame() {
//		phase = Phase.Waiting;
//
//		foreach ( Node node in nodes ) {
//			node.inGame = false;
//			node.ResetLEDAndRumble();
//		}
//	}

	void SendAttackers() {
		int count = level;

		for ( int i=0; i<count; i++ ) {
			attackerSchedule.Pop().Activate();
		}

		attacks++;
	} 

	void OnTargetHit(Target target) {
		if ( !invincible ) {
			audioSource.PlayOneShot(nodeHitSound);

			hp--;
			if ( hp == 0 ) {
				audioSource.PlayOneShot(gameOverSound);
				StopGame();
				return;
			}

			foreach ( Target t in targets ) {
				t.SignalTookDamage();
			}
			StartInvincible();
			Invoke("StopInvincible", invincibleLengthOnDamage);
		}
	}

	void OnAttackerHit(Attacker attacker) {
		audioSource.PlayOneShot(attackerKilledSounds[UnityEngine.Random.Range(0, attackerKilledSounds.Count-1)]);
		attacker.Kill();

		foreach ( Target t in targets ) {
			t.SignalDidDamage();
		}
		StartInvincible();
		Invoke("StopInvincible", invincibleLengthOnKill);

		bool allDead = true;
		foreach ( Attacker a in attackers ) {
			if ( a.IsAlive() ) {
				allDead = false;
			}
		}

		if ( allDead ) {
			int levelTarget = level == 1 ? level1Attacks : level == 2 ? level2Attacks : level == 3 ? level3Attacks : 99999;
			if ( attacks >= levelTarget ) {
				level++;
				attacks = 0;
			}

			Invoke("SendAttackers", timeBetweenAttacks);
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