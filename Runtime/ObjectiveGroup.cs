using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static NRVS.Objectives.ObjectiveManager;

namespace NRVS.Objectives
{
    [CreateAssetMenu(fileName = "Objective Group_ New", menuName = "Game/Objective Group")]
    public sealed class ObjectiveGroup : ObjectiveBase
    {
        [Header("Objective Group Settings")]

        [Tooltip("If true, objectives in this group must be completed in sequence.")]
        public bool sequentialObjectives = false;

        [Tooltip("If true, the group will only be considered complete when all objectives are completed.")]
        public bool requireAllObjectives = true;

        public bool failGroupOnObjectiveFailure = true;

        public float delayBeforeNextObjective = 0f;

        [Space(10)]

        public List<ObjectiveBase> objectives = new();

        public override void Initialize(ObjectiveManager observer, ObjectiveGroupState parentGroupState = null)
        {
            observer.Register(this, parentGroupState);
        }

        public override void CleanUp()
        {
            // Cleanup handled by ObjectiveObserver
        }
    }
}
