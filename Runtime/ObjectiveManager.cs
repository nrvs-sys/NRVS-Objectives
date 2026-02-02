using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using FishNet;

namespace NRVS.Objectives
{
    public class ObjectiveManager : MonoBehaviour
    {
        #region State Management Types

        public class ObjectiveState : IDisposable
        {
            public Objective objective { get; }
            public ObjectiveManager manager { get; }
            public ObjectiveGroupState parentGroupState { get; }

            public int successes { get; private set; } = 0;
            public bool isFailed { get; private set; } = false;

            public bool isCompleted { get; private set; } = false;

            Coroutine pollingCoroutine;

            public ObjectiveState(Objective objective, ObjectiveManager manager, ObjectiveGroupState parentGroupState = null)
            {
                this.objective = objective;
                this.manager = manager;
                this.parentGroupState = parentGroupState;

                this.objective.CompleteInvoked += Objective_CompleteInvoked;
                this.objective.FailInvoked += Objective_FailInvoked;
                this.objective.IncrementSuccessesInvoked += Objective_IncrementSuccessesInvoked;
                this.objective.ResetInvoked += Objective_ResetInvoked;

                if (manager.logObjectiveEvents)
                {
                    Debug.Log($"[ObjectiveManager] Registered Objective: {objective.name}");
                }

                if (objective.pollMethod == Objective.PollMethod.Condition)
                {
                    pollingCoroutine = this.manager.StartPollingObjective(this);
                }
            }

            public void IncrementSuccesses()
            {
                if (isCompleted || isFailed)
                    return;

                successes += 1;

                objective.onIncrementSuccesses?.Invoke(successes);
                manager.onObjectiveIncrementSuccess?.Invoke(objective);

                if (manager.logObjectiveEvents)
                {
                    Debug.Log($"[ObjectiveManager] Objective '{objective.name}' incremented successes to {successes}/{objective.successesBeforeComplete}");
                }

                if (successes >= objective.successesBeforeComplete)
                    Complete();
            }

            public void Complete()
            {
                if (isCompleted)
                    return;

                isCompleted = true;

                successes = objective.successesBeforeComplete;
                objective.onCompleted?.Invoke();
                manager.onObjectiveCompleted?.Invoke(objective);

                if (manager.logObjectiveEvents)
                {
                    Debug.Log($"[ObjectiveManager] Objective '{objective.name}' completed.");
                }

                parentGroupState?.OnSubObjectiveCompleted(this);
            }

            public void Fail()
            {
                if (isFailed)
                    return;

                isFailed = true;
                objective.onFailed?.Invoke();
                manager.onObjectiveFailed?.Invoke(objective);

                if (manager.logObjectiveEvents)
                {
                    Debug.Log($"[ObjectiveManager] Objective '{objective.name}' failed.");
                }

                parentGroupState?.OnSubObjectiveFailed(this);
            }

            public void Reset()
            {
                successes = 0;
                isFailed = false;
                objective.onReset?.Invoke();
                manager.onObjectiveReset?.Invoke(objective);

                if (manager.logObjectiveEvents)
                {
                    Debug.Log($"[ObjectiveManager] Objective '{objective.name}' reset.");
                }
            }

            void Objective_CompleteInvoked(ObjectiveBase objective) => Complete();
            void Objective_FailInvoked(ObjectiveBase objective) => Fail();
            void Objective_IncrementSuccessesInvoked(Objective objective) => IncrementSuccesses();
            void Objective_ResetInvoked(Objective objective) => Reset();

            public void Dispose()
            {
                if (objective != null)
                {
                    objective.CompleteInvoked -= Objective_CompleteInvoked;
                    objective.FailInvoked -= Objective_FailInvoked;
                    objective.IncrementSuccessesInvoked -= Objective_IncrementSuccessesInvoked;
                    objective.ResetInvoked -= Objective_ResetInvoked;
                }

                if (pollingCoroutine != null)
                {
                    manager?.StopCoroutine(pollingCoroutine);

                    pollingCoroutine = null;
                }
            }
        }

        public class ObjectiveGroupState : IDisposable
        {
            public ObjectiveGroup objectiveGroup { get; }
            public ObjectiveManager manager { get; }
            public ObjectiveGroupState parentGroupState { get; }

            private List<object> subObjectiveStates = new List<object>(); // Contains ObjectiveState or ObjectiveGroupState
            private int currentObjectiveIndex = 0;
            public bool isFailed { get; private set; } = false;
            public bool isCompleted { get; private set; } = false;

            public ObjectiveGroupState(ObjectiveGroup objectiveGroup, ObjectiveManager manager, ObjectiveGroupState parentGroupState = null)
            {
                this.objectiveGroup = objectiveGroup;
                this.manager = manager;
                this.parentGroupState = parentGroupState;

                this.objectiveGroup.CompleteInvoked += ObjectiveGroup_CompleteInvoked;
                this.objectiveGroup.FailInvoked += ObjectiveGroup_FailInvoked;

                Start();
            }

            private void Start()
            {
                objectiveGroup.onReset?.Invoke();

                if (manager.logObjectiveEvents)
                {
                    Debug.Log($"[ObjectiveManager] Registered Objective Group: {objectiveGroup.name}");
                }

                if (objectiveGroup.sequentialObjectives)
                {
                    ActivateNextObjective();
                }
                else
                {
                    foreach (var obj in objectiveGroup.objectives)
                    {
                        AddSubObjectiveState(obj);
                    }
                }
            }

            private void ActivateNextObjective()
            {
                if (currentObjectiveIndex < objectiveGroup.objectives.Count)
                {
                    var obj = objectiveGroup.objectives[currentObjectiveIndex];
                    AddSubObjectiveState(obj);
                    currentObjectiveIndex++;

                    if (manager.logObjectiveEvents)
                    {
                        Debug.Log($"[ObjectiveManager] Activated Objective '{obj.name}' in Group '{objectiveGroup.name}'");
                    }
                }
                else
                {
                    Complete();
                }
            }

            private void AddSubObjectiveState(ObjectiveBase obj)
            {
                if (obj is ObjectiveGroup group)
                {
                    var groupState = new ObjectiveGroupState(group, manager, this);
                    subObjectiveStates.Add(groupState);
                }
                else if (obj is Objective objective)
                {
                    var objectiveState = manager.RegisterObjectiveState(objective, this);
                    subObjectiveStates.Add(objectiveState);
                }
            }

            public void OnSubObjectiveCompleted(object subObjectiveState)
            {
                if (isCompleted || isFailed)
                    return;

                if (objectiveGroup.sequentialObjectives)
                {
                    if (objectiveGroup.delayBeforeNextObjective > 0)
                    {
                        manager.StartCoroutine(DelayedActivateNextObjective());
                    }
                    else
                    {
                        ActivateNextObjective();
                    }
                }
                else
                {
                    if (objectiveGroup.requireAllObjectives)
                    {
                        if (AllSubObjectivesCompleted())
                        {
                            Complete();
                        }
                    }
                    else
                    {
                        Complete();
                    }
                }
            }

            private System.Collections.IEnumerator DelayedActivateNextObjective()
            {
                yield return new WaitForSeconds(objectiveGroup.delayBeforeNextObjective);
                ActivateNextObjective();
            }

            public void OnSubObjectiveFailed(object subObjectiveState)
            {
                if (isFailed)
                    return;

                if (objectiveGroup.failGroupOnObjectiveFailure)
                {
                    Fail();
                }
            }

            private bool AllSubObjectivesCompleted()
            {
                foreach (var state in subObjectiveStates)
                {
                    if (state is ObjectiveState objState)
                    {
                        if (!objState.isCompleted && !objState.objective.isOptional)
                            return false;
                    }
                    else if (state is ObjectiveGroupState groupState)
                    {
                        if (!groupState.isCompleted && !groupState.objectiveGroup.isOptional)
                            return false;
                    }
                }
                return true;
            }

            public void Complete()
            {
                if (isCompleted)
                    return;

                isCompleted = true;
                objectiveGroup.onCompleted?.Invoke();
                manager.onObjectiveGroupCompleted?.Invoke(objectiveGroup);

                if (manager.logObjectiveEvents)
                {
                    Debug.Log($"[ObjectiveManager] Objective Group '{objectiveGroup.name}' completed.");
                }

                parentGroupState?.OnSubObjectiveCompleted(this);
            }

            public void Fail()
            {
                if (isFailed)
                    return;

                isFailed = true;
                objectiveGroup.onFailed?.Invoke();
                manager.onObjectiveGroupFailed?.Invoke(objectiveGroup);

                if (manager.logObjectiveEvents)
                {
                    Debug.Log($"[ObjectiveManager] Objective Group '{objectiveGroup.name}' failed.");
                }

                parentGroupState?.OnSubObjectiveFailed(this);
            }

            public void Reset()
            {
                isCompleted = false;
                isFailed = false;
                currentObjectiveIndex = 0;
                subObjectiveStates.Clear();

                if (manager.logObjectiveEvents)
                {
                    Debug.Log($"[ObjectiveManager] Objective Group '{objectiveGroup.name}' reset.");
                }
            }

            public void Dispose()
            {
                if (objectiveGroup != null)
                {
                    objectiveGroup.CompleteInvoked -= ObjectiveGroup_CompleteInvoked;
                    objectiveGroup.FailInvoked -= ObjectiveGroup_FailInvoked;
                }
            }

            void ObjectiveGroup_CompleteInvoked(ObjectiveBase objective) => Complete();
            void ObjectiveGroup_FailInvoked(ObjectiveBase objective) => Fail();
        }

        #endregion

        [Header("Settings")]

        public bool logObjectiveEvents = false;

        [Header("Events")]

        public UnityEvent<Objective> onObjectiveAdded;
        public UnityEvent<Objective> onObjectiveRemoved;
        public UnityEvent<Objective> onObjectiveCompleted;
        public UnityEvent<Objective> onObjectiveFailed;
        public UnityEvent<Objective> onObjectiveIncrementSuccess;
        public UnityEvent<Objective> onObjectiveReset;

        public UnityEvent<ObjectiveGroup> onObjectiveGroupAdded;
        public UnityEvent<ObjectiveGroup> onObjectiveGroupRemoved;
        public UnityEvent<ObjectiveGroup> onObjectiveGroupCompleted;
        public UnityEvent<ObjectiveGroup> onObjectiveGroupFailed;

        /// <summary>
        /// Contains ObjectiveState or ObjectiveGroupState
        /// </summary>
        List<object> rootObjectiveStates = new List<object>();
        Dictionary<Objective, ObjectiveState> objectiveStateMap = new Dictionary<Objective, ObjectiveState>();
        Dictionary<ObjectiveGroup, ObjectiveGroupState> groupStateMap = new Dictionary<ObjectiveGroup, ObjectiveGroupState>();


        #region Unity Methods

        void OnEnable()
        {
            Ref.Register<ObjectiveManager>(this);
        }

        void OnDisable()
        {
            Ref.Unregister<ObjectiveManager>(this);
        }

        #endregion

        #region Registration Methods

        public void Register(ObjectiveBase objective, ObjectiveGroupState parentGroupState = null)
        {
            if (objective is ObjectiveGroup group)
            {
                var groupState = new ObjectiveGroupState(group, this, parentGroupState);
                groupStateMap[group] = groupState;
                if (parentGroupState == null)
                    rootObjectiveStates.Add(groupState);

                onObjectiveGroupAdded?.Invoke(group);
            }
            else if (objective is Objective obj)
            {
                var objectiveState = RegisterObjectiveState(obj, parentGroupState);
                if (parentGroupState == null)
                    rootObjectiveStates.Add(objectiveState);

                onObjectiveAdded?.Invoke(obj);
            }
        }

        public ObjectiveState RegisterObjectiveState(Objective objective, ObjectiveGroupState parentGroupState = null)
        {
            if (objectiveStateMap.ContainsKey(objective))
                return objectiveStateMap[objective];

            var objectiveState = new ObjectiveState(objective, this, parentGroupState);
            objectiveStateMap[objective] = objectiveState;

            onObjectiveAdded?.Invoke(objective);

            return objectiveState;
        }

        public void Unregister(ObjectiveBase objective)
        {
            if (objective is ObjectiveGroup group)
            {
                if (groupStateMap.TryGetValue(group, out var groupState))
                {
                    groupStateMap.Remove(group);
                    rootObjectiveStates.Remove(groupState);
                    onObjectiveGroupRemoved?.Invoke(group);
                }

                // Unregister all sub-objectives
                foreach (var subObjective in group.objectives)
                {
                    Unregister(subObjective);
                }
            }
            else if (objective is Objective obj)
            {
                if (objectiveStateMap.TryGetValue(obj, out var objectiveState))
                {
                    objectiveStateMap.Remove(obj);
                    rootObjectiveStates.Remove(objectiveState);
                    onObjectiveRemoved?.Invoke(obj);

                    objectiveState.Dispose();
                }
            }
        }

        #endregion

        #region Polling Methods

        public Coroutine StartPollingObjective(ObjectiveState objectiveState)
        {
            return StartCoroutine(PollObjectiveCondition(objectiveState));
        }

        private System.Collections.IEnumerator PollObjectiveCondition(ObjectiveState objectiveState)
        {
            while (!objectiveState.isCompleted && !objectiveState.isFailed)
            {
                if (objectiveState.objective.conditionBehavior.If())
                    objectiveState.IncrementSuccesses();

                yield return null;
            }
        }

        #endregion

        #region State Query Methods

        public ObjectiveGroupState GetGroupState(ObjectiveGroup group)
        {
            groupStateMap.TryGetValue(group, out var groupState);
            return groupState;
        }

        public ObjectiveState GetObjectiveState(Objective objective)
        {
            objectiveStateMap.TryGetValue(objective, out var objectiveState);
            return objectiveState;
        }

        public bool IsRegistered(ObjectiveBase objective) => objective is ObjectiveGroup group ? groupStateMap.ContainsKey(group) : objectiveStateMap.ContainsKey(objective as Objective);

        public bool IsComplete(ObjectiveBase objective)
        {
            if (objective is ObjectiveGroup group)
            {
                return groupStateMap.TryGetValue(group, out var groupState) && groupState.isCompleted;
            }
            else if (objective is Objective obj)
            {
                return objectiveStateMap.TryGetValue(obj, out var objectiveState) && objectiveState.isCompleted;
            }

            return false;
        }

        public bool IsFailed(ObjectiveBase objective)
        {
            if (objective is ObjectiveGroup group)
            {
                return groupStateMap.TryGetValue(group, out var groupState) && groupState.isFailed;
            }
            else if (objective is Objective obj)
            {
                return objectiveStateMap.TryGetValue(obj, out var objectiveState) && objectiveState.isFailed;
            }

            return false;
        }

        public int GetSuccessCount(Objective objective) => objectiveStateMap.TryGetValue(objective, out var objectiveState) ? objectiveState.successes : 0;
       
        #endregion
    }
}
