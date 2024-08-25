using System.Collections.Generic;
using System.Linq;
using Gameplay.InGameEvents;
using UnityEngine;

public class Actor : MonoBehaviour
{
    // todo, shown in inspector for debugging
    public bool hasBall;
    [SerializeField] protected bool outOfPlay;
    public Transform head;
    public Team team;
    internal float outOfBoundsEndTime;
    internal ActorTeam friendlyTeam;
    public ActorTeam opposingTeam;
    public DodgeballPlayArea playArea;
    public float outOfBoundsWaitTime = 1f;
    public Dictionary<Collider, bool> colliderMap = new();

    public bool IsColliderOwner(Collider collider) => colliderMap.ContainsKey(collider);
    
    protected void PopulateTeamObjects()
    {
        var colliders = GetComponentsInChildren<Collider>();
        foreach (var collider in colliders) colliderMap[collider] = true;
        
        if (!playArea)
        {
            Debug.LogWarning("Actor could not find dodgeball play area.");
            return;
        }
        if (team == Team.TeamOne)
        {
            friendlyTeam = new ActorTeam
            {
                actors = playArea.team1Actors.ToList(),
                color = playArea.team1Color,
                playArea = playArea.team1PlayArea,
                outOfBounds = playArea.team1OutOfBounds,
                layerName = "TeamOne"
            };
            opposingTeam = new ActorTeam
            {
                actors = playArea.team2Actors.ToList(),
                color = playArea.team2Color,
                playArea = playArea.team2PlayArea,
                outOfBounds = playArea.team2OutOfBounds,
                layerName = "TeamTwo"
            };
        }
        else
        {
            friendlyTeam = new ActorTeam
            {
                // todo this initialized mid way, scene swap doesn't work til after init
                actors = playArea.team2Actors.ToList(),
                color = playArea.team2Color,
                playArea = playArea.team2PlayArea,
                outOfBounds = playArea.team2OutOfBounds,
                layerName = "TeamTwo"
            };

            opposingTeam = new ActorTeam
            {
                actors = playArea.team1Actors.ToList(),
                color = playArea.team1Color,
                playArea = playArea.team1PlayArea,
                outOfBounds = playArea.team1OutOfBounds,
                layerName = "TeamOne"
            };
        }
    }

    internal bool IsOutOfPlay() => outOfPlay;
    internal virtual void SetOutOfPlay(bool value)
    {
        outOfPlay = value;
        if (value) PhaseManager.DecreaseTeamLife(team);
    }

    protected void HandleOutOfPlay()
    {
        if (!ValidWaitingArea()) outOfBoundsEndTime = Time.time + outOfBoundsWaitTime;
        
        if (Time.time >= outOfBoundsEndTime) SetOutOfPlay(false);
        

    }

    public bool IsInPlayArea(Vector3 position)
    {
        var playAreaBounds = new Bounds(friendlyTeam.playArea.position,
            new Vector3(friendlyTeam.playArea.localScale.x, 5,
                friendlyTeam.playArea.localScale.z));
        return playAreaBounds.Contains(position);
    }

    private bool ValidWaitingArea()
    { 
        var waitBounds = new Bounds(friendlyTeam.outOfBounds.position,
            new Vector3(friendlyTeam.outOfBounds.localScale.x, 5,
                friendlyTeam.outOfBounds.localScale.z));
        return waitBounds.Contains(Camera.main.transform.position);
    }
}