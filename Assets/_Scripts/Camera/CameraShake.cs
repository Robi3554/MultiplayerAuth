using Unity.Cinemachine;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    private CinemachineCamera cinemachineCamera;
    private float shakeTimer;
    private float shakeTimerTotal;
    private float startingIntensity;


    private void Awake()
    {
        Instance = this;
        cinemachineCamera = GetComponent<CinemachineCamera>();
    }

    private void Update()
    {
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            CinemachineBasicMultiChannelPerlin perlin = cinemachineCamera.GetCinemachineComponent(CinemachineCore.Stage.Noise) as CinemachineBasicMultiChannelPerlin;
                
            perlin.AmplitudeGain = Mathf.Lerp(startingIntensity, 0f, 1 - (shakeTimer / shakeTimerTotal));
        }
    }

    public void ShakeCamera(float intesity, float time)
    {
        CinemachineBasicMultiChannelPerlin perlin = cinemachineCamera.GetCinemachineComponent(CinemachineCore.Stage.Noise) as CinemachineBasicMultiChannelPerlin;

        perlin.AmplitudeGain = intesity;

        startingIntensity = intesity;
        shakeTimer = time;
    }
}
