using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Characters.EnemyAI.Utilities.UtilityRefactor;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.Priority
{

    [Serializable] 
    public class PriorityData : ScriptableObject
    {
        public UtilityType utilityType;
        private readonly Dictionary<PriorityType, float> _priorityValues = new();
        public string name;
        public float GetPriorityValue(PriorityType priorityType)
        {
            if (!_priorityValues.ContainsKey(priorityType))
            {
                return 1f;
            }
            return _priorityValues[priorityType];
        }

        public List<Priority> priorities = new();
        public float maxValue;
        public float currentValue;
        public bool recative = true;

        // call this at runtime to initialize the priority values
        public void Initialize()
        {
            _priorityValues.Clear();
            foreach (var p in priorities)
                _priorityValues.Add(p.priority, p.score);
        }

        // shown in the inspector to display our current total value of all priorities
        public void UpdateCurrentValue()
        {
            currentValue = 0;
            foreach (var priority in priorities)
            {
                currentValue += priority.score;
            }
        }
        
        // sets a new max for our priorities
        public void SetNewMaxPriorityValue(float value)
        {
            foreach (var priority in priorities)
            {
                maxValue = value;
                if (priority.score > value) priority.score = value;
            }
        }

        public void BalancePrioritiesAround(PriorityType priorityType, float newValue)
        {
            var priority = priorities.Find(p => p.priority == priorityType);
            if (priority == null) return;

            if (!recative)
            {
                priority.score = newValue;
                return;
            }

            float currentTotal = 0;
            foreach (var p in priorities)
            {
                currentTotal += p.score;
            }

            float difference = newValue - priority.score;
            priority.score = newValue;

            float adjustmentFactor = difference / (priorities.Count - 1);
            foreach (var p in priorities)
            {
                if (p.priority == priorityType) continue;

                p.score -= adjustmentFactor;
                if (p.score < 0) p.score = 0;
                if (p.score > maxValue) p.score = maxValue;
            }

            // Ensure the total sum of priorities equals maxValue
            AdjustPrioritiesToMaxValue();
        }
        private void AdjustPrioritiesToMaxValue()
        {
            float currentTotal = 0;
            foreach (var p in priorities)
            {
                currentTotal += p.score;
            }

            float adjustmentFactor = (maxValue - currentTotal) / priorities.Count;
            foreach (var p in priorities)
            {
                p.score += adjustmentFactor;
                if (p.score < 0) p.score = 0;
                if (p.score > maxValue) p.score = maxValue;
            }

            // Final adjustment to ensure the sum is exactly maxValue
            currentTotal = 0;
            foreach (var p in priorities)
            {
                currentTotal += p.score;
            }

            float finalAdjustment = maxValue - currentTotal;
            if (finalAdjustment != 0)
            {
                foreach (var p in priorities)
                {
                    if (p.score + finalAdjustment <= maxValue && p.score + finalAdjustment >= 0)
                    {
                        p.score += finalAdjustment;
                        break;
                    }
                }
            }
        }
        
        // balance all the priorities based on their current values
        public void BalancePriorities()
        {
            float total = 0;
            foreach (var priority in priorities)
            {
                total += priority.score;
            }

            if (total == 0) return;

            foreach (var priority in priorities)
            {
                priority.score /= total;
                priority.score *= maxValue;
            }
        }

        public bool ContainsPriority(PriorityType priority) =>
            _priorityValues.ContainsKey(priority) || priorities.Any(x => x.priority == priority);
    }
}