using System;
using UnityEngine;

namespace ObjectPing;

public class PingDestruction : MonoBehaviour
{
    public float m_timeout = 1f;
    public bool m_triggerOnAwake;
    public ZNetView m_nview = null!;

    public void Awake()
    {
        m_nview = GetComponent<ZNetView>();
        if (!m_triggerOnAwake)
            return;
        Trigger();
    }

    public void Trigger() => InvokeRepeating(nameof(DestroyNow), m_timeout, 1f);

    public void Trigger(float timeout) => InvokeRepeating(nameof(DestroyNow), timeout, 1f);

    public void OnDestroy()
    {
        Cancel();
        TryDestroyGameObject();
    }

    public void DestroyNow()
    {
        TryDestroyGameObject();
    }

    public void Cancel() => CancelInvoke(nameof(DestroyNow));

    public void OnDisable() => Cancel();

    private void OnPreCull()
    {
        TryDestroyGameObject();
    }

    private void TryDestroyGameObject()
    {
        try
        {
            if (m_nview)
            {
                if (!m_nview.IsValid() || !m_nview.IsOwner())
                    return;
                ZNetScene.instance.Destroy(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        catch (Exception ex)
        {
            ObjectPingPlugin.ObjectPingLogger.LogError($"Failed to destroy game object: {ex.Message} {gameObject.name}");
        }
    }
}