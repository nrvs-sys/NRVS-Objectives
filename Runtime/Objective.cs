using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using NaughtyAttributes;
using UnityEngine.Localization;

namespace NRVS.Objectives
{
    [CreateAssetMenu(fileName = "Objective_ New", menuName = "Game/Objective")]
    public sealed class Objective : ObjectiveBase
    {
        public enum PollMethod
        {
            None,
            Condition,
        }

        #region Serialized Fields

        [Header("Objective Settings")]

        [Min(1)]
        public int successesBeforeComplete = 1;

        [Header("Objective Query Settings")]

        public PollMethod pollMethod;

        [ShowIf("pollMethod", PollMethod.Condition)]
        public ConditionBehavior conditionBehavior;

        [Header("Objective Events")]

        public UnityEvent<int> onIncrementSuccesses;

        #endregion

        public int successes => Ref.TryGet(out ObjectiveManager manager) ? manager.GetSuccessCount(this) : 0;

        #region Observer Notifications

        public delegate void ObjectiveEvent(Objective objective);
        public event ObjectiveEvent IncrementSuccessesInvoked;
        public event ObjectiveEvent ResetInvoked;

        #endregion

        public void IncrementSuccesses() => IncrementSuccessesInvoked?.Invoke(this);

        public void IncrementSuccesses(int amount)
        {
            for (int i = 0; i < amount; i++)
                IncrementSuccesses();
        }

        public void IncrementSuccesses(float amount) => IncrementSuccesses((int)amount);

        public void ResetObjective() => ResetInvoked?.Invoke(this);

        public override void Initialize(ObjectiveManager observer, ObjectiveManager.ObjectiveGroupState parentGroupState = null)
        {
            observer.Register(this, parentGroupState);
        }

        public override void CleanUp()
        {
            // Cleanup handled by ObjectiveObserver
        }
    }
}
