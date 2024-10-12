using UnityEngine;

public abstract class BehaviorSO : ScriptableObject
{
    // This method will be overridden in derived SO classes to apply specific behaviors
    public abstract void ApplyBehavior(GameObject gameObject);
}
