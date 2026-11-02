using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class WinTrigger : MonoBehaviour
{
    public PlayableDirector OutroTimeline;

    public GameObject StaminaBar;

    [SerializeField] private Animator winAnimator;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlatformerController playerMovement = collision.GetComponent<PlatformerController>();

            if (playerMovement != null)
            {
                playerMovement.SetMoveInput(Vector2.zero);
                playerMovement.rb2d.linearVelocity = Vector2.zero;

                playerMovement.enabled = false;
            }

            if (StaminaBar != null)
            {
                StaminaBar.SetActive(false);
            }

            if (OutroTimeline != null)
            {
                OutroTimeline.Play();
            }

            winAnimator.SetTrigger("PlayWinAnim");
        }
    }
}