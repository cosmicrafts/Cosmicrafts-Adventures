using UnityEngine;
using Unity.Netcode;

public class TeamComponent : NetworkBehaviour
{
    public enum TeamTag
    {
        Friend,
        Neutral,
        Enemy
    }

    public NetworkVariable<TeamTag> PlayerTeam = new NetworkVariable<TeamTag>(
        TeamTag.Neutral, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Start()
    {
        // Set the layer based on the team
        SetLayerBasedOnTeam(PlayerTeam.Value);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Set all players to be friends by default
            SetTeam(TeamTag.Friend);
        }
    }

    public void SetTeam(TeamTag team)
    {
        if (IsServer)
        {
            PlayerTeam.Value = team;
            SetLayerBasedOnTeam(team);
        }
    }

    private void SetLayerBasedOnTeam(TeamTag team)
    {
        switch (team)
        {
            case TeamTag.Friend:
                gameObject.layer = LayerMask.NameToLayer("Friend");
                break;
            case TeamTag.Enemy:
                gameObject.layer = LayerMask.NameToLayer("Enemy");
                break;
            case TeamTag.Neutral:
                gameObject.layer = LayerMask.NameToLayer("Neutral");
                break;
        }
    }

    public TeamTag GetTeam()
    {
        return PlayerTeam.Value;
    }
}
