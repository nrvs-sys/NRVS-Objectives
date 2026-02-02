using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NRVS.Objectives
{
    [CreateAssetMenu(fileName = "Condition_ Objective_ Active_ New", menuName = "Behaviors/Conditions/Objectives/Objective Active")]
    public class ObjectiveActiveConditionBehavior : ConditionBehavior<ObjectiveBase>
    {
        protected override bool Evaluate(ObjectiveBase value) => Ref.TryGet(out ObjectiveManager objectiveManager) && objectiveManager.IsRegistered(value);
    }
}
