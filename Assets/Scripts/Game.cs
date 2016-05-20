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

	[Range(0, 10)]
	public float cycleLength;

	[Range(0, 5)]
	public float coreDamagePenalty;

	[Range(0, 5)]
	public float attackLength;

	public float attackAccelThreshold;

	[Range(1, 6)]
	public int gameOverHits;

	public static Game Instance;

	private AudioSource audioSource;

	private List<Node> nodes;
	private int activeNodeIdx;

	public List<Attacker> attackers;
	private int activeAttackerIdx;

	private Phase phase;
	private bool isPractice;

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

		int count = UniMoveController.GetNumConnected();
		Debug.Log("Controllers connected: " + count);

		for (int i = 0; i < count; i++)
		{
			Node node = Instantiate(nodePrefab);
			node.HitEvent += OnNodeHit;
			node.AttackEvent += OnNodeAttack;
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

			double activeTime = DateTime.Now.Subtract(nodes[activeNodeIdx].activeAt).TotalSeconds;
			if ( activeTime > CurrentCycleLength() ) {
				// next
				nodes[activeNodeIdx].SetActive(false);
				SelectNextNode();
				nodes[activeNodeIdx].SetActive(true);
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
		isPractice = practice;

		foreach ( Node node in nodes ) {
			node.Reset();
			node.inGame = true;
		}

		activeNodeIdx = -1;
		SelectNextNode();
		nodes[activeNodeIdx].SetActive(true);
		
		activeAttackerIdx = -1;
		SelectNextAttacker();
		Invoke("AnnounceAttacker", 1f);

//		passTimeoutRoutine = null;
	}


	void StopGame() {
		phase = Phase.Waiting;

//		if ( scoreRoutine != null ) StopCoroutine(scoreRoutine);

		foreach ( Node node in nodes ) {
			node.inGame = false;
			node.ResetLEDAndRumble();
		}
	}

	void EndGame() {
		phase = Phase.Waiting;

//		if ( scoreRoutine != null ) StopCoroutine(scoreRoutine);

		foreach ( Node node in nodes ) {
			node.inGame = false;
			node.ResetLEDAndRumble();
		}

		audioSource.PlayOneShot(gameOverSound);

//		StartCoroutine(WaitAndPlayAnnouncerEnding(3));
//		StartCoroutine(crowd.WaitAndDeactivate(8));
	}

	void OnNodeHit(Node node) {
		audioSource.PlayOneShot(nodeHitSound);

		// Game Over?
		int hitCount = 0;
		foreach ( Node n in nodes ) {
			if ( !n.alive ) {
				hitCount++;
			}
		}
		if ( hitCount >= gameOverHits ) {
			EndGame();
		}
	}

	void OnNodeAttack(Node node) {
		// kill current attacker
		audioSource.PlayOneShot(attackerKilledSound);

		SelectNextAttacker();

		Invoke("AnnounceAttacker", 1f);
	}

	void AnnounceAttacker() {
		attackers[activeAttackerIdx].Announce();
	}

	void SelectNextNode() {
		do {
			activeNodeIdx++;
			if ( activeNodeIdx == nodes.Count ) {
				activeNodeIdx = 0;
			}
		} while ( nodes[activeNodeIdx].core );
	}

	void SelectNextAttacker() {
		do {
			activeAttackerIdx++;
			if ( activeAttackerIdx == attackers.Count ) {
				activeAttackerIdx = 0;
			}
		} while ( !attackers[activeAttackerIdx].gameObject.activeSelf );
	}

	public float CurrentCycleLength() {
		float length = cycleLength;
		foreach ( Node node in nodes ) {
			if ( node.core && !node.alive ) {
				length += coreDamagePenalty;
			}
		}
		return length;
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
