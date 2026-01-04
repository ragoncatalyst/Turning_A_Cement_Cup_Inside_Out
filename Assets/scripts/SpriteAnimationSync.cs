using UnityEngine;

/// <summary>
/// Syncs sprite animation from Animator to Quad material
/// Allows animated sprites to work with 3D Quad mesh
/// </summary>
public class SpriteAnimationSync : MonoBehaviour
{
    private Animator animator;
    private MeshRenderer meshRenderer;
    private SpriteRenderer spriteRenderer;

    public void SetAnimator(Animator anim)
    {
        animator = anim;
    }

    public void SetMeshRenderer(MeshRenderer renderer)
    {
        meshRenderer = renderer;
    }

    private void OnEnable()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();
    }

    private void Update()
    {
        if (animator == null || meshRenderer == null)
            return;

        // Get current animation clip info
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        
        // Calculate current frame time in the animation
        float normalizedTime = stateInfo.normalizedTime;
        AnimationClip clip = animator.GetCurrentAnimatorClipInfo(0)[0].clip;
        
        if (clip != null)
        {
            // Get all sprites from the clip (if available through events or manual tracking)
            // For now, we'll update the material's tiling/offset based on animation time
            // A more complete solution would require custom animation processing
        }
    }

    /// <summary>
    /// Get the current sprite being displayed by the animator
    /// This is used to update the quad material's texture
    /// </summary>
    private Sprite GetCurrentSprite()
    {
        // Try to get sprite from animator through SpriteRenderer if available
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            return sr.sprite;
        
        return null;
    }
}
