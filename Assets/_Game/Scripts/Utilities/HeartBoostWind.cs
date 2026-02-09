using UnityEngine;

public class HeartBoostWind : MonoBehaviour
{
    [SerializeField] ParticleSystem windPS;

    void Awake()
    {
        if (!windPS)
            windPS = GetComponentInChildren<ParticleSystem>(true);

        ForceStop();
    }

    public void SetBoosting(bool boosting)
    {
        if (!windPS) return;

        if (boosting) Play();
        else ForceStop();
    }

    void Play()
    {

        windPS.Clear(true);
        windPS.Play(true);
    }

    void ForceStop()
    {

        windPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        windPS.Clear(true);
    }
}
