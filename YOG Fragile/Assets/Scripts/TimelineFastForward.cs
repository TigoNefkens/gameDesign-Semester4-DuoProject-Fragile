using UnityEngine;
using UnityEngine.Playables;
// Add this line to access the new Input System
using UnityEngine.InputSystem;

public class TimelineFastForward : MonoBehaviour
{
    private PlayableDirector director;
    [SerializeField] private float fastForwardSpeed = 5f;
    [SerializeField] private float normalSpeed = 1f;

    void Start()
    {
        director = GetComponent<PlayableDirector>();
    }

    void Update()
    {
        if (director == null || !director.playableGraph.IsValid())
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.cKey.isPressed)
        {
            director.playableGraph.GetRootPlayable(0).SetSpeed(fastForwardSpeed);
        }
        else
        {
            director.playableGraph.GetRootPlayable(0).SetSpeed(normalSpeed);
        }
    }
}