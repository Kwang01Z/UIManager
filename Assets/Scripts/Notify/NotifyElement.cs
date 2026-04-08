using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class NotifyElement : MonoBehaviour
{
    [SerializeField] private NotifyKeyName keyName;
    [SerializeField] private Image notifyImge;
    public UnityEvent OnNotifyStateActive;
    public UnityEvent OnNotifyStateInActive;
    private NotifyScaleAnim  notifyScaleAnim;
    protected virtual void Reset()
    {
        if (!notifyImge)
        {
            notifyImge = GetComponent<Image>();
        }
        if (!notifyScaleAnim) notifyScaleAnim = GetComponent<NotifyScaleAnim>();

#if UNITY_EDITOR
        if (notifyScaleAnim)
        {
            var so = new SerializedObject(this);

            // Add OnNotifyStateActive -> notifyScaleAnim.StartScaleAnim
            UnityEventEditorUtility.AddVoidPersistentListener(so.FindProperty("OnNotifyStateActive"), notifyScaleAnim, "StartScaleAnim");

            // Add OnNotifyStateInActive -> notifyScaleAnim.StopScaleAnim
            UnityEventEditorUtility.AddVoidPersistentListener(so.FindProperty("OnNotifyStateInActive"), notifyScaleAnim, "StopScaleAnim");

            so.ApplyModifiedProperties();
        }
#endif
    }

    protected virtual void Awake()
    {
        if (keyName == NotifyKeyName.None)
        {
            Debug.LogWarning("NotifyElement keyName is None, please set a valid keyName.");
            return;
        }

        NotifyFactory.Instance?.RegisterNotify(keyName, OnNotify);
    }
    protected void Start()
    {
        var isActive = NotifyFactory.Instance?.GetNotifyState(keyName) ?? false;
        OnNotify(isActive);
    }
    protected virtual void OnDestroy()
    {
        if (keyName == NotifyKeyName.None)
        {
            Debug.LogWarning("NotifyElement keyName is None, cannot unregister.");
            return;
        }
        
        NotifyFactory.Instance?.UnregisterNotify(keyName, OnNotify);
    }
    protected virtual void OnNotify(bool isActive)
    {
        if (!notifyImge)
        {
            Debug.LogWarning($"NotifyElement: notifyObject is null for keyName {keyName}");
            return;
        }
        
        notifyImge.enabled = isActive;
        if(isActive) OnNotifyStateActive?.Invoke();
        else OnNotifyStateInActive?.Invoke();
    }
}
