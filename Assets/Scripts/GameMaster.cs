using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using FMODUnity;
using TMPro;
using UnityEngine.AI;
using DG.Tweening;

public class GameMaster : MonoBehaviour
{
    public static GameMaster Instance {get; private set;}
    
	[SerializeField] private GameObject decoyPrefab;
	private readonly List<GameObject> decoys = new List<GameObject>();

    [SerializeField] public GameObject player;

    [SerializeField] private Transform point1;
    [SerializeField] private Transform point2;
    [SerializeField] private Transform point3;
    [SerializeField] private Transform point4;

    // UI References (assign in Inspector)
    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timerText; // hidden initially; shows live timer after score >= 25
    [SerializeField] private TMP_Text milestoneText; 
    [SerializeField] private TMP_Text gameOverTimerText; 
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private TMP_Text gameOverScoreText;
    [SerializeField] private TMP_Text gameOverSubtext;
    [SerializeField] private TMP_Text gameOverLabel;
    [SerializeField] private float gameOverScaleDuration = 0.6f;
    [SerializeField] private Ease gameOverEase = Ease.OutBack;
    public GameObject sillyKitty;
    public int Score { get; private set; }
	

    //FMOD references
    [Header("Audio")]
    public EventReference DistractEvent;
    public EventReference AlertEvent;
    public EventReference LostEvent;
    public EventReference GameOverEvent;
    public EventReference collectEvent;
    public FMODUnity.StudioEventEmitter GameMusicEvent {get; private set;}
    
    private void Awake() 
    { 
        // If there is an instance, and it's not me, delete myself.
    
        if (Instance != null && Instance != this) 
        { 
            Destroy(this); 
        } 
        else 
        { 
            Instance = this; 
        }
    }
    
    void Start()
    {
	    sillyKitty.SetActive(false);
        GameMusicEvent = GetComponent<FMODUnity.StudioEventEmitter>();
        if (FMODUnity.RuntimeManager.HasBankLoaded("Master Bank"))
        {
            Debug.Log("Master Bank Loaded");
        }
        GameMusicEvent.SetParameter("GameOver", 0);
        UpdateScoreText();
		// Prepare Game Over UI (hidden and scaled down)
		if (gameOverUI != null)
		{
			var t = gameOverUI.transform;
			t.localScale = Vector3.zero;
			gameOverUI.SetActive(false);
		}

		// Ensure timer/milestone UI starts hidden
		if (timerText != null)
		{
			timerText.gameObject.SetActive(false);
		}
		if (milestoneText != null)
		{
			var mt = milestoneText.transform;
			mt.localScale = Vector3.zero;
			milestoneText.gameObject.SetActive(false);
		}
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCameras();
        // Update timer each frame if running
        if (_timerRunning)
        {
            _timerSeconds += Time.deltaTime;
            if (timerText != null)
            {
                timerText.text = FormatTime(_timerSeconds);
            }
        }
    }

    public void MakeDecoy()
    {
        
    }
	public GameObject GetNextDecoy()
    {
		// Return most recently spawned non-null decoy
		for (int i = decoys.Count - 1; i >= 0; i--)
		{
			if (decoys[i] != null)
			{
				return decoys[i];
			}
			else
			{
				decoys.RemoveAt(i);
			}
		}
		return null;
    }

	public GameObject SpawnDecoy(Vector3 position)
	{
		if (decoyPrefab == null)
		{
			Debug.LogWarning("Decoy prefab not assigned on GameMaster");
			return null;
		}
		var decoy = Instantiate(decoyPrefab, position, Quaternion.identity);
		RegisterDecoy(decoy);
		return decoy;
	}

	public void RegisterDecoy(GameObject decoy)
	{
		if (decoy == null) return;
		if (decoys.Contains(decoy)) return;
		var marker = decoy.GetComponent<Decoy>();
		if (marker == null) marker = decoy.AddComponent<Decoy>();
		if (marker.hasBeenHandled) return; // do not re-register after delivery
		decoys.Add(decoy);
	}

	public void UnregisterDecoy(GameObject decoy)
	{
		if (decoy == null) return;
		decoys.Remove(decoy);
		var marker = decoy.GetComponent<Decoy>();
		if (marker != null)
		{
			marker.hasBeenHandled = true;
		}
	}

    public void OneShotAudioEvent(EventReference refEvent)
    {
        RuntimeManager.PlayOneShot(refEvent, Vector3.zero);
    }

    // Score API
    public void AddScore(int amount)
    {
        if (amount == 0) return;
        int previous = Score;
        Score += amount;
        if (Score < 0) Score = 0;
        UpdateScoreText();

        // Start timer when crossing threshold to 25
        if (!_timerRunning && previous < 25 && Score >= 25)
        {
            StartMilestoneTimer();
        }
    }

    public void SetScore(int newScore)
    {
        Score = Mathf.Max(0, newScore);
        UpdateScoreText();
    }

    public void ResetScore()
    {
        Score = 0;
        UpdateScoreText();
    }
	

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {Score}";
        }
    }

    // --- Timer logic ---
    private bool _timerRunning = false;
    private float _timerSeconds = 0f;

    public bool IsBonusRoundActive => _timerRunning;

    private void StartMilestoneTimer()
    {
        _timerRunning = true;
        _timerSeconds = 0f;
        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
            timerText.text = FormatTime(_timerSeconds);
        }
        if (milestoneText != null)
        {
            milestoneText.gameObject.SetActive(true);
            var t = milestoneText.transform;
            t.DOKill();
            t.localScale = Vector3.zero;
            t.DOScale(1f, 0.6f).SetEase(Ease.OutBack);
        }
        GameMusicEvent.SetParameter("GameState", 1);
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int mins = total / 60;
        int secs = total % 60;
        int millis = Mathf.FloorToInt((seconds - Mathf.Floor(seconds)) * 1000f);
        return $"{mins:00}:{secs:00}.{millis:000}";
    }

    void UpdateCameras()
    {
        GetClosestPointToPlayer().gameObject.GetComponentInChildren<CinemachineVirtualCamera>().MoveToTopOfPrioritySubqueue();
    }
    
    private Transform GetClosestPointToPlayer()
    {
        if (player == null)
        {
            Debug.LogWarning("Player reference is null in GameMaster");
            return null;
        }

        Vector3 playerPosition = player.transform.position;
        
        // Calculate distances to each point (check for null transforms)
        float distance1 = point1 != null ? Vector3.Distance(playerPosition, point1.position) : float.MaxValue;
        float distance2 = point2 != null ? Vector3.Distance(playerPosition, point2.position) : float.MaxValue;
        float distance3 = point3 != null ? Vector3.Distance(playerPosition, point3.position) : float.MaxValue;
        float distance4 = point4 != null ? Vector3.Distance(playerPosition, point4.position) : float.MaxValue;
        
        // Find the minimum distance
        float minDistance = Mathf.Min(distance1, distance2, distance3, distance4);
        
        // Return the transform with the minimum distance
        if (minDistance == distance1) return point1;
        if (minDistance == distance2) return point2;
        if (minDistance == distance3) return point3;
        return point4;
    }

    public void GameOver(bool caught = true)
    {
	    OneShotAudioEvent(GameOverEvent);
	    var collector = FindObjectOfType<Collector>();
	    collector.gameObject.GetComponent<Collector>().enabled = false;
	    collector.gameObject.GetComponent<NavMeshAgent>().isStopped = true;
	    collector.gameObject.GetComponent<Animator>().SetInteger("State", 3);
	    GameMusicEvent.SetParameter("GameState", 0);
	    GameMusicEvent.SetParameter("GameOver", 1); ;
	    scoreText.enabled = false;
	    gameOverScoreText.text = Score.ToString();
	    gameOverSubtext.text = caught ? "You Have Been" : "Too Bad...";
	    gameOverLabel.text = caught?"Collected":"Time's Up!";
	    milestoneText.gameObject.SetActive(false);
	    timerText.gameObject.SetActive(false);
		// Show final timer value if available
		if (gameOverTimerText != null)
		{
			gameOverTimerText.text = _timerRunning ? $"Bonus Timing: {FormatTime(_timerSeconds)}" : "";
		}
		_timerRunning = false;
		// Show Game Over UI with a grow effect
		if (gameOverUI != null)
		{
			var t = gameOverUI.transform;
			t.DOKill();
			t.localScale = Vector3.zero;
			gameOverUI.SetActive(true);
			StartCoroutine(ShowEndScreen(t));
		}
		sillyKitty.SetActive(true);
    }

    IEnumerator ShowEndScreen(Transform t)
    {
	    yield return new WaitForSeconds(2.5f);
	    t.DOScale(1f, gameOverScaleDuration).SetEase(gameOverEase);
    }

    public void PlayAgain()
    {
	    GameMusicEvent.Stop();
	    Application.LoadLevel(0);
    }
}
