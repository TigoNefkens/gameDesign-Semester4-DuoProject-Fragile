using UnityEngine;

public class CecilAnimator : MonoBehaviour
{
    private Animator animator;
    private PlatformerController controller;

    void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<PlatformerController>();
    }

    void Update()
    {
        animator.SetBool("isGrounded", controller.isGrounded);
        animator.SetBool("isMoving", Mathf.Abs(controller.moveInput.x) > 0.1f);
        animator.SetBool("isSticking", controller.isWallSticking);
        animator.SetBool("isCeiling", controller.isCeilingSticking);
        animator.SetBool("isMantling", controller.isMantling);
    }
}