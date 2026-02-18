using System;
using UnityEngine;

public class TimedNpcDespawn : MonoBehaviour
{
    private float lifetime;
    private Action onExpired;
    private float startedAt;

    public void Init(float lifetimeSeconds, Action onExpiredCallback)
    {
        lifetime = lifetimeSeconds;
        onExpired = onExpiredCallback;
        startedAt = Time.time;
    }

    private void Update()
    {
        if (Time.time - startedAt >= lifetime)
            onExpired?.Invoke();
    }
}