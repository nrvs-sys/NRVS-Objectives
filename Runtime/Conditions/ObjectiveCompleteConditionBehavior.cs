using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NRVS.Objectives
{
    [CreateAssetMenu(fileName = "Condition_ Objective_ Complete_ New", menuName = "Behaviors/Conditions/Objectives/Objective Complete")]
    public class ObjectiveCompleteConditionBehavior : ConditionBehavior<ObjectiveBase>
    {
        protected override bool Evaluate(ObjectiveBase value) => value.isCompleted;
    }
}
