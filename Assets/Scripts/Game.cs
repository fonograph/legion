using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

public class Game : MonoBehaviour {

	public enum Phase { Connecting, Waiting, Playing, Ended };

	public Node nodePrefab;

	[Range(0, 300)]
	public float magnetThresholdAttacker;

	[Range(0, 300)]
	public float magnetThresholdTarget;

	[Range(0, 5)]
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
	public int maxWave;

	[Range(1,10)]
	public int breakTime;

	[Range(1, 30)]
	public int countdownTime;

	public bool countdownEnabled;

	private float sfxVolume = 0;

	private float musicVolume = 0;

	private float voVolume = 0;


	public static Game Instance;


	[HideInInspector]
	public bool isPractice;
	private MusicManager music;
	private Display display;
	private List<Node> nodes;
	private List<Attacker> attackers;
	private List<Target> targets;
	private Stack<Attacker> attackerSchedule;
	private bool setupInited;
	private Phase phase;
	private int wave;
	private int positionInWave;
	private float minTimeBetweenAttacks;
	private float maxTimeBetweenAttacks;
	private int attackerCount;
	private bool invincible;
	private int hp;
	private int score;
	

	public GameObject setupPanel;
	public Toggle countdownEnabledToggle;
	public InputField magnetThresholdTargetText;
	public InputField magnetThresholdAttackerText;
	public InputField invincibleLengthOnDamageText;
	public InputField invincibleLengthOnKillText;
	public InputField hpCountText;
	public InputField startMinTimeBetweenAttacksText;
	public InputField reduceMinTimeBeweenAttacksText;
	public InputField startMaxTimeBetweenAttacksText;
	public InputField reduceMaxTimeBeweenAttacksText;
	public InputField startAttackerCountText;
	public InputField increaseAttackerCountText;
	public InputField breakTimeText;
	public InputField countdownTimeText;
	public InputField maxWaveText;
	public InputField sfxVolumeText;
	public InputField musicVolumeText;
	public InputField voVolumeText;

	public Button[] debugButtons;
	public GameObject debugPanel;

	public AudioMixer audioMixer;
	public AudioSource nodeHitAudioSource;
	public AudioSource healAudioSource;
	public AudioSource healthAudioSource;
	public AudioSource attackerSlashAudioSource;
	public AudioSource attackerScreamAudioSource;
	public AudioSource waveEventAudioSource;
	public AudioSource gameEventAudioSource;
	public AudioSource voAudioSource;

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
	public AudioClip[] voStart;
	public AudioClip[] voHit;
	public AudioClip[] voHealth;
	public AudioClip[] voWaveCompleted;
	public AudioClip[] voGameOver;
	public AudioClip[] voCountdownWarning;
	public AudioClip[] voCountdownOver;

	private int voVariationIndex = -1;

//	private IEnumerator ballCycleRoutine;

	void Awake() {
		Instance = this;
		music = GameObject.FindObjectOfType<MusicManager>();
		display = GameObject.FindObjectOfType<Display>();
	}

	void Start() {
		nodes = new List<Node>();
		attackers = new List<Attacker>();
		targets = new List<Target>();

		InitSetup();
		setupInited = true;

		int count = UniMoveController.GetNumConnected();
		Debug.Log("Controllers connected: " + count);

		if (count == 0) {
			count = 6; // debug mode
		}

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
				if (UniMoveController.GetNumConnected() > 0) {
					if ( nodes[i].controller == null ) {
						UniMoveController controller = nodes[i].gameObject.AddComponent<UniMoveController>();
						if ( controller.Init(i) ) {
							controller.InitOrientation();
							controller.ResetOrientation();
							nodes[i].Init(controller, this.debugButtons[i]);
						} else {
							Destroy(controller);
							allConnected = false;
						}
					} 
				}
				else {
					nodes[i].InitDebug(i<=1?1:2, this.debugButtons[i]);
				}
			}

			if ( allConnected ) {
				for ( int i=0; i<nodes.Count; i++ ) {
					if ( nodes[i].type == 1 ) {
						Target target = nodes[i].gameObject.AddComponent<Target>();
						target.HitEvent += OnTargetHit;
						targets.Add(target);
					}
					else {
						Attacker attacker = nodes[i].gameObject.AddComponent<Attacker>();
						attacker.HitEvent += OnAttackerHit;
						attacker.TimeoutEvent += OnAttackerTimeout;
						attacker.TimeoutWarningEvent += OnAttackerTimeoutWarning;
						attackers.Add(attacker);
					}
				}

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

			// heal
			if ( Input.GetKeyDown(KeyCode.H) ) {
				Heal();
			}
		}

		else if ( phase == Phase.Ended ) {
		}

		if ( Input.GetKeyDown(KeyCode.D) ) {
			//ControllerDebug.Active = !ControllerDebug.Active;
			debugPanel.SetActive(!debugPanel.activeSelf);
		}
		if ( Input.GetKeyDown(KeyCode.S) ) {
			setupPanel.SetActive(!setupPanel.activeSelf);
		}
	}

	private int GetPrefIntOrDefault(string key, int defaultVal) {
		return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : defaultVal;
	}
	private float GetPrefFloatOrDefault(string key, float defaultVal) {
		return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetFloat(key) : defaultVal;
	}

	void InitSetup() {
		countdownEnabled = PlayerPrefs.GetInt("countdownEnabled")==1;
		magnetThresholdTarget = GetPrefFloatOrDefault("magnetThresholdTarget", magnetThresholdTarget);
		magnetThresholdAttacker = GetPrefFloatOrDefault("magnetThresholdAttacker", magnetThresholdAttacker);
		invincibleLengthOnDamage = GetPrefFloatOrDefault("invincibleLengthOnDamage", invincibleLengthOnDamage);
		invincibleLengthOnKill = GetPrefFloatOrDefault("invincibleLengthOnKill", invincibleLengthOnKill);
		hpCount = GetPrefIntOrDefault("hpCount", hpCount);
		startMinTimeBetweenAttacks = GetPrefIntOrDefault("startMinTimeBetweenAttacks", startMinTimeBetweenAttacks);
		reduceMinTimeBeweenAttacks = GetPrefFloatOrDefault("reduceMinTimeBeweenAttacks", reduceMinTimeBeweenAttacks);
		startMaxTimeBetweenAttacks = GetPrefIntOrDefault("startMaxTimeBetweenAttacks", startMaxTimeBetweenAttacks);
		reduceMaxTimeBeweenAttacks = GetPrefFloatOrDefault("reduceMaxTimeBeweenAttacks", reduceMaxTimeBeweenAttacks);
		startAttackerCount = GetPrefIntOrDefault("startAttackerCount", startAttackerCount);
		increaseAttackerCount = GetPrefIntOrDefault("increaseAttackerCount", increaseAttackerCount);
		breakTime = GetPrefIntOrDefault("breakTime", breakTime);
		countdownTime = GetPrefIntOrDefault("countdownTime", countdownTime);
		maxWave = GetPrefIntOrDefault("maxWave", maxWave);
		sfxVolume = GetPrefFloatOrDefault("sfxVolume", sfxVolume);
		musicVolume = GetPrefFloatOrDefault("musicVolume", musicVolume);
		voVolume = GetPrefFloatOrDefault("voVolume", voVolume);

		countdownEnabledToggle.isOn = countdownEnabled;
		magnetThresholdTargetText.text = magnetThresholdTarget.ToString();
		magnetThresholdAttackerText.text = magnetThresholdAttacker.ToString();
		invincibleLengthOnDamageText.text = invincibleLengthOnDamage.ToString();
		invincibleLengthOnKillText.text = invincibleLengthOnKill.ToString();
		hpCountText.text = hpCount.ToString();
		startMinTimeBetweenAttacksText.text = startMinTimeBetweenAttacks.ToString();
		reduceMinTimeBeweenAttacksText.text = reduceMinTimeBeweenAttacks.ToString();
		startMaxTimeBetweenAttacksText.text = startMaxTimeBetweenAttacks.ToString();
		reduceMaxTimeBeweenAttacksText.text = reduceMaxTimeBeweenAttacks.ToString();
		startAttackerCountText.text = startAttackerCount.ToString();
		increaseAttackerCountText.text = increaseAttackerCount.ToString();
		breakTimeText.text = breakTime.ToString();
		countdownTimeText.text = countdownTime.ToString();
		maxWaveText.text = maxWave.ToString();
		sfxVolumeText.text = sfxVolume.ToString();
		musicVolumeText.text = musicVolume.ToString();
		voVolumeText.text = voVolume.ToString();

		audioMixer.SetFloat("sfxVolume", sfxVolume);
		audioMixer.SetFloat("musicVolume", musicVolume);
		audioMixer.SetFloat("voVolume", voVolume);
	}

	public void UpdateSetup() {
		if ( !setupInited ) {
			return;
		}

		countdownEnabled = countdownEnabledToggle.isOn;
		magnetThresholdTarget = int.Parse(magnetThresholdTargetText.text);
		magnetThresholdAttacker = int.Parse(magnetThresholdAttackerText.text);
		invincibleLengthOnDamage = int.Parse(invincibleLengthOnDamageText.text);
		invincibleLengthOnKill = int.Parse(invincibleLengthOnKillText.text);
		hpCount = int.Parse(hpCountText.text);
		startMinTimeBetweenAttacks = int.Parse(startMinTimeBetweenAttacksText.text);
		reduceMinTimeBeweenAttacks = float.Parse(reduceMinTimeBeweenAttacksText.text);
		startMaxTimeBetweenAttacks = int.Parse(startMaxTimeBetweenAttacksText.text);
		reduceMaxTimeBeweenAttacks = float.Parse(reduceMaxTimeBeweenAttacksText.text);
		startAttackerCount = int.Parse(startAttackerCountText.text);
		increaseAttackerCount = int.Parse(increaseAttackerCountText.text);
		breakTime = int.Parse(breakTimeText.text);
		countdownTime = int.Parse(countdownTimeText.text);
		maxWave = int.Parse(maxWaveText.text);
		sfxVolume = float.Parse(sfxVolumeText.text);
		musicVolume = float.Parse(musicVolumeText.text);
		voVolume = float.Parse(voVolumeText.text);

		PlayerPrefs.SetInt("countdownEnabled", countdownEnabled?1:0);
		PlayerPrefs.SetFloat("magnetThresholdTarget", magnetThresholdTarget);
		PlayerPrefs.SetFloat("magnetThresholdAttacker", magnetThresholdAttacker);
		PlayerPrefs.SetFloat("invincibleLengthOnDamage", invincibleLengthOnDamage);
		PlayerPrefs.SetFloat("invincibleLengthOnKill", invincibleLengthOnKill);
		PlayerPrefs.SetInt("hpCount", hpCount);
		PlayerPrefs.SetInt("startMinTimeBetweenAttacks", startMinTimeBetweenAttacks);
		PlayerPrefs.SetFloat("reduceMinTimeBeweenAttacks", reduceMinTimeBeweenAttacks);
		PlayerPrefs.SetInt("startMaxTimeBetweenAttacks", startMaxTimeBetweenAttacks);
		PlayerPrefs.SetFloat("reduceMaxTimeBeweenAttacks", reduceMaxTimeBeweenAttacks);
		PlayerPrefs.SetInt("startAttackerCount", startAttackerCount);
		PlayerPrefs.SetInt("increaseAttackerCount", increaseAttackerCount);
		PlayerPrefs.SetInt("breakTime", breakTime);
		PlayerPrefs.SetInt("countdownTime", countdownTime);
		PlayerPrefs.SetInt("maxWave", maxWave);
		PlayerPrefs.SetFloat("sfxVolume", sfxVolume);
		PlayerPrefs.SetFloat("musicVolume", musicVolume);
		PlayerPrefs.SetFloat("voVolume", voVolume);
		PlayerPrefs.Save();

		audioMixer.SetFloat("sfxVolume", sfxVolume);
		audioMixer.SetFloat("musicVolume", musicVolume);
		audioMixer.SetFloat("voVolume", voVolume);
	}

	public void ResetSetup() {
		PlayerPrefs.DeleteAll();
		this.InitSetup();
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

		display.ShowStart();
		display.SetLife(hp);
		display.SetScore(0);
		display.SetPractice(isPractice);

		if ( !isPractice ) {
			music.StartGame();

			gameEventAudioSource.PlayOneShot(gameStartSound);

			voVariationIndex++;

			voAudioSource.clip = voStart[voVariationIndex % voStart.Length];
			voAudioSource.PlayDelayed(1);
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

		foreach ( Target target in targets ) {
			target.Reset();
		}
		foreach ( Attacker attacker in attackers ) {
			attacker.Reset();
		}
		foreach ( Node node in nodes ) {
			node.inGame = false;
			node.ResetLEDAndRumble();
		}

		KillProcesses();

		music.Reset();
		display.Reset(0, hpCount);
	}

	void EndGame() {
		phase = Phase.Ended;

		gameEventAudioSource.clip = gameOverSound;
		gameEventAudioSource.PlayDelayed(1f);

		voAudioSource.clip = voGameOver[voVariationIndex % voGameOver.Length];
		voAudioSource.PlayDelayed(1.5f);

		foreach ( Target target in targets ) {
			target.SignalDead();
		}
		foreach ( Attacker attacker in attackers ) {
			attacker.Reset();
		}

		KillProcesses();

		music.Reset();
		display.ShowGameOver();

		Invoke("StopGame", 5);
	}

	void StartNextWave() {
		wave++;
		positionInWave = 0;

		// waveEventAudioSource.clip = waveStartSound;
		// waveEventAudioSource.PlayDelayed(1f);
		display.ShowWaveStart(wave);

		SendAttacker();

		if ( !isPractice ) {
			StartCoroutine(music.SetWave(wave, 0f));
		}
	}

	void EndWave() {
		// increase difficulty
		attackerCount += increaseAttackerCount;

		minTimeBetweenAttacks -= reduceMinTimeBeweenAttacks;
		if ( minTimeBetweenAttacks < 0 ) {
			minTimeBetweenAttacks = 0;
		}

		maxTimeBetweenAttacks -= reduceMaxTimeBeweenAttacks;
		if ( maxTimeBetweenAttacks < 0 ) {
			maxTimeBetweenAttacks = 0;
		}

		display.ShowWaveEnd(wave);

		waveEventAudioSource.PlayOneShot(waveEndSound);

		voAudioSource.clip = voWaveCompleted[voVariationIndex % voWaveCompleted.Length];
		voAudioSource.PlayDelayed(0.5f);

		if (hp < hpCount) {
			Invoke("Heal", 2f);
			Invoke("StartNextWave", (float)breakTime + 2f);
		} else {
			Invoke("PlayHealthAudio", 2f);
			Invoke("StartNextWave", (float)breakTime);
		}

		if ( !isPractice ) {
			StartCoroutine(music.SetBreak(0));
		}
	}

	void Heal() {
		if ( hp < hpCount ) {
			hp++;
			display.SetLife(hp);
			healAudioSource.PlayOneShot(healSound);
			Invoke("PlayHealthAudio", 1f);
			foreach ( Target t in targets ) {
				t.SignalHeal();
			}
		}
	}

	void SendAttacker() {
		attackerSchedule.Pop().Activate();
	} 

	void OnTargetHit(Target target) {
		if ( !invincible ) {
			nodeHitAudioSource.PlayOneShot(nodeHitSound);

			if ( !isPractice ) {
				hp--;
				if (hp > 0) { 
					voAudioSource.clip = voHit[voVariationIndex % voHit.Length];
					voAudioSource.PlayDelayed(0.3f);
					//dont play VO on final attacker because it will overlap with the wave end stuff
					if (positionInWave < attackerCount - 1) {
						Invoke("PlayHealthAudio", 1.5f);
					}
				}
			}

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
					EndAttacker(attacker, false);
				}
			}
		}
	}

	void OnAttackerHit(Attacker attacker) {
		attackerSlashAudioSource.PlayOneShot(attackerKilledSounds[UnityEngine.Random.Range(0, attackerKilledSounds.Count-1)]);
		attackerScreamAudioSource.clip = attackerKilledScream[UnityEngine.Random.Range(0, attackerKilledScream.Count-1)];
		attackerScreamAudioSource.PlayDelayed(0.3f);

		EndAttacker(attacker, true);

		foreach ( Target t in targets ) {
			t.SignalDidDamage();
		}
		StartInvincible();
		Invoke("StopInvincible", invincibleLengthOnKill);

		score++;

		display.SetScore(score);
		display.ShowKill();
	}

	void OnAttackerTimeout(Attacker attacker) {
		OnAttackerHit(attacker);

		voAudioSource.clip = voCountdownOver[voVariationIndex % voHit.Length];
		voAudioSource.PlayDelayed(0.3f);
	}

	void OnAttackerTimeoutWarning(Attacker attacker) {
		voAudioSource.PlayOneShot(voCountdownWarning[voVariationIndex % voHit.Length]);
	}

	void EndAttacker(Attacker attacker, bool wasKilled) {
		attacker.Kill();

		positionInWave++;
		if ( isPractice ) {
			Invoke("SendAttacker", 1f);
		}
		else if ( positionInWave < attackerCount || wave >= maxWave ) {
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
			t.StopSignal(Target.SignalType.TookOrDidDamage);
		}
	}

	void PlayHealthAudio() {
		if (hp > 0) {
			voAudioSource.PlayOneShot(voHealth[hp-1 + 5*(voVariationIndex%2)]);
		}
		//StartCoroutine(_PlayHealthAudio());
	}

	IEnumerator _PlayHealthAudio() {
		for ( int i=0; i<hp; i++ ) {
			healthAudioSource.pitch = 1 + i*0.05f;
			healthAudioSource.PlayOneShot(healthSound);	
			yield return new WaitForSeconds(0.2f);
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