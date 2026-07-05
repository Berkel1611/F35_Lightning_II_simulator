using UnityEngine;

public class EngineAudio : MonoBehaviour
{
    [SerializeField]
    Plane plane;
    [SerializeField]
    PlayerController playerController;
    [SerializeField]
    AudioSource engineAudioSource;
    [SerializeField]
    AudioClip engineStart;
    [SerializeField]
    AudioClip engineIdle;
    [SerializeField]
    AudioClip engineMilitary;
    [SerializeField]
    AudioClip engineAfterburner;

    [SerializeField]
    float minPitch = 0.5f;
    [SerializeField]
    float maxPitch = 2f;
    [SerializeField]
    float minVolume = 0.5f;
    [SerializeField]
    float maxVolume = 1f;
    [SerializeField]
    float smoothSpeed = 2f;

    float currentPitch;
    float currentVolume;

    PlayerController.EngineStartState lastEngineState = PlayerController.EngineStartState.Off;

    private void Start()
    {
        currentPitch = minPitch;
        currentVolume = 0f;
        engineAudioSource.volume = 0f;
    }

    private void Update()
    {
        if (plane == null || playerController == null) return;

        if (playerController.EngineState != lastEngineState)
        {
            switch (playerController.EngineState)
            {
                case PlayerController.EngineStartState.Starting:
                    engineAudioSource.clip = engineStart;
                    engineAudioSource.loop = false;
                    engineAudioSource.pitch = 1f;
                    engineAudioSource.volume = minVolume;
                    engineAudioSource.Play();
                    break;
                case PlayerController.EngineStartState.Running:
                    engineAudioSource.clip = engineIdle;
                    engineAudioSource.loop = true;
                    engineAudioSource.Play();
                    break;
                case PlayerController.EngineStartState.Off:
                    engineAudioSource.Stop();
                    break;
            }
            lastEngineState = playerController.EngineState;
        }

        if (playerController.EngineState != PlayerController.EngineStartState.Running) return;

        float power = plane.EnginePowerOutput / 100f;

        float targetPitch = Mathf.Lerp(minPitch, maxPitch, power);
        float targetVolume = Mathf.Lerp(minVolume, maxVolume, power);

        currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * smoothSpeed);
        currentVolume = Mathf.Lerp(currentVolume, targetVolume, Time.deltaTime * smoothSpeed);

        engineAudioSource.pitch = currentPitch;
        engineAudioSource.volume = currentVolume;

        AudioClip targetClip;

        if (plane.EnginePowerOutput > 50f)
            targetClip = engineAfterburner;
        else if (plane.EnginePowerOutput > 5f)
            targetClip = engineMilitary;
        else
            targetClip = engineIdle;

        if (engineAudioSource.clip != targetClip)
        {
            engineAudioSource.clip = targetClip;
            engineAudioSource.Play();
        }
    }
}
