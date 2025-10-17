using Unity.VisualScripting;
using UnityEngine;

public class MovementScript : MonoBehaviour
{
    public float speed;
    public float gravity = 20.0f;
    public float jumpForce = 8f;
    public float rotationSpeed;
    
    public Animator animator;
    
    Vector3 movementDirection = Vector3.zero;
    Vector3 inputDirection = Vector3.zero;
    bool jumpInput = false;

	public bool controlsEnabled = true;

    float jumpSpeed = 0f;
    CharacterController characterController;
    
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int Airtime = Animator.StringToHash("Airtime");

    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
		if (controlsEnabled)
		{
			float horizontalInput = Input.GetAxis("Horizontal");
			float verticalInput = Input.GetAxis("Vertical");
			inputDirection = new Vector3(horizontalInput, 0, verticalInput);
			jumpInput = Input.GetButtonDown("Jump");
		}
		else
		{
			inputDirection = Vector3.zero;
			jumpInput = false;
		}
        if (jumpInput)
        {
            jumpSpeed = jumpForce;
            CharacterMovement();
        }
        
        else
        {
            jumpSpeed = 0.0f;
        }
    }
    public void EnableControls()
    {
        controlsEnabled = true;
        if (characterController != null)
        {
            characterController.enabled = true;
        }
    }

    public void DisableControls()
    {
        controlsEnabled = false;
        movementDirection = Vector3.zero;
        if (characterController != null)
        {
            characterController.enabled = false;
        }
    }
    void FixedUpdate()
    {
        CharacterMovement();
    }

    void CharacterMovement()
    {
        if (characterController == null || !characterController.enabled)
        {
            return;
        }
        if (characterController.isGrounded)
        {
			// Compute camera-relative planar movement (XZ only)
			Vector3 desiredPlanarMove;
			Camera mainCam = Camera.main;
			if (mainCam != null)
			{
				Vector3 camForward = mainCam.transform.forward;
				camForward.y = 0f;
				camForward.Normalize();
				Vector3 camRight = mainCam.transform.right;
				camRight.y = 0f;
				camRight.Normalize();
				desiredPlanarMove = camForward * inputDirection.z + camRight * inputDirection.x;
			}
			else
			{
				// Fallback to world-relative if no camera found
				desiredPlanarMove = new Vector3(inputDirection.x, 0f, inputDirection.z);
			}

			if (desiredPlanarMove.sqrMagnitude > 1f)
			{
				desiredPlanarMove.Normalize();
			}

			Vector3 planarVelocity = desiredPlanarMove * speed;
			movementDirection.x = planarVelocity.x;
			movementDirection.z = planarVelocity.z;

			animator.SetFloat(Speed, planarVelocity.magnitude);
			GameMaster.Instance.GameMusicEvent.SetParameter("CatMoveSpeed", planarVelocity.magnitude);
			movementDirection.y = jumpSpeed;
        }
		if (inputDirection != Vector3.zero)
        {
			// Face the planar movement direction only
			Vector3 lookDirection = new Vector3(movementDirection.x, 0f, movementDirection.z);
			if (lookDirection != Vector3.zero)
			{
				Quaternion toRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
				transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);
			}
        }
        movementDirection.y -= gravity * Time.deltaTime;
        characterController.Move(movementDirection * Time.deltaTime);
        animator.SetBool(Airtime, !characterController.isGrounded);
    }
}