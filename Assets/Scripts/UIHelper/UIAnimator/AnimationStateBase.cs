using LitMotion;
using UnityEngine;

public abstract class AnimationStateBase : MonoBehaviour
{
    [SerializeField] protected float duration = 0.7f;
    [SerializeField] protected Ease ease = Ease.OutBack;
    public abstract void PlayState();
    public abstract void PlayStateSmooth();
    public abstract void SetupState(int stateIndex);
    public abstract void SetupStateSmooth(int stateIndex);
}
