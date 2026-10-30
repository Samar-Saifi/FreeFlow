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
    private float verticalVelocity = -2f;

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

    private void HandleMovement()
    {
        if (cam == null) cam = Camera.main;

        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));

        if (input.sqrMagnitude < 0.01f)
        {
            animator.SetFloat("Speed", 0f);
            moveDir = Vector3.zero;
        }
        else
        {
            Vector3 camForward = cam != null ? cam.transform.forward : Vector3.forward;
            Vector3 camRight = cam != null ? cam.transform.right : Vector3.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            moveDir = camForward * input.z + camRight * input.x;

            if (moveDir.sqrMagnitude > 0.001f)
            {
                float factor = 1f - Mathf.Pow(1f - Mathf.Clamp01(rotationSpeed), Time.deltaTime * 60f);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), factor);
            }

            animator.SetFloat("Speed", input.magnitude * currentAcceleration);
        }

        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        Vector3 motion = (moveDir * currentAcceleration) + (Vector3.up * verticalVelocity);
        controller.Move(motion * Time.deltaTime);
    }

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
}
