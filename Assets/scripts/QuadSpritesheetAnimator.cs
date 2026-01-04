using UnityEngine;

/// <summary>
/// QuadSpritesheetAnimator: 控制 Quad 的 Spritesheet 动画播放
/// 通过更新 Material 的 _CurrentFrame 来实现帧动画
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class QuadSpritesheetAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private int frameCountX = 4;
    [SerializeField] private int frameCountY = 4;
    [SerializeField] private float frameRate = 10f;  // frames per second
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loop = true;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    private MeshRenderer meshRenderer;
    private Material animatedMaterial;
    private int currentFrame = 0;
    private float frameTimer = 0f;
    private bool isPlaying = false;
    private int totalFrames;
    
    // Shader property ID
    private int currentFramePropID;
    
    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            Debug.LogError($"[QuadSpritesheetAnimator] {name} has no MeshRenderer!");
            enabled = false;
            return;
        }
        
        // 创建材质实例
        animatedMaterial = new Material(meshRenderer.material);
        meshRenderer.material = animatedMaterial;
        
        currentFramePropID = Shader.PropertyToID("_CurrentFrame");
        totalFrames = frameCountX * frameCountY;
        
        // 初始化帧
        animatedMaterial.SetFloat(currentFramePropID, 0f);
        
        if (enableDebugLogs)
            Debug.Log($"[QuadSpritesheetAnimator] {name} initialized: {frameCountX}x{frameCountY} = {totalFrames} frames");
    }
    
    private void Start()
    {
        if (playOnStart)
            Play();
    }
    
    private void Update()
    {
        if (!isPlaying) return;
        
        // 更新帧计时器
        frameTimer += Time.deltaTime * frameRate;
        
        // 检查是否需要进入下一帧
        if (frameTimer >= 1f)
        {
            frameTimer -= 1f;
            currentFrame++;
            
            // 处理循环
            if (currentFrame >= totalFrames)
            {
                if (loop)
                {
                    currentFrame = 0;
                }
                else
                {
                    currentFrame = totalFrames - 1;
                    Stop();
                }
            }
            
            // 更新 Shader
            animatedMaterial.SetFloat(currentFramePropID, (float)currentFrame);
            
            if (enableDebugLogs)
                Debug.Log($"[QuadSpritesheetAnimator] {name} frame: {currentFrame}/{totalFrames}");
        }
    }
    
    /// <summary>
    /// 开始播放动画
    /// </summary>
    public void Play()
    {
        isPlaying = true;
        currentFrame = 0;
        frameTimer = 0f;
        animatedMaterial.SetFloat(currentFramePropID, 0f);
    }
    
    /// <summary>
    /// 停止动画
    /// </summary>
    public void Stop()
    {
        isPlaying = false;
    }
    
    /// <summary>
    /// 恢复动画
    /// </summary>
    public void Resume()
    {
        isPlaying = true;
    }
    
    /// <summary>
    /// 跳转到指定帧
    /// </summary>
    public void SetFrame(int frameIndex)
    {
        currentFrame = Mathf.Clamp(frameIndex, 0, totalFrames - 1);
        frameTimer = 0f;
        animatedMaterial.SetFloat(currentFramePropID, (float)currentFrame);
    }
    
    /// <summary>
    /// 设置播放速度
    /// </summary>
    public void SetFrameRate(float newFrameRate)
    {
        frameRate = Mathf.Max(0.1f, newFrameRate);
    }
    
    public int GetCurrentFrame() => currentFrame;
    public int GetTotalFrames() => totalFrames;
    public bool IsPlaying() => isPlaying;
    
    private void OnDestroy()
    {
        if (animatedMaterial != null)
            Destroy(animatedMaterial);
    }
}
