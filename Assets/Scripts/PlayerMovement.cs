using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public Health health;

    [HideInInspector] public Vector3 moveDir;

    [Header("Movement Settings")]
    [SerializeField] private float gravity = 9.8f;
    [SerializeField] private float moveAcceleration = 1f;
    [SerializeField] private float rotationSpeed = 0.05f;

    private CharacterController controller;
    private Animator animator;
    private Camera cam;

    private float currentAcceleration = 1f;
    private Coroutine accelerationRoutine;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        cam = Camera.main;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        HandleMovement();
    }

    private void FixedUpdate()
    {
        if (!controller.isGrounded)
        {
            controller.Move(Vector3.down * gravity * Time.fixedDeltaTime);
        }
    }

    private void HandleMovement()
    {
        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));

        if (input.sqrMagnitude < 0.01f)
        {
            animator.SetFloat("Speed", 0f);
            moveDir = Vector3.zero;
            return;
        }

        Vector3 camForward = cam.transform.forward;
        Vector3 camRight = cam.transform.right;
        camForward.y = camRight.y = 0f;

        moveDir = camForward.normalized * input.z + camRight.normalized * input.x;

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir * currentAcceleration), rotationSpeed);

        animator.SetFloat("Speed", input.magnitude * currentAcceleration);

        controller.Move(moveDir * currentAcceleration * Time.deltaTime);
    }

    #region Enable / Disable Movement
    public void DisableMovement()
    {
        animator.SetFloat("Speed", 0f);
        if (accelerationRoutine != null) StopCoroutine(accelerationRoutine);
        currentAcceleration = 0f;
    }

    public void EnableMovement(float duration = 0.6f)
    {
        if (accelerationRoutine != null) StopCoroutine(accelerationRoutine);
        accelerationRoutine = StartCoroutine(SmoothAcceleration(currentAcceleration, moveAcceleration, duration));
    }

    private IEnumerator SmoothAcceleration(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            currentAcceleration = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        currentAcceleration = to;
    }
    #endregion
}
