using System.Collections.Generic;
using Gameplay.Characters.EnemyAI.Utilities.UtilityRefactor;
using Gameplay.Characters.EnemyAI.Utilities.UtilityRefactor.UtilityCalculators;
using Hands.SinglePlayer.EnemyAI.Priority;
using UnityEngine;

public class AISimulator : MonoBehaviour
{
    public Actor actor; // Reference to the AI actor we want to simulate
    public int simulationSteps = 100; // Number of steps to simulate
    public List<float> utilityValues = new List<float>(); // Store utility values for graphing

    public PriorityData priorityData;
    public float timeStep = 0.1f;
    public List<IUtilityCalculator> utilities = new List<IUtilityCalculator>();

    private bool isSimulating;

    public void AddCalculator(IUtilityCalculator calculator)
    {
        
    }
    public void CreateAndAddUtilityCalculator(UtilityType type, PriorityData priorityData)
    {
        priorityData.Initialize();
        switch (type)
        {
            case UtilityType.Actor:
                var thrw = new ThrowUtilityCalculator();
                thrw.PriorityData = priorityData;
                AddUtilityCalculator(thrw);
                break;
            case UtilityType.Ball:
                var pick = new PickUpUtilityCalculator();
                pick.PriorityData = priorityData;
                AddUtilityCalculator(pick);
                break;
            case UtilityType.Trajectory: // todo, we need to handle this better
                var dodge = new DodgeUtilityCalculator();
                var ctch = new CatchUtilityCalculator();
                dodge.PriorityData = priorityData;
                ctch.PriorityData = priorityData;
                AddUtilityCalculator(dodge);
                break;
        }
    }

    public void AddUtilityCalculator(IUtilityCalculator calculator)
    {
        utilities.Add(calculator);
    }

    public void SimulateAI()
    {
        actor.stateMatrix = new StateMatrix(actor, timeStep, utilities);
        actor.stateMatrix.onCalculationComplete += AcceptCalculation;

        isSimulating = true;
        utilities.Clear();
        utilityValues.Clear();
    }

    private void AcceptCalculation(int state)
    {
        utilityValues.Add(state);
        simulationSteps--;

        if (simulationSteps <= 0)
        {
            StopSimulation();
        }
    }

    private void StopSimulation()
    {
        if (!isSimulating) return;

        actor.stateMatrix.StopCalculations();
        actor.stateMatrix.onCalculationComplete -= AcceptCalculation;

        isSimulating = false;
    }

    private void EditorUpdate()
    {
        if (simulationSteps <= 0) StopSimulation();
    }
}