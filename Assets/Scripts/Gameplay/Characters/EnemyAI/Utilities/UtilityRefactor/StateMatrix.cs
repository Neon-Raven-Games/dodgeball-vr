using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Hands.SinglePlayer.EnemyAI.Priority;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor;
using UnityEngine;

namespace Gameplay.Characters.EnemyAI.Utilities.UtilityRefactor
{
    public class StateMatrix
    {
        public event Action<int> onCalculationComplete;
        public event Action<Vector3, DodgeBall> ballThrown;
        
        private Bounds _playAreaBounds;
        
        private readonly MaxHeap<int> _stateUtilities = new();
        
        private readonly List<IUtilityCalculator> _actorUtilities = new();
        private readonly List<IUtilityCalculator> _ballUtilities = new();
        private readonly List<IUtilityCalculator> _trajectoryUtilities = new();
        
        private readonly Team _team;
        private readonly List<Actor> _actors;
        private readonly List<DodgeBall> _dodgeballs;
        private readonly PriorityData _priorityData;
        private readonly Actor _owner;
        private readonly DodgeballPlayArea _playArea;
        private readonly float _timeStep;
        private bool _calculating;

        public void StopCalculations() => 
            _calculating = false;
        
        public void StartCalculations() => 
            _calculating = true;
        
        private bool IsWithinBounds(Vector3 position) => 
            _playAreaBounds.Contains(position);

        public StateMatrix(Actor owner,float timeStep, List<IUtilityCalculator> utilities)
        {
            InitializeUtilities(utilities);
            _owner = owner;
            _team = owner.team;
            _playArea = owner.playArea;
            _playAreaBounds =
                new Bounds(owner.playArea.team1PlayArea.position,
                    new Vector3(owner.playArea.team1PlayArea.localScale.x, 5,
                        owner.playArea.team1PlayArea.localScale.z));

            _actors = owner.playArea.team1Actors.ToList();
            _actors.AddRange(owner.playArea.team2Actors);
            ballThrown += OnBallThrown;
            _timeStep = timeStep;
            _calculating = true;
            PopulateMatrixAsync(_owner).Forget();
        }

        private int GetBestState()
        {
            if (_stateUtilities.Count == 0) 
                return StateStruct.Move;
            return _stateUtilities.ExtractMax();
        }

        private void InitializeUtilities(List<IUtilityCalculator> utilities)
        {
            foreach (var util in utilities)
            {
                if ((util.Type & UtilityType.Actor) != 0) _actorUtilities.Add(util);
                if ((util.Type & UtilityType.Ball) != 0) _ballUtilities.Add(util);
                if ((util.Type & UtilityType.Trajectory) != 0) _trajectoryUtilities.Add(util);
            }
        }

        private async UniTaskVoid PopulateMatrixAsync(Actor actor)
        {
            _stateUtilities.Clear();
            await UniTask.WaitForSeconds(_timeStep);

            // 5 actors in actual gameplay, means 5 frames to calculate all actors, shouldn't be too stale
            foreach (var otherActor in _actors)
            {
                CalculateActorUtilities(actor, otherActor);
                await UniTask.Yield();
            }
            
            // dodgeballs can change often so we want to calculate them all without a step
            foreach (var ball in _playArea.dodgeBalls.Keys) CalculateBallUtilities(actor, ball);

            // wait for a final frame spreading calculations across 6 frames
            await UniTask.Yield();
            
            // if we are not calculating, safe to toss last result
            if (!_calculating) return;
            
            onCalculationComplete?.Invoke(GetBestState());
            PopulateMatrixAsync(_owner).Forget();
        }

        /// <summary>
        /// Calculates Ball utilities.
        /// </summary>
        /// <param name="actor">The owner actor and each dodgeball in play during ~1 frame ago.</param>
        /// <param name="ball"></param>
        private void CalculateBallUtilities(Actor actor, DodgeBall ball)
        {
            foreach (var util in _ballUtilities)
            {
                if (!actor || !ball) continue;
                var roll = util.CalculateBallUtility(actor, ball);
                if (roll > 0f) _stateUtilities.Add(roll, util.State);
            }
        }

        /// <summary>
        /// Calculates actor utilities from owner to other.
        /// </summary>
        private void CalculateActorUtilities(Actor owner, Actor otherActor)
        {
            foreach (var util in _actorUtilities)
            {
                if (!owner || !otherActor || owner == otherActor) continue;
                var roll = util.CalculateActorUtility(owner, otherActor);
                if (roll > 0f) _stateUtilities.Add(roll, util.State);
            }
        }

        /// <summary>
        /// Callback for when a dodgeball is thrown.
        /// </summary>
        public void DodgeballThrown(Vector3 trajectory, DodgeBall ball) => ballThrown?.Invoke(trajectory, ball);

        /// <summary>
        /// Callback to provide immediate utility calculations for catch/dodge states. The ai should delay the visual response,
        /// but it should know what to do immediately.
        /// </summary>
        private void OnBallThrown(Vector3 trajectory, DodgeBall ball)
        {
            if (ball.team == _team) return;
            foreach (var util in _trajectoryUtilities)
            {
                var roll = util.CalculateTrajectoryUtility(_owner, ball, trajectory);
                if (roll > 0f) _stateUtilities.Add(roll, util.State);
            }
        }
    }
}