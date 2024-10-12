using UnityEngine;
using Unity.Netcode;

public class TeamComponent : NetworkBehaviour
{
    // Reference to the SpriteRenderer to change color based on team
    [SerializeField] private SpriteRenderer spriteRenderer;

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
        // Set the initial layer and color based on the team (for local instantiation)
        SetLayerBasedOnTeam(PlayerTeam.Value);
        UpdateColorBasedOnTeam(PlayerTeam.Value);
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

    // Change the team and update the corresponding layer and color
    public void SetTeam(TeamTag team)
    {
        if (IsServer)
        {
            PlayerTeam.Value = team;
            SetLayerBasedOnTeam(team);  // Update the object's layer based on its team
            UpdateColorBasedOnTeam(team);  // Update the sprite color based on the team
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

    // Set the color of the SpriteRenderer based on the team
    private void UpdateColorBasedOnTeam(TeamTag team)
    {
        if (spriteRenderer == null)
        {
            Debug.LogWarning("SpriteRenderer is not assigned!");
            return;
        }

        switch (team)
        {
            case TeamTag.Friend:
                spriteRenderer.color = Color.green;
                break;
            case TeamTag.Enemy:
                spriteRenderer.color = Color.red;
                break;
            case TeamTag.Neutral:
                spriteRenderer.color = Color.white;
                break;
        }
    }

    // Get the current team of the object
    public TeamTag GetTeam()
    {
        return PlayerTeam.Value;
    }
}
