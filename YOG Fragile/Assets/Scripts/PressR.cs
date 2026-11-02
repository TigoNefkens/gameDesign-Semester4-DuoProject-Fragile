using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PressR : MonoBehaviour
{
    private bool canPressR = false;

    private void Update()
    {
        if (canPressR && Keyboard.current.rKey.isPressed)
        {
            SceneManager.LoadScene("Opening");
        }
    }

    public void CanPressR()
    {
        canPressR = true;
    }
}
