using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.Abilities
{
    public class FakeoutBall : DodgeBall
    {
        [SerializeField] private GameObject smokeEffect;

        public override void Start()
        {
            transform.parent = null;
            smokeEffect.transform.parent = null;
            _rb = GetComponent<Rigidbody>();
        }
        private void OnEnable()
        {
            smokeEffect.SetActive(false);
        }

        protected override void OnCollisionEnter(Collision other)
        {
            smokeEffect.transform.position = transform.position;
            smokeEffect.SetActive(true);
            
            gameObject.SetActive(false);
        }
    }
}