using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.Watchers
{
    public class NinjaWatcher : MonoBehaviour
    {
        [SerializeField] private float centerOffsetEndPositions = 1.5f;
        [SerializeField] public float smokeBombDuration = 25f;
        [SerializeField] private float registrationInterval = 0.4f;
        [SerializeField] private ShadowCourt shadowCourt;
        [SerializeField] private DodgeballPlayArea playArea;
        [SerializeField] float disappearDelaySeconds;

        public List<NinjaAgent> _ninjaAgents;
        private bool _smokeBomb;

        public void SmokeBomb()
        {
            if (_smokeBomb) return;
            _smokeBomb = true;
            shadowCourt.smokeScreenDuration = smokeBombDuration;
            ShadowCourt.SmokeScreen();
            var center = PrepareAgentsForSmokeBomb();
            StartCoroutine(RegisterAgentRoutine(center));
        }

        private IEnumerator WaitForSmokeScreenEnd()
        {
            yield return new WaitForSeconds(smokeBombDuration);
            foreach (var agent in _ninjaAgents) agent.EndSmokeBomb();
            yield return null;
            _smokeBomb = false;
        }

        private IEnumerator RegisterAgentRoutine(Vector3 center)
        {
            yield return new WaitForSeconds(disappearDelaySeconds);
            lock (_ninjaAgents)
            {
                foreach (var agent in _ninjaAgents)
                {
                    var direction = center - agent.transform.position;
                    var endPoint = center - direction.normalized * centerOffsetEndPositions;
                    agent.SmokeBomb(endPoint);
                    yield return new WaitForSeconds(registrationInterval);
                }
            }

            StartCoroutine(WaitForSmokeScreenEnd());
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