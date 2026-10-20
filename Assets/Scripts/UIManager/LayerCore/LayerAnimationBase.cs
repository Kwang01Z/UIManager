using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public abstract class LayerAnimationBase : MonoBehaviour
{
    public abstract UniTask PlayShowAnimationAsync(CancellationToken cancellationToken = default);
    public abstract UniTask PlayHideAnimationAsync(CancellationToken cancellationToken = default);
}
