using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.Priority
{
    public class PriorityHandler : MonoBehaviour
    {
        public void Awake()
        {
            targetUtility.Initialize();
            dodgeUtility.Initialize();
            moveUtility.Initialize();
            catchUtility.Initialize();
            pickUpUtility.Initialize();
            throwUtility.Initialize();
            outOfBoundsUtility.Initialize();
        }

        public PriorityData targetUtility;
        public PriorityData dodgeUtility;
        public PriorityData moveUtility;
        public PriorityData catchUtility;
        public PriorityData pickUpUtility;
        public PriorityData throwUtility;
        public PriorityData outOfBoundsUtility;
        public float maxValue;
        public bool recative;
    }

}