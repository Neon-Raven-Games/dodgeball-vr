using UnityEngine;

public class BallLauncherAnimationEvents : MonoBehaviour
{
    /// <summary>
    /// Reference to the ball launcher script.
    /// </summary>
    [SerializeField] private HandCannon ballLauncherReference;
    
    /// <summary>
    /// Changes teh ball launcher state to shooting.
    /// </summary>
    public void ShootBall()
    {
         ballLauncherReference.Shoot();
    }
}
