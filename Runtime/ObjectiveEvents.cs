using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace NRVS.Objectives
{
    public class ObjectiveEvents : MonoBehaviour
    {
        [SerializeField, Expandable]
        private ObjectiveBase objective;

        [Header("Base Events")]
        public UnityEvent<ObjectiveBase> onObjectiveAdded;
        public UnityEvent<ObjectiveBase> onObjectiveRemoved;

        [Space(10)]

        public UnityEvent<ObjectiveBase> onObjectiveCompleted;
        public UnityEvent<ObjectiveBase> onObjectiveFailed;
        public UnityEvent<ObjectiveBase> onObjectiveReset;

        [Header("Objective Events")]

        public UnityEvent<Objective> onObjectiveIncrementSuccesses;

        private bool objectiveListenersAdded = false;

        void OnEnable()
        {
            Ref.Instance.OnRegistered += Ref_OnRegistered;
            Ref.Instance.OnUnregistered += Ref_OnUnregistered;

            if (Ref.TryGet<ObjectiveManager>(out var objectiveManager))
                Ref_OnRegistered(typeof(ObjectiveManager), objectiveManager);
            else if (objectiveListenersAdded)
                Ref_OnUnregistered(typeof(ObjectiveManager), objectiveManager);
        }

        void OnDisable()
        {
            if (Ref.Instance != null)
            {
                Ref.Instance.OnRegistered -= Ref_OnRegistered;
                Ref.Instance.OnUnregistered -= Ref_OnUnregistered;

                if (Ref.TryGet<ObjectiveManager>(out var objectiveManager))
                    Ref_OnUnregistered(typeof(ObjectiveManager), objectiveManager);
            }
        }

        private void OnDestroy()
        {
            if (objective != null && objectiveListenersAdded)
            {
                objective.onCompleted.RemoveListener(Objective_onObjectiveCompleted);
                objective.onFailed.RemoveListener(Objective_onObjectiveFailed);
                objective.onReset.RemoveListener(Objective_onReset);

                var o = objective as Objective;
                if (o != null)
                {
                    o.onIncrementSuccesses.RemoveListener(Objective_onIncrementSuccesses);
                }

                objectiveListenersAdded = false;
            }
        }

        void Ref_OnRegistered(System.Type type, object instance)
        {
            var objectiveManager = instance as ObjectiveManager;
            if (objectiveManager != null)
            {
                objectiveManager.onObjectiveAdded.AddListener(ObjectiveManager_onObjectiveAdded);
                objectiveManager.onObjectiveRemoved.AddListener(ObjectiveManager_onObjectiveRemoved);

                if (!objectiveListenersAdded && objectiveManager.IsRegistered(objective))
                {
                    ObjectiveManager_onObjectiveAdded(objective);
                }
                else if (objectiveListenersAdded && !objectiveManager.IsRegistered(objective))
                {
                    ObjectiveManager_onObjectiveRemoved(objective);
                }
            }
        }

        void Ref_OnUnregistered(System.Type type, object instance)
        {
            var objectiveManager = instance as ObjectiveManager;
            if (objectiveManager != null)
            {
                objectiveManager.onObjectiveAdded.RemoveListener(ObjectiveManager_onObjectiveAdded);
                objectiveManager.onObjectiveRemoved.RemoveListener(ObjectiveManager_onObjectiveRemoved);
            }
        }

        void ObjectiveManager_onObjectiveAdded(ObjectiveBase obj)
        {
            if (obj == objective)
            {
                if (!objectiveListenersAdded)
                {
                    objective.onCompleted.AddListener(Objective_onObjectiveCompleted);
                    objective.onFailed.AddListener(Objective_onObjectiveFailed);
                    objective.onReset.AddListener(Objective_onReset);

                    var o = objective as Objective;
                    if (o != null)
                    {
                        o.onIncrementSuccesses.AddListener(Objective_onIncrementSuccesses);
                    }

                    objectiveListenersAdded = true;
                    onObjectiveAdded?.Invoke(objective);
                }
            }
        }

        void ObjectiveManager_onObjectiveRemoved(ObjectiveBase obj)
        {
            if (obj == objective)
            {
                if (objectiveListenersAdded)
                {
                    objective.onCompleted.RemoveListener(Objective_onObjectiveCompleted);
                    objective.onFailed.RemoveListener(Objective_onObjectiveFailed);
                    objective.onReset.RemoveListener(Objective_onReset);

                    var o = objective as Objective;

                    if (o != null)
                    {
                        o.onIncrementSuccesses.RemoveListener(Objective_onIncrementSuccesses);
                    }

                    objectiveListenersAdded = false;
                    onObjectiveRemoved?.Invoke(objective);
                }
            }
        }

        void Objective_onObjectiveCompleted()
        {
            if (gameObject.activeInHierarchy)
                onObjectiveCompleted?.Invoke(objective);
        }

        void Objective_onObjectiveFailed()
        {
            if (gameObject.activeInHierarchy)
                onObjectiveFailed?.Invoke(objective);
        }

        void Objective_onIncrementSuccesses(int amt)
        {
            if (gameObject.activeInHierarchy)
                onObjectiveIncrementSuccesses?.Invoke(objective as Objective);
        }

        void Objective_onReset()
        {
            if (gameObject.activeInHierarchy)
                onObjectiveReset?.Invoke(objective as Objective);
        }
    }
}
