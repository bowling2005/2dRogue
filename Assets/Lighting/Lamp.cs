using UnityEngine;

public class Lamp : MonoBehaviour
{
    public string lampId;           

    public UnityEngine.Rendering.Universal.Light2D pointLight;      
    public Animator animator;       


    void Start()
    {
        if (pointLight == null)
            pointLight = GetComponentInChildren<UnityEngine.Rendering.Universal.Light2D>();
        if (animator == null)
            animator = GetComponent<Animator>();

        if (string.IsNullOrEmpty(lampId))
            lampId = gameObject.name;

        if (LampManager.Instance != null)
        {
            LampManager.Instance.RegisterLamp(this);
        }
    }

    void OnDestroy()
    {
        if (LampManager.Instance != null)
        {
            LampManager.Instance.UnregisterLamp(lampId);
        }
    }

    public void OnAnimationFrame(int frameIndex)
    {
        if (LampManager.Instance != null)
        {
            LampManager.Instance.OnLampFrameChanged(lampId, frameIndex);
        }
    }

    public float GetCurrentIntensity()
    {
        return pointLight != null ? pointLight.intensity : 0;
    }

    public void SetActive(bool active)
    {
        if (pointLight != null)
            pointLight.enabled = active;
        if (animator != null)
            animator.enabled = active;

        gameObject.SetActive(active);
    }

    public void SetIntensity(float intensity)
    {
        if (pointLight != null)
        {
            pointLight.intensity = intensity;
        }
    }
}