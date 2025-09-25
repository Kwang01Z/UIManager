using UnityEngine;
using LitMotion;
using LitMotion.Extensions;

public class WarmUpManager : MonoBehaviour
{
    [Header("WarmUp Settings")]
    public bool warmUpScale = true;
    public bool warmUpAlpha = true;
    public bool warmUpPosition = true;
    public bool warmUpRotation = true;
    public bool warmUpColor = true;
    public bool warmUpFloat = true;

    private void Awake()
    {
        RunWarmUp();
    }

    private void RunWarmUp()
    {
        var go = new GameObject("LM_WarmUp_Scale");
        if (warmUpScale)
        {
            var t = go.transform;
            t.localScale = Vector3.zero;

            LMotion.Create(Vector3.zero, Vector3.one, 0.01f)
                .WithOnComplete(() => Destroy(go))
                .BindToLocalScale(t);
        }

        if (warmUpAlpha)
        {
            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            LMotion.Create(0f, 1f, 0.01f)
                .BindToAlpha(cg);
        }

        if (warmUpPosition)
        {
            var t = go.transform;

            LMotion.Create(Vector3.zero, Vector3.one, 0.01f)
                .BindToLocalPosition(t);
        }

        if (warmUpRotation)
        {
            var t = go.transform;

            LMotion.Create(Quaternion.identity, Quaternion.Euler(0, 90, 0), 0.01f)
                .BindToLocalRotation(t);
        }

        if (warmUpColor)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = Color.black;

            LMotion.Create(Color.black, Color.white, 0.01f)
                .BindToColor(sr);
        }

        if (warmUpFloat)
        {
            // Float warm-up (dùng cho giá trị bất kỳ, VD: volume, fillAmount…)
            LMotion.Create(0f, 1f, 0.01f);
        }
    }
}
