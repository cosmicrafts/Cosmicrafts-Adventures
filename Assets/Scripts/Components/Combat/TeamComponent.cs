using UnityEngine;
using Unity.Netcode;

public class TeamComponent : NetworkBehaviour
{
    // Network variable to synchronize the team tag across clients
    public NetworkVariable<TeamTag> PlayerTeam = new NetworkVariable<TeamTag>(
        TeamTag.Neutral, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public enum TeamTag
    {
        Friend,
        Neutral,
        Enemy
    }

    private void Start()
    {
        // Set the initial layer based on the team (for local instantiation)
        SetLayerBasedOnTeam(PlayerTeam.Value);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Set all players or NPCs based on the SO configuration
            SetTeam(PlayerTeam.Value);  // Ensure the initial team tag is set when spawning
        }
    }

    // Apply configuration from SO and update the team
    public void ApplyConfiguration(ObjectSO config)
    {
        // Set the team based on the ScriptableObject configuration
        SetTeam(config.teamTag);
    }


    public void ApplyConfiguration(PlayerSO config)
    {
        // Set the team based on the ScriptableObject configuration
        SetTeam(config.teamTag);
    }

    // Change the team and update the corresponding layer
    public void SetTeam(TeamTag team)
    {
        if (IsServer)
        {
            PlayerTeam.Value = team;
            SetLayerBasedOnTeam(team); // Update the object's layer based on its team
        }
    }

    // Set the layer of the object based on the team
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

    // Get the current team of the object
    public TeamTag GetTeam()
    {
        return PlayerTeam.Value;
    }
}
