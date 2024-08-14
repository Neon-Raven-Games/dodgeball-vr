using UnityEngine;

public class CooldownTimer : MonoBehaviour
{
    [SerializeField] private float cooldownDuration = 2f;
    private float cooldownTimer;

    private bool _isAvailable;
    private void Update()
    {
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }
        else
        {
            _isAvailable = true;
        }
    }
    
    public float NormalizedProgress()
    {
        return Mathf.Clamp01(1 - cooldownTimer / cooldownDuration);
    }
    
    public bool IsAvailable()
    {
        return _isAvailable;
    }

    public void StartCooldown()
    {
        _isAvailable = false;
        cooldownTimer = cooldownDuration;
    }

    public bool IsOnCooldown()
    {
        return cooldownTimer > 0;
    }

    public void SetCooldownTime(float suctionCooldown)
    {
        cooldownDuration = suctionCooldown;
    }
}