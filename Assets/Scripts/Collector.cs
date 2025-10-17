using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using FMODUnity;

public class Collector : MonoBehaviour
{
    private NavMeshAgent agent;
    
    public Animator animator;
    
    [Header("Nav")]
    public Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private float idleTimer = 0f;
    public float idleDuration = 2f;
    
    [Header("Player Vis")]
    public float detectionRange = 10f;
    public float detectionAngle = 60f;
    public LayerMask playerLayer = 1; // Layer mask for player
    public LayerMask obstacleLayer = 1; // Layer mask for obstacles
    
    [Header("Decoy Logic")]
    public Transform decoyDropPoint; // Where to drop collected decoys
    private GameObject currentDecoy = null;
    public Transform decoyHoldSlot;
    private GameObject carriedPlayer = null;
    private float playerPickupBlockUntil = 0f;
    private bool deliveryLock = false;
    
    private bool detectPlayerAlerted = false;
    private bool isCollectingDecoy = false;
    
    
    public enum CollectorState
    {
        Patrolling,
        Idling,
        ChasingPlayer,
        CollectingDecoy,
        DeliveringDecoy
    }
    
	private CollectorState currentState = CollectorState.Patrolling;
	private CollectorState previousState = CollectorState.Patrolling;

	void SetState(CollectorState nextState)
	{
    if (deliveryLock && nextState != CollectorState.DeliveringDecoy) return;
    if (currentState == nextState) return;
		previousState = currentState;
		currentState = nextState;
		PlayStateChangeSfx(nextState, previousState);
	}

	void PlayStateChangeSfx(CollectorState state, CollectorState prevState)
	{
		switch (state)
		{
			case CollectorState.Patrolling:
				// Only play patrol sfx (LostEvent) when returning to patrol from chase
				if (prevState == CollectorState.ChasingPlayer)
				{
					GameMaster.Instance.OneShotAudioEvent(GameMaster.Instance.LostEvent);
				}
				break;
			case CollectorState.Idling:
				//if (!EventReference.IsNull(idleSfx)) GameMaster.Instance.OneShotAudioEvent(idleSfx);
				break;
			case CollectorState.ChasingPlayer:
				GameMaster.Instance.OneShotAudioEvent(GameMaster.Instance.AlertEvent);
				break;
			case CollectorState.CollectingDecoy:
				GameMaster.Instance.OneShotAudioEvent(GameMaster.Instance.DistractEvent);
				break;
			case CollectorState.DeliveringDecoy:
				//if (!EventReference.IsNull(deliverSfx)) GameMaster.Instance.OneShotAudioEvent(deliverSfx);
				break;
		}
	}
    
    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        
        // Start patrolling if we have waypoints
        if (waypoints.Length > 0)
        {
            MoveToWaypoint();
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateStateMachine();
        SetAudio();
    }

    void SetAudio()
    {
        var dist = Vector3.Distance(GameMaster.Instance.player.transform.position, transform.position);
        GameMaster.Instance.GameMusicEvent.SetParameter("CollectorFade", dist/32);
    }
    
    void UpdateStateMachine()
    {
        // In bonus round, always chase player regardless of decoys or detection
        if (GameMaster.Instance != null && GameMaster.Instance.IsBonusRoundActive)
        {
            ForceChasePlayer();
            return;
        }
        switch (currentState)
        {
            case CollectorState.Patrolling:
                HandlePatrolling();
                GameMaster.Instance.GameMusicEvent.SetParameter("GameState", 0);
                break;
            case CollectorState.Idling:
                HandleIdling();
                GameMaster.Instance.GameMusicEvent.SetParameter("GameState", 0);
                break;
            case CollectorState.ChasingPlayer:
                HandleChasingPlayer();
                GameMaster.Instance.GameMusicEvent.SetParameter("GameState", 1);
                break;
            case CollectorState.CollectingDecoy:
                HandleCollectingDecoy();
                GameMaster.Instance.GameMusicEvent.SetParameter("GameState", 2);
                break;
            case CollectorState.DeliveringDecoy:
                HandleDeliveringDecoy();
                GameMaster.Instance.GameMusicEvent.SetParameter("GameState", 2);
                break;
            default:
                break;
        }
    }

    void ForceChasePlayer()
    {
        if (GameMaster.Instance == null || GameMaster.Instance.player == null) return;
        if (animator.GetInteger("State") != 1)
        {
            animator.SetInteger("State", 1);
        }
        SetState(CollectorState.ChasingPlayer);
        agent.destination = GameMaster.Instance.player.transform.position;

        // Attempt pickup when close
        if (Time.time >= playerPickupBlockUntil && Vector3.Distance(transform.position, GameMaster.Instance.player.transform.position) < 1.0f)
        {
            PickupPlayer(GameMaster.Instance.player);
            deliveryLock = true;
            GameMaster.Instance.GameOver();
        }
    }
    
    void HandlePatrolling()
    {
        if (animator.GetInteger("State") != 1)
        {
            animator.SetInteger("State", 1);
        }
        // Check for decoys first (highest priority)
		if (GameMaster.Instance.GetNextDecoy() != null)
		{
			SetState(CollectorState.CollectingDecoy);
			return;
		}
        
        // Check for player detection
		if (DetectPlayer())
		{
			SetState(CollectorState.ChasingPlayer);
			return;
		}
        detectPlayerAlerted = false;
        
        // Continue patrolling
		if (!agent.pathPending && agent.remainingDistance < 0.5f)
		{
			SetState(CollectorState.Idling);
			idleTimer = 0f;
		}
    }
    
    void HandleIdling()
    {
        if (animator.GetInteger("State") != 0)
        {
            animator.SetInteger("State", 0);
        }
        // Check for decoys first (highest priority)
		if (GameMaster.Instance.GetNextDecoy() != null)
		{
			SetState(CollectorState.CollectingDecoy);
			return;
		}
        
        // Check for player detection
		if (DetectPlayer())
		{
			SetState(CollectorState.ChasingPlayer);
			return;
		}
        
        // Idle for specified duration
        idleTimer += Time.deltaTime;
		if (idleTimer >= idleDuration)
		{
			MoveToNextWaypoint();
			SetState(CollectorState.Patrolling);
		}
    }
    
    void HandleChasingPlayer()
    {
        // Check for decoys first (highest priority)
		if (GameMaster.Instance.GetNextDecoy() != null)
		{
			SetState(CollectorState.CollectingDecoy);
			return;
		}
        
        // Continue chasing if player is still detected
        if (DetectPlayer())
        {
            if (animator.GetInteger("State") != 1)
            {
                animator.SetInteger("State", 1);
            }
            agent.destination = GameMaster.Instance.player.transform.position;

			// Attempt to pick up player if close enough and not blocked
            if (Time.time >= playerPickupBlockUntil && Vector3.Distance(transform.position, GameMaster.Instance.player.transform.position) < 1.0f && carriedPlayer == null)
			{
				PickupPlayer(GameMaster.Instance.player);
				// After pickup, switch to delivering state using same pipeline as decoy
                deliveryLock = true;
				GameMaster.Instance.GameOver();
				return;
			}
        }
        else
        {
            if (animator.GetInteger("State") != 2)
            {
                animator.SetInteger("State", 2);
            }
            // Player lost, return to patrolling
			idleTimer += Time.deltaTime;
			if (idleTimer >= idleDuration)
			{
				MoveToNextWaypoint();
				SetState(CollectorState.Patrolling);
			}
        }
    }
    
    void HandleCollectingDecoy()
    {
		GameObject decoy = GameMaster.Instance.GetNextDecoy();
		if (decoy == null)
		{
			SetState(CollectorState.Patrolling);
			return;
		}
        
		currentDecoy = decoy;
		agent.destination = decoy.transform.position;
        
        // Check if we've reached the decoy
		if (!agent.pathPending && agent.remainingDistance < 1f)
		{
            // Collect the decoy: parent, disable physics, unregister from GM
			decoy.transform.SetParent(decoyHoldSlot);
			decoy.transform.localPosition = Vector3.zero;
			var decoyComp = decoy.GetComponent<Decoy>();
			if (decoyComp != null)
			{
				decoyComp.DisablePhysics();
			}
			GameMaster.Instance.UnregisterDecoy(decoy);
			isCollectingDecoy = true;
            deliveryLock = true;
			SetState(CollectorState.DeliveringDecoy);
		}
    }
    
    void HandleDeliveringDecoy()
    {
        if (animator.GetInteger("State") != 3)
        {
            animator.SetInteger("State", 3);
        }
		if (currentDecoy == null)
		{
			SetState(CollectorState.Patrolling);
			return;
		}
        
		// Move to drop point
        if (decoyDropPoint != null)
        {
            agent.destination = decoyDropPoint.position;
            
            // Check if we've reached the drop point
            if (!agent.pathPending && agent.remainingDistance < 1f)
            {
                // Drop the decoy or player
                if (currentDecoy != null)
                {
                    currentDecoy.transform.SetParent(null);
                    var decoyComp = currentDecoy.GetComponent<Decoy>();
                    if (decoyComp != null)
                    {
                        decoyComp.EnablePhysics();
                    }
                    currentDecoy = null;
                    isCollectingDecoy = false;
                }
                if (carriedPlayer != null)
                {
                    carriedPlayer.transform.SetParent(null);
                    var move = carriedPlayer.GetComponent<MovementScript>();
                    if (move != null)
                    {
                        move.EnableControls();
                    }
                    carriedPlayer = null;
                    // Prevent immediate re-pickup for 3 seconds
                    playerPickupBlockUntil = Time.time + 3f;
                }
                // Allow state transitions now that delivery completed
                deliveryLock = false;
                
                // Check for next decoy
				if (GameMaster.Instance.GetNextDecoy() != null)
				{
					SetState(CollectorState.CollectingDecoy);
				}
				else
				{
					SetState(CollectorState.Patrolling);
				}
            }
        }
        else
        {
            // No drop point defined, just unparent the decoy
            if (currentDecoy != null)
            {
                currentDecoy.transform.SetParent(null);
                currentDecoy = null;
                isCollectingDecoy = false;
            }
            if (carriedPlayer != null)
            {
                carriedPlayer.transform.SetParent(null);
                var move = carriedPlayer.GetComponent<MovementScript>();
                if (move != null)
                {
                    move.EnableControls();
                }
                carriedPlayer = null;
                playerPickupBlockUntil = Time.time + 3f;
            }
            deliveryLock = false;
			SetState(CollectorState.Patrolling);
        }
    }

	private void PickupPlayer(GameObject player)
	{
		if (player == null) return;
		carriedPlayer = player;
		player.transform.SetParent(decoyHoldSlot);
		player.transform.localPosition = Vector3.zero;
		var move = player.GetComponent<MovementScript>();
		if (move != null)
		{
			move.DisableControls();
		}
	}
    
    bool DetectPlayer()
    {
        if (GameMaster.Instance.player == null) return false;
        
        Vector3 directionToPlayer = GameMaster.Instance.player.transform.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;
        
        // Check if player is within detection range
        if (distanceToPlayer > detectionRange) return false;
        
        // Check if player is within detection angle
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer.normalized);
        if (angleToPlayer > detectionAngle) return false;
        
        // Perform 3 raycasts in a cone pattern
        Vector3[] raycastDirections = new Vector3[3];
        raycastDirections[0] = directionToPlayer.normalized; // Center ray
        raycastDirections[1] = Quaternion.AngleAxis(-detectionAngle/2, Vector3.up) * transform.forward; // Left ray
        raycastDirections[2] = Quaternion.AngleAxis(detectionAngle/2, Vector3.up) * transform.forward; // Right ray
        
        foreach (Vector3 direction in raycastDirections)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, direction, out hit, detectionRange))
            {
                // Check if we hit the player
                if (hit.collider.gameObject == GameMaster.Instance.player)
                {
                    return true;
                }
                // Check if we hit an obstacle (wall, etc.)
                if (((1 << hit.collider.gameObject.layer) & obstacleLayer) != 0)
                {
                    continue; // This ray is blocked, try the next one
                }
            }
        }
        return false;
    }
    
    void MoveToWaypoint()
    {
        if (waypoints.Length > 0 && waypoints[currentWaypointIndex] != null)
        {
            agent.destination = waypoints[currentWaypointIndex].position;
        }
    }
    
    void MoveToNextWaypoint()
    {
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        MoveToWaypoint();
    }
}
