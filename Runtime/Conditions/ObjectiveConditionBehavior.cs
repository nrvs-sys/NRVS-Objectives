using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NRVS.Objectives
{
    [CreateAssetMenu(fileName = "Condition_ Objective Is_ New", menuName = "Behaviors/Conditions/Objectives/Objective Is")]
    public class ObjectiveConditionBehavior : ConditionBehavior<ObjectiveBase>
    {
        [SerializeField]
        ObjectiveBase equalTo;

        protected override bool Evaluate(ObjectiveBase value) => value == equalTo;
    }
}
