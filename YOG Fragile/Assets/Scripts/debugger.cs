using UnityEngine;

public class ParticleDebugger : MonoBehaviour
{
    private void OnDisable()
    {
        Debug.LogWarning("PARTICLES DISABLED at " + Time.unscaledTime);
    }

    private void OnEnable()
    {
        Debug.Log("PARTICLES ENABLED at " + Time.unscaledTime);
    }
}