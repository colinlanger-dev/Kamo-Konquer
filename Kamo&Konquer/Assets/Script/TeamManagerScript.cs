using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TeamManagerScript : NetworkBehaviour
{
    public enum Team : byte
    {
        None,
        Azad,
        Kamo
    }

    public static Team LocalTeam { get; set; } = Team.None;

    private static readonly Dictionary<ulong, Team> ClientTeams = new();

    [SerializeField] private Team startingTeam = Team.None;

    public readonly NetworkVariable<Team> CurrentTeam = new(
        Team.None,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        CurrentTeam.OnValueChanged += OnCurrentTeamChanged;

        if (IsServer)
        {
            CurrentTeam.Value = startingTeam != Team.None
                ? startingTeam
                : GetTeamForClient(OwnerClientId);
        }

        ApplyTeamLayer(CurrentTeam.Value);
    }

    public override void OnNetworkDespawn()
    {
        CurrentTeam.OnValueChanged -= OnCurrentTeamChanged;
    }

    public void SetTeamOnServer(Team team)
    {
        if (IsServer)
            CurrentTeam.Value = team;
    }

    public void SetStartingTeam(Team team)
    {
        startingTeam = team;
        if (IsServer && IsSpawned)
            CurrentTeam.Value = team;
        ApplyTeamLayer(gameObject, team);
    }

    private void OnCurrentTeamChanged(Team previousTeam, Team newTeam)
    {
        ApplyTeamLayer(newTeam);
    }

    private void ApplyTeamLayer(Team team)
    {
        ApplyTeamLayer(gameObject, team);
    }

    public static void ApplyTeamLayer(GameObject target, Team team)
    {
        if (target == null)
            return;

        int teamLayer = UnitSelectionManager.GetLayerForTeam(team);
        if (teamLayer >= 0)
            SetLayerRecursively(target.transform, teamLayer);
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        foreach (Transform child in root)
            SetLayerRecursively(child, layer);
    }

    public static void SetTeamForClient(ulong clientId, Team team)
    {
        if (team == Team.None)
            ClientTeams.Remove(clientId);
        else
            ClientTeams[clientId] = team;
    }

    public static Team GetTeamForClient(ulong clientId)
    {
        return ClientTeams.TryGetValue(clientId, out Team team) ? team : Team.None;
    }

    public static void RemoveClientTeam(ulong clientId) => ClientTeams.Remove(clientId);

    public static TeamManagerScript FindInParents(Transform child)
    {
        return child != null ? child.GetComponentInParent<TeamManagerScript>() : null;
    }
}
