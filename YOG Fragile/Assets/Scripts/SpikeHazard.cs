using UnityEngine;

public class SpikeHazard : MonoBehaviour
{ 
    private void OnTriggerEnter2D(Collider2D other)
    {
        StaminaSystem stamina = other.GetComponent<StaminaSystem>();
        if (stamina != null)
        {
            stamina.DepleteAllStamina();
        }
    }
}
