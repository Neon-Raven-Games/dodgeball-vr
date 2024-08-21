using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Hands.SinglePlayer.EnemyAI.Watchers
{
    public class NinjaWatcher : MonoBehaviour
    {
        [SerializeField] private float centerOffsetEndPositions = 1.5f;
        [SerializeField] private float smokeBombDuration = 25f;
        [SerializeField] private float registrationInterval = 0.4f;
        [SerializeField] private ShadowCourt shadowCourt;
        [SerializeField] private DodgeballPlayArea playArea;

        private readonly List<NinjaAgent> _ninjaAgents = new();
        private readonly List<Vector3> _agentPositions = new();
        
        
        private int _handSignCount = 0;
        private bool _smokeBomb = false;
        private bool _agentsTraveling;

        private void Start()
        {
            foreach (var agent in playArea.team2Actors)
            {
                _ninjaAgents.Add(agent as NinjaAgent);
            }
        }

        private void FixedUpdate()
        {
            if (_agentsTraveling)
            {
                var i = 0;
                foreach (var agent in _ninjaAgents)
                {
                    var closestPoint = _agentPositions.OrderBy(x => 
                        Vector3.Distance(x, agent.transform.position)).First();
                    var distance = Vector3.Distance(agent.transform.position, closestPoint);
                    if (distance < 0.3f) i++;
                }
                
                if (i == _ninjaAgents.Count)
                {
                    _agentsTraveling = false;
                    shadowCourt.smokeScreenDuration = smokeBombDuration;
                    
                    ShadowCourt.SmokeScreen();
                    WaitForSmokeScreenEnd().Forget();
                }
            }

            if (!_smokeBomb) return;
            _handSignCount = 0;
            foreach (var ninja in _ninjaAgents)
            {
                if (ninja.State == NinjaState.HandSign)
                {
                    _handSignCount++;
                }
            }

            if (_handSignCount == _ninjaAgents.Count) SmokeBomb();
        }

        private async UniTaskVoid WaitForSmokeScreenEnd()
        {
            await UniTask.WaitForSeconds(smokeBombDuration);
            _smokeBomb = false;
            _handSignCount = 0;
            foreach (var agent in _ninjaAgents)
            {
                agent.EndSmokeBomb();
            }
        }

        public void SmokeBomb()
        {
            _smokeBomb = true;
            RegisterAgentRoutine().Forget();
        }

        private async UniTaskVoid RegisterAgentRoutine()
        {
            var center = PrepareAgentsForSmokeBomb();
            await UniTask.WaitForSeconds(registrationInterval);
            foreach (var agent in _ninjaAgents)
            {
                var direction = center - agent.transform.position;
                var endPoint = center - direction.normalized * centerOffsetEndPositions;
                agent.SmokeBomb(endPoint);
                _agentPositions.Add(endPoint);
                await UniTask.WaitForSeconds(registrationInterval);
            }
            _agentsTraveling = true;
        }

        private Vector3 PrepareAgentsForSmokeBomb()
        {
            var endPositions = new List<Vector3>();
            foreach (var agent in _ninjaAgents)
            {
                endPositions.Add(agent.transform.position);
                agent.PrepareSmokeBomb();
            }

            return CalculateCenter(endPositions);
        }

        private static Vector3 CalculateCenter(List<Vector3> agentPositions)
        {
            var center = Vector3.zero;
            foreach (var position in agentPositions) center += position;
            center /= agentPositions.Count;
            return center;
        }
    }
}