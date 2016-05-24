using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

public class Game : MonoBehaviour {

	public enum Phase { Connecting, Waiting, Playing, Ended, Calibrating };

	public Node nodePrefab;

	[Range(1, 2)]
	public int gameMode;

	[Range(0, 300)]
	public float magnetThreshold;

	[Range(0, 3)]
	public float invincibleLengthOnKill;

	[Range(0, 5)]
	public float invincibleLengthOnDamage;

	[Range(1, 5)]
	public int hpCount;

	public static Game Instance;

	private AudioSource audioSource;

	private List<Node> nodes;

	private List<Attacker> attackers;
	private List<Target> targets;

	private Phase phase;
//	private bool isPractice;

	private bool invincible;
	private int hp;

	public AudioClip nodeHitSound;
	public AudioClip attackerKilledSound;
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
						if ( gameMode == 1 ) {
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
						}
						else {
							Target target = nodes[i].gameObject.AddComponent<Target>();
							target.HitEvent += OnTargetHit;
							targets.Add(target);
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

//		passTimeoutRoutine = null;
	}


	void StopGame() {
		phase = Phase.Waiting;

		foreach ( Node node in nodes ) {
			node.inGame = false;
			node.ResetLEDAndRumble();
		}
	}

	void EndGame() {
		phase = Phase.Waiting;

		foreach ( Node node in nodes ) {
			node.inGame = false;
			node.ResetLEDAndRumble();
		}
	}

	void OnTargetHit(Target target) {
		if ( gameMode == 1 ) {
			if ( !invincible ) {
				audioSource.PlayOneShot(nodeHitSound);
				target.Kill();

				hp--;
				if ( hp == 0 ) {
					audioSource.PlayOneShot(gameOverSound);
					EndGame();
				}

				StartInvincible();
				Invoke("StopInvincible", invincibleLengthOnDamage);
			}
		}
		else {
			audioSource.PlayOneShot(target.node.type == 1 ? nodeHitSound : attackerKilledSound);
			target.Kill();
		}
	}

	void OnAttackerHit(Attacker attacker) {
		audioSource.PlayOneShot(attackerKilledSound);
		attacker.Kill();

		StartInvincible();
		Invoke("StopInvincible", invincibleLengthOnKill);
	}

	void StartInvincible() {
		CancelInvoke("StopInvincible");
		invincible = true;
	}

	void StopInvincible() {
		invincible = false;
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
