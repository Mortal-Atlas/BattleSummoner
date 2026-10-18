using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class IdleAnimationPlaylist : MonoBehaviour
{
    public List<AnimationClip> animations = new List<AnimationClip>();
    public float playDuration = 2.5f;

    private Animator animator;
    private AnimatorOverrideController overrideController;
    private int index;
    private float timer;

    void Awake()
    {
        animator = GetComponent<Animator>();

        overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
        animator.runtimeAnimatorController = overrideController;
    }

    void OnEnable()
    {
        index = 0;
        timer = 0f;
        PlayCurrent();
    }

    void Update()
    {
        if (animations == null || animations.Count == 0)
            return;

        timer += Time.deltaTime;

        if (timer >= playDuration)
        {
            timer = 0f;
            index = (index + 1) % animations.Count;
            PlayCurrent();
        }
    }

    void PlayCurrent()
    {
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);

        overrides[0] = new KeyValuePair<AnimationClip, AnimationClip>(
            overrides[0].Key,
            animations[index]
        );

        overrideController.ApplyOverrides(overrides);

        animator.Rebind();
        animator.Update(0f);
    }
}
