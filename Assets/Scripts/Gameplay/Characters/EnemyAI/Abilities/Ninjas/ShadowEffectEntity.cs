using System;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.Abilities
{
    public class ShadowEffectEntity : MonoBehaviour
    {
        public float moveSpeed;
        
        [SerializeField] private float xDirection;
        [SerializeField] private float distanceToTravel;
        [SerializeField] private float _moveDistance;

        private void OnDisable()
        {
            _moveDistance = 0;
        }

        public void SetDirectionAndDistance(float xDirection, float distanceToTravel)
        {
            this.xDirection = xDirection;
            this.distanceToTravel = distanceToTravel;
        }
        private void Update()
        {
            if (xDirection == 0)
            {
                _moveDistance += moveSpeed * Time.deltaTime;
                transform.position += new Vector3(0, 0, moveSpeed * Time.deltaTime);
            }
            else
            {
                _moveDistance += moveSpeed * Time.deltaTime;
                transform.position -= new Vector3(0, 0, moveSpeed * Time.deltaTime);
            }

            if (_moveDistance >= distanceToTravel)
            {
                gameObject.SetActive(false);
            }
        }
    }
}