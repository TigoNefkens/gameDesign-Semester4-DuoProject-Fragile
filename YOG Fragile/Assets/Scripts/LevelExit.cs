using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.VisualScripting;

public class LevelExit : MonoBehaviour
{
    public GameObject staminaObject;

    private StaminaSystem staminaSystem;

    [SerializeField] private string nextSceneName;
    [SerializeField] private Animator transitionAnimator;
    [SerializeField] private float waitTime = 1f;

    private void Awake()
    {
        staminaSystem = staminaObject.GetComponent<StaminaSystem>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && staminaSystem.activeShatterRoutine == null)
        {
            StartCoroutine(LoadLevelRoutine());
        }
    }

    IEnumerator LoadLevelRoutine()
    {
        transitionAnimator.SetTrigger("StartFade");

        yield return new WaitForSeconds(waitTime);

        SceneManager.LoadScene(nextSceneName);
    }
}