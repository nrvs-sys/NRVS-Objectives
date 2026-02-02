using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NRVS.Objectives
{
    public class ObjectivesUtility : MonoBehaviour
    {
        public List<ObjectiveBase> objectives;

        [Header("Events")]

        public UnityEvent onAllObjectivesCompleted;
        public UnityEvent onAnyObjectiveFailed;

        private ObjectiveManager objectiveManager;

        private Dictionary<ObjectiveBase, UnityAction> onCompletedHandlers = new Dictionary<ObjectiveBase, UnityAction>();
        private Dictionary<ObjectiveBase, UnityAction> onFailedHandlers = new Dictionary<ObjectiveBase, UnityAction>();

        void OnEnable()
        {
            if (Ref.TryGet(out objectiveManager))
            {
                foreach (var objective in objectives)
                {
                    objective.Initialize(objectiveManager);
                    SubscribeToObjectiveEvents(objective);
                }
            }
        }

        void OnDisable()
        {
            if (objectiveManager != null)
            {
                foreach (var objective in objectives)
                {
                    UnsubscribeFromObjectiveEvents(objective);
                    objective.CleanUp();
                    objectiveManager.Unregister(objective);
                }
            }
        }

        private void SubscribeToObjectiveEvents(ObjectiveBase objective)
        {
            UnityAction onCompletedHandler = null;
            UnityAction onFailedHandler = null;

            onCompletedHandler = () => OnObjectiveCompleted(objective);
            onFailedHandler = () => OnObjectiveFailed(objective);

            onCompletedHandlers[objective] = onCompletedHandler;
            onFailedHandlers[objective] = onFailedHandler;

            objective.onCompleted.AddListener(onCompletedHandler);
            objective.onFailed.AddListener(onFailedHandler);

            if (objective is ObjectiveGroup group)
            {
                foreach (var subObjective in group.objectives)
                {
                    SubscribeToObjectiveEvents(subObjective);
                }
            }
        }

        private void UnsubscribeFromObjectiveEvents(ObjectiveBase objective)
        {
            if (onCompletedHandlers.TryGetValue(objective, out var onCompletedHandler))
            {
                objective.onCompleted.RemoveListener(onCompletedHandler);
                onCompletedHandlers.Remove(objective);
            }

            if (onFailedHandlers.TryGetValue(objective, out var onFailedHandler))
            {
                objective.onFailed.RemoveListener(onFailedHandler);
                onFailedHandlers.Remove(objective);
            }

            if (objective is ObjectiveGroup group)
            {
                foreach (var subObjective in group.objectives)
                {
                    UnsubscribeFromObjectiveEvents(subObjective);
                }
            }
        }

        private void OnObjectiveCompleted(ObjectiveBase objective)
        {
            if (AreAllRootObjectivesCompleted())
                onAllObjectivesCompleted?.Invoke();
        }

        private void OnObjectiveFailed(ObjectiveBase objective)
        {
            onAnyObjectiveFailed?.Invoke();
        }

        private bool AreAllRootObjectivesCompleted()
        {
            foreach (var objective in objectives)
            {
                if (!IsObjectiveCompleted(objective))
                    return false;
            }
            return true;
        }

        private bool IsObjectiveCompleted(ObjectiveBase objective)
        {
            if (objective is ObjectiveGroup group)
            {
                var groupState = objectiveManager.GetGroupState(group);
                return groupState != null && groupState.isCompleted;
            }
            else if (objective is Objective obj)
            {
                var objectiveState = objectiveManager.GetObjectiveState(obj);
                return objectiveState != null && objectiveState.isCompleted;
            }
            return false;
        }
    }
}
