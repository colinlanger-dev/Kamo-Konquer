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
        if (!IsServer)
            return;

        CurrentTeam.Value = startingTeam != Team.None
            ? startingTeam
            : GetTeamForClient(OwnerClientId);
    }

    public void SetTeamOnServer(Team team)
    {
        if (IsServer)
            CurrentTeam.Value = team;
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
