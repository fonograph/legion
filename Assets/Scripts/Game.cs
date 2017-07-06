using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

public class Game : MonoBehaviour {

	public enum Phase { Connecting, Waiting, Playing, Ended };

	public Node nodePrefab;

	[HideInInspector]
	public float magnetThresholdAttacker;

	[HideInInspector]
	public float magnetThresholdTarget;

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

	[HideInInspector]
	public bool countdownEnabled;


	public static Game Instance;

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

	public GameObject setupPanel;
	public Toggle timeoutToggle;
	public InputField targetSensitivityText;
	public InputField attackerSensitivityText;

	public AudioSource nodeHitAudioSource;
	public AudioSource healAudioSource;
	public AudioSource healthAudioSource;
	public AudioSource attackerSlashAudioSource;
	public AudioSource attackerScreamAudioSource;
	public AudioSource waveEventAudioSource;
	public AudioSource gameEventAudioSource;

	public AudioClip nodeHitSound;
	public AudioClip healSound;
	public AudioClip healthSound;
	public List<AudioClip> attackerKilledSounds;
	public List<AudioClip> attackerKilledScream;
	public AudioClip attackerTimeoutSound;
	public AudioClip waveStartSound;
	public AudioClip waveEndSound;
	public AudioClip gameStartSound;
	public AudioClip gameOverSound;

//	private IEnumerator ballCycleRoutine;

	void Awake() {
		Instance = this;
		AudioSource[] audioSources = gameObject.GetComponents<AudioSource>();
		music = GameObject.FindObjectOfType<MusicManager>();
		display = GameObject.FindObjectOfType<Display>();
	}

	void Start() {
		nodes = new List<Node>();
		attackers = new List<Attacker>();
		targets = new List<Target>();

		InitSetup();

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
							attacker.TimeoutEvent += OnAttackerHit;
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

		if ( Input.GetKeyDown(KeyCode.D) ) {
			ControllerDebug.Active = !ControllerDebug.Active;
//			debugContainer.SetActive(!debugContainer.activeSelf);
		}
	}

	void InitSetup() {
		countdownEnabled = PlayerPrefs.GetInt("countdownEnabled")==1;
		magnetThresholdTarget = PlayerPrefs.GetInt("magnetThresholdTarget");
		magnetThresholdAttacker = PlayerPrefs.GetInt("magnetThresholdAttacker");

		timeoutToggle.isOn = countdownEnabled;
		targetSensitivityText.text = magnetThresholdTarget.ToString();
		attackerSensitivityText.text = magnetThresholdAttacker.ToString();
	}

	public void UpdateSetup() {
		countdownEnabled = timeoutToggle.isOn;
		magnetThresholdTarget = int.Parse(targetSensitivityText.text);
		magnetThresholdAttacker = int.Parse(attackerSensitivityText.text);

		PlayerPrefs.SetInt("countdownEnabled", countdownEnabled?1:0);
		PlayerPrefs.SetInt("magnetThresholdTarget", (int)magnetThresholdTarget);
		PlayerPrefs.SetInt("magnetThresholdAttacker", (int)magnetThresholdAttacker);
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

		// only put active attackers in the schedule of attackers
		List<Attacker> activeAttackers = new List<Attacker>();
		foreach ( Attacker attacker in attackers ) {
			if ( attacker.IsActive() ) {
				activeAttackers.Add(attacker);
			}
		}

		// create a schedule of attackers. just keep shuffling the list and putting the results on top -- this ensures randomization with an equal distribution
		attackerSchedule = new Stack<Attacker>();
		for ( int i=0; i<999; i++ ) {
			activeAttackers.Shuffle();

			// make sure 2 in a row doesn't happen
			while ( attackerSchedule.Count > 1 && activeAttackers.Count > 1 && attackerSchedule.Peek() == activeAttackers[0] ) {
				activeAttackers.Shuffle();
			}

			foreach ( Attacker a in activeAttackers ) {
				attackerSchedule.Push(a);
			}
		}

		Invoke("StartNextWave", 3f);

		gameEventAudioSource.PlayOneShot(gameStartSound);
		display.ShowStart();
		display.SetLife(hp);
		display.SetScore(0);

		if ( !isPractice ) {
			music.StartGame();
		}

		setupPanel.SetActive(false);
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
		setupPanel.SetActive(true);
	}

	void EndGame() {
		phase = Phase.Ended;

		gameEventAudioSource.clip = gameOverSound;
		gameEventAudioSource.PlayDelayed(1f);

		foreach ( Target target in targets ) {
			target.SignalDead();
		}
		foreach ( Attacker attacker in attackers ) {
			attacker.Reset();
		}

		KillProcesses();

		music.Reset();
		display.ShowGameOver();
		setupPanel.SetActive(true);
	}

	void StartNextWave() {
		wave++;
		positionInWave = 0;

		Invoke("SendAttacker", 2f);

		waveEventAudioSource.clip = waveStartSound;
		waveEventAudioSource.PlayDelayed(1f);
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

		waveEventAudioSource.clip = waveEndSound;
		waveEventAudioSource.PlayDelayed(1f);
		display.ShowWaveEnd(wave);

		if ( !isPractice ) {
			StartCoroutine(music.SetBreak(0));
		}
	}

	void Heal() {
		if ( hp < hpCount ) {
			hp++;
			display.SetLife(hp);
			healAudioSource.PlayOneShot(healSound);
		}
		Invoke("PlayHealthAudio", 1.5f);
	}

	void SendAttacker() {
		attackerSchedule.Pop().Activate();
	} 

	void OnTargetHit(Target target) {
		if ( !invincible ) {
			nodeHitAudioSource.PlayOneShot(nodeHitSound);

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
		attackerSlashAudioSource.PlayOneShot(attackerKilledSounds[UnityEngine.Random.Range(0, attackerKilledSounds.Count-1)]);
		attackerScreamAudioSource.clip = attackerKilledScream[UnityEngine.Random.Range(0, attackerKilledScream.Count-1)];
		attackerScreamAudioSource.PlayDelayed(0.3f);

		EndAttacker(attacker);

		StartInvincible();
		Invoke("StopInvincible", invincibleLengthOnKill);

		score++;

		display.SetScore(score);
		display.ShowKill();
	}

	void OnAttackerTimeout(Attacker attacker) {
		attackerSlashAudioSource.PlayOneShot(attackerTimeoutSound);

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

	void PlayHealthAudio() {
		StartCoroutine(_PlayHealthAudio());
	}

	IEnumerator _PlayHealthAudio() {
		for ( int i=0; i<hp; i++ ) {
			healAudioSource.pitch = i*1;
			healthAudioSource.PlayOneShot(healthSound);	
			yield return new WaitForSeconds(0.3f);
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