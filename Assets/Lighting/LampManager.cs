using UnityEngine;
using System.Collections.Generic;

public class LampManager : MonoBehaviour
{
    [Header("单例")]
    public static LampManager Instance;

    [Header("全局控制")]
    public float globalIntensityMultiplier = 1f;  
    public bool enableGlobalControl = true;

    [Header("调试")]
    public bool showDebugLogs = false;

    private Dictionary<string, Lamp> lamps = new Dictionary<string, Lamp>();

    [System.Serializable]
    public class LampConfig
    {
        public string lampId;
        public float[] frameIntensities;  // 每帧对应的亮度值
        public AnimationCurve intensityCurve; 
    }

    [Header("亮度配置")]
    public LampConfig[] lampConfigs;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        BuildConfigDictionary();
    }

    private Dictionary<string, LampConfig> configDict = new Dictionary<string, LampConfig>();

    void BuildConfigDictionary()
    {
        configDict.Clear();
        foreach (var config in lampConfigs)
        {
            if (!string.IsNullOrEmpty(config.lampId))
            {
                configDict[config.lampId] = config;
                if (showDebugLogs) 
                    Debug.Log($"[LampManager] 加载配置: {config.lampId}, 帧数: {config.frameIntensities?.Length ?? 0}");
            }
        }
    }

    public void RegisterLamp(Lamp lamp)
    {
        if (lamps.ContainsKey(lamp.lampId))
        {
            lamps[lamp.lampId] = lamp;
        }
        else
        {
            lamps.Add(lamp.lampId, lamp);
        }

        ApplyInitialIntensity(lamp);
    }

    public void UnregisterLamp(string lampId)
    {
        if (lamps.ContainsKey(lampId))
        {
            lamps.Remove(lampId);
        }
    }

    public void OnLampFrameChanged(string lampId, int frameIndex)
    {
        if (!lamps.ContainsKey(lampId))
        {
            Debug.LogWarning($"[LampManager] 未找到灯具: {lampId}");
            return;
        }

        Lamp lamp = lamps[lampId];
        float targetIntensity = GetIntensityForFrame(lampId, frameIndex);

        if (enableGlobalControl)
        {
            targetIntensity *= globalIntensityMultiplier;
        }

        lamp.SetIntensity(targetIntensity);
    }

    private float GetIntensityForFrame(string lampId, int frameIndex)
    {
        if (configDict.TryGetValue(lampId, out LampConfig config))
        {
            if (config.intensityCurve != null && config.intensityCurve.keys.Length > 0)
            {
                float t = (float)frameIndex / (config.frameIntensities?.Length ?? 5);
                return config.intensityCurve.Evaluate(t);
            }
            if (config.frameIntensities != null && frameIndex < config.frameIntensities.Length)
            {
                return config.frameIntensities[frameIndex];
            }
        }
        //默认呼吸灯
        float defaultIntensity = 0.8f + Mathf.Sin(frameIndex * Mathf.PI / 2) * 0.2f;
        return Mathf.Clamp(defaultIntensity, 0.5f, 1.3f);
    }

    private void ApplyInitialIntensity(Lamp lamp)
    {
        float initialIntensity = GetIntensityForFrame(lamp.lampId, 0);
        if (enableGlobalControl)
            initialIntensity *= globalIntensityMultiplier;
        lamp.SetIntensity(initialIntensity);
    }

    public Lamp GetLamp(string lampId)
    {
        return lamps.ContainsKey(lampId) ? lamps[lampId] : null;
    }

    public void ToggleLamp(string lampId, bool active)
    {
        if (lamps.TryGetValue(lampId, out Lamp lamp))
        {
            lamp.SetActive(active);
        }
    }
}