using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    [Header("UI组件")]
    public CanvasGroup loadingCanvasGroup;
    public Animator loadingAnimator;

    [Header("设置")]
    public float fadeDuration = 0.3f;
    public float minLoadingTime = 1.5f;
    public float fastForwardSpeed = 3.0f;

    private AsyncOperation asyncLoad;
    private bool isTransitioning = false;

    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Setup();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Setup()
    {
        // 确保CanvasGroup存在
        if (loadingCanvasGroup == null)
        {
            loadingCanvasGroup = GetComponent<CanvasGroup>();
            if (loadingCanvasGroup == null)
            {
                loadingCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // 初始隐藏
        loadingCanvasGroup.alpha = 0;
        loadingCanvasGroup.interactable = false;
        loadingCanvasGroup.blocksRaycasts = false;

        // 检查Animator
        if (loadingAnimator == null)
        {
            Debug.LogError("请将LoadingAnimation的Animator拖到SceneLoader脚本中！");
        }
    }

    /// <summary>
    /// 加载场景
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (isTransitioning) return;

        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    public void LoadScene(int sceneBuildIndex)
    {
        LoadScene(GetSceneNameByIndex(sceneBuildIndex));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isTransitioning = true;

        Debug.Log($"开始加载场景: {sceneName}");

        // 1. 淡入加载界面
        yield return StartCoroutine(FadeLoadingScreen(true));

        // 2. 开始动画
        if (loadingAnimator != null)
        {
            loadingAnimator.SetBool("Loading", true);
            loadingAnimator.speed = 1.0f;
            Debug.Log("开始播放加载动画");
        }

        // 3. 开始异步加载场景
        asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        float startTime = Time.time;
        float animationProgress = 0f;
        float loadingProgress = 0f;

        // 4. 同时等待动画和加载完成
        while (true)
        {
            // 更新加载进度
            loadingProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            // 检查动画进度
            if (loadingAnimator != null)
            {
                AnimatorStateInfo stateInfo = loadingAnimator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName("Loading"))
                {
                    animationProgress = stateInfo.normalizedTime;

                    // 如果加载完成但动画未完成，加速动画
                    if (loadingProgress >= 0.999f && animationProgress < 0.999f)
                    {
                        loadingAnimator.speed = fastForwardSpeed;
                    }
                }
            }

            // 检查是否满足条件
            bool animationComplete = (loadingAnimator == null) || animationProgress >= 0.999f;
            bool loadingComplete = loadingProgress >= 0.999f;
            bool timeComplete = Time.time - startTime >= minLoadingTime;

            if (animationComplete && loadingComplete && timeComplete)
            {
                Debug.Log("加载条件满足，准备切换场景");
                break;
            }

            // 输出调试信息
            Debug.Log($"进度 - 动画: {animationProgress:P0}, 加载: {loadingProgress:P0}, 时间: {Time.time - startTime:F1}s");

            yield return null;
        }

        // 5. 停止动画
        if (loadingAnimator != null)
        {
            loadingAnimator.SetBool("Loading", false);
            loadingAnimator.speed = 1.0f;
            Debug.Log("停止加载动画");
        }

        // 6. 淡出加载界面
        yield return StartCoroutine(FadeLoadingScreen(false));

        // 7. 激活场景
        asyncLoad.allowSceneActivation = true;
        Debug.Log($"场景激活: {sceneName}");

        // 8. 重置状态
        isTransitioning = false;
    }

    private IEnumerator FadeLoadingScreen(bool show)
    {
        float startAlpha = loadingCanvasGroup.alpha;
        float targetAlpha = show ? 1 : 0;

        loadingCanvasGroup.interactable = show;
        loadingCanvasGroup.blocksRaycasts = show;

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            loadingCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        loadingCanvasGroup.alpha = targetAlpha;

        if (!show)
        {
            // 完全隐藏后停用相关对象（可选）
            // loadingCanvasGroup.gameObject.SetActive(false);
        }
    }

    private string GetSceneNameByIndex(int index)
    {
        if (index < 0 || index >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"场景索引无效: {index}");
            return null;
        }

        string path = SceneUtility.GetScenePathByBuildIndex(index);
        return System.IO.Path.GetFileNameWithoutExtension(path);
    }

    // 调试用：强制完成
    [ContextMenu("测试加载场景")]
    public void TestLoadScene()
    {
        LoadScene(1); // 加载Build Settings中的第二个场景
    }
}