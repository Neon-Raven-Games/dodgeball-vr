using System.Linq;
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

    protected void PopulateTeamObjects()
    {
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
    internal void SetOutOfPlay(bool value) => outOfPlay = value;

    protected void HandleOutOfPlay()
    {
        if (IsInPlayArea(transform.position)) outOfBoundsEndTime = Time.time + outOfBoundsWaitTime;
        if (Time.time >= outOfBoundsEndTime) outOfPlay = false;
    }

    public bool IsInPlayArea(Vector3 position)
    {
        var playAreaBounds = new Bounds(friendlyTeam.playArea.position,
            new Vector3(friendlyTeam.playArea.localScale.x, 5,
                friendlyTeam.outOfBounds.localScale.z));
        return playAreaBounds.Contains(position);
    }
}