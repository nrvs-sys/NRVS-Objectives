using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;

namespace NRVS.Objectives 
{
    public abstract class ObjectiveBase : ScriptableObject
    {
        [Header("Base Settings")]

        [Tooltip("If true, this objective will not need to be completed to progress.")]
        public bool isOptional;

        [Header("Base Events")]

        public UnityEvent onCompleted;
        public UnityEvent onFailed;
        public UnityEvent onReset;

        #region Observer Notifications

        public delegate void ObjectiveBaseEvent(ObjectiveBase objective);
        public event ObjectiveBaseEvent CompleteInvoked;
        public event ObjectiveBaseEvent FailInvoked;

        #endregion

        public void Complete() => CompleteInvoked?.Invoke(this);
        public void Fail() => FailInvoked?.Invoke(this);

        public bool isCompleted => Ref.TryGet(out ObjectiveManager observer) && observer.IsComplete(this);
        public bool isFailed => Ref.TryGet(out ObjectiveManager observer) && observer.IsFailed(this);

        public abstract void Initialize(ObjectiveManager observer, ObjectiveManager.ObjectiveGroupState parentGroupState = null);
        public abstract void CleanUp();
    }
}