using System.Collections;
using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using InnerNet;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using Reactor.Utilities;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Game;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Options;
using TownOfUs.Options.Roles.Neutral;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Roles.Other;
using TownOfUs.Utilities;
using TouMiraRolesExtension.Modifiers;
using TouMiraRolesExtension.Networking;
using TouMiraRolesExtension.Options.Roles.Neutral;
using UnityEngine;
using Random = System.Random;
using TownOfUs.Extensions;
using TownOfUs.Roles.Neutral;
using TownOfUs.Roles;
using TownOfUs.Modules.Wiki;
using TownOfUs.Modules.Localization;
using TownOfUs;
using TownOfUs.Assets;
using MiraAPI.GameEnd;
using TouMiraRolesExtension.GameOver;
using TownOfUs.GameOver;

namespace TouMiraRolesExtension.Roles.Neutral;

public sealed class LawyerRole(IntPtr cppPtr) : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable,
    IAssignableTargets, ICrewVariant
{
    public PlayerControl? Client { get; set; }
    public bool ClientVoted { get; set; }
    public bool AboutToWin { get; set; }

    [HideFromIl2Cpp] public List<byte> Voters { get; set; } = [];

    public int Priority { get; set; } = 2;

    public void AssignTargets()
    {
        if (!OptionGroupSingleton<RoleOptions>.Instance.IsClassicRoleAssignment)
        {
            return;
        }

        var lawyers = PlayerControl.AllPlayerControls.ToArray()
            .Where(x => x.IsRole<LawyerRole>() && !x.HasDied());

        foreach (var lawyer in lawyers)
        {
            var killers = PlayerControl.AllPlayerControls.ToArray()
                .Where(x => !x.IsRole<LawyerRole>() && !x.HasDied() &&
                            (x.IsImpostorAligned() || x.Is(RoleAlignment.NeutralKilling)) &&
                            !x.HasModifier<ExecutionerTargetModifier>() &&
                            !x.HasModifier<GuardianAngelTargetModifier>() &&
                            !x.HasModifier<AllianceGameModifier>() &&
                            !SpectatorRole.TrackedSpectators.Contains(x.Data.PlayerName))
                .ToList();

            if (killers.Count > 0)
            {
                Random rnd = new();
                var shuffled = killers.OrderBy(x => rnd.Next()).ToList();
                var randomTarget = shuffled[0];

                RpcSetLawyerClient(lawyer, randomTarget);
            }
            else
            {
                lawyer.GetRole<LawyerRole>()!.CheckClientDeath(null);
            }
        }
    }

    public RoleBehaviour CrewVariant => RoleManager.Instance.GetRole((RoleTypes)RoleId.Get<SnitchRole>());
    public DoomableType DoomHintType => DoomableType.Trickster;
    public string LocaleKey => "Lawyer";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
    public string RoleDescription => ClientString(true);
    public string RoleLongDescription => ClientString();

    public string GetAdvancedDescription()
    {
        return
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") +
            MiscUtils.AppendOptionsText(GetType());
    }

    private string ClientString(bool capitalize = false)
    {
        string desc;
        if (Client != null)
        {
            desc = capitalize 
                ? TouLocale.GetParsed("ExtensionRoleLawyerIntroBlurb")
                : TouLocale.GetParsed("ExtensionRoleLawyerTabDescription");
            desc = desc.Replace("%client%", Client.Data.PlayerName);
        }
        else
        {
            desc = TouLocale.GetParsed("ExtensionRoleLawyerIntroBlurb");
        }

        return capitalize ? desc.ToTitleCase() : desc;
    }

    public Color RoleColor => TownOfUsColors.Lawyer;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralEvil;

    public bool SetupIntroTeam(IntroCutscene instance,
        ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
    {
        if (Player != PlayerControl.LocalPlayer)
        {
            return true;
        }

        var lawyerTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();

        lawyerTeam.Add(PlayerControl.LocalPlayer);
        if (Client != null)
        {
            lawyerTeam.Add(Client);
        }

        yourTeam = lawyerTeam;

        return true;
    }

    public CustomRoleConfiguration Configuration => new(this)
    {
        IntroSound = TouAudio.DiscoveredSound,
        Icon = TouRoleIcons.Lawyer,
        GhostRole = (RoleTypes)RoleId.Get<NeutralGhostRole>()
    };

    public bool MetWinCon => !ClientVoted && Client != null && !Client.HasDied();

    public bool WinConditionMet()
    {
        if (Player.HasDied())
        {
            return false;
        }

        return !ClientVoted && Client != null && !Client.HasDied();
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);

        if (Client == null)
        {
            Client = ModifierUtils
                .GetPlayersWithModifier<LawyerTargetModifier>([HideFromIl2Cpp](x) => x.OwnerId == Player.PlayerId)
                .FirstOrDefault();
        }

        if (Client != null)
        {
            var lawyerRole = RoleManager.Instance.GetRole((RoleTypes)RoleId.Get<LawyerRole>());
            if (!Player.HasModifier<LawyerRevealModifier>())
            {
                Player.AddModifier<LawyerRevealModifier>(lawyerRole);
            }
            
            if (!Client.HasModifier<ClientRevealModifier>())
            {
                var clientRole = Client.Data?.Role as RoleBehaviour;
                if (clientRole != null)
                {
                    Client.AddModifier<ClientRevealModifier>(clientRole);
                }
            }
        }

        if (TutorialManager.InstanceExists && Client == null &&
            AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started && Player.AmOwner &&
            Player.IsHost())
        {
            Coroutines.Start(SetTutorialTargets(this));
        }
    }

    private static IEnumerator SetTutorialTargets(LawyerRole lawyer)
    {
        yield return new WaitForSeconds(0.01f);
        lawyer.AssignTargets();
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        if (TutorialManager.InstanceExists && Player.AmOwner)
        {
            var players = ModifierUtils
                .GetPlayersWithModifier<LawyerTargetModifier>([HideFromIl2Cpp](x) => x.OwnerId == Player.PlayerId)
                .ToList();
            players.Do(x => x.RpcRemoveModifier<LawyerTargetModifier>());
        }

        if (!Player.HasModifier<BasicGhostModifier>() && ClientVoted)
        {
            Player.AddModifier<BasicGhostModifier>();
        }
    }

    public override void OnDeath(DeathReason reason)
    {
        RoleBehaviourStubs.OnDeath(this, reason);

        Client = null;
    }

    public override bool CanUse(IUsable usable)
    {
        if (!GameManager.Instance.LogicUsables.CanUse(usable, Player))
        {
            return false;
        }

        var console = usable.TryCast<Console>()!;
        return console == null || console.AllowImpostor;
    }

    public override bool DidWin(GameOverReason gameOverReason)
    {
        if (Client == null || Client.HasDied())
        {
            return false;
        }
        
        if (ClientVoted)
        {
            return false;
        }
        
        if (gameOverReason == CustomGameOver.GameOverReason<LawyerGameOver>())
        {
            return true;
        }
        
        if (gameOverReason is GameOverReason.CrewmatesByVote or GameOverReason.CrewmatesByTask
            or GameOverReason.ImpostorDisconnect or GameOverReason.HideAndSeek_CrewmatesByTimer)
        {
            return false;
        }
        
        if (Client.IsImpostorAligned())
        {
            return gameOverReason is GameOverReason.ImpostorsByKill or GameOverReason.ImpostorsBySabotage
                or GameOverReason.ImpostorsByVote or GameOverReason.CrewmateDisconnect 
                or GameOverReason.HideAndSeek_ImpostorsByKills;
        }
        
        var clientRole = Client.Data?.Role;
        if (clientRole is ICustomRole customRole && customRole.Team == ModdedRoleTeams.Custom)
        {
            if (gameOverReason == CustomGameOver.GameOverReason<NeutralGameOver>())
            {
                return false;
            }
            
            if (gameOverReason is GameOverReason.ImpostorsByKill or GameOverReason.ImpostorsBySabotage
                or GameOverReason.ImpostorsByVote or GameOverReason.CrewmateDisconnect 
                or GameOverReason.HideAndSeek_ImpostorsByKills)
            {
                return false;
            }
        }
        
        return false;
    }

    public void CheckClientDeath(PlayerControl? victim)
    {
        if (Player.HasDied() || AboutToWin || ClientVoted)
        {
            return;
        }

        if (Client == null || victim == Client)
        {
            var dieOnClientDeath = OptionGroupSingleton<LawyerOptions>.Instance.DieOnClientDeath;
            if (dieOnClientDeath)
            {
                Player.MurderPlayer(Player, MurderResultFlags.Succeeded | MurderResultFlags.DecisionByHost);
                return;
            }

            var roleType = ((BecomeOptions)OptionGroupSingleton<LawyerOptions>.Instance.OnClientDeath.Value) switch
            {
                BecomeOptions.Crew => (ushort)RoleTypes.Crewmate,
                BecomeOptions.Jester => RoleId.Get<JesterRole>(),
                BecomeOptions.Survivor => RoleId.Get<SurvivorRole>(),
                BecomeOptions.Amnesiac => RoleId.Get<AmnesiacRole>(),
                BecomeOptions.Mercenary => RoleId.Get<MercenaryRole>(),
                _ => (ushort)RoleTypes.Crewmate
            };

            Player.ChangeRole(roleType);

            if ((roleType == RoleId.Get<JesterRole>() && OptionGroupSingleton<JesterOptions>.Instance.ScatterOn) ||
                (roleType == RoleId.Get<SurvivorRole>() && OptionGroupSingleton<SurvivorOptions>.Instance.ScatterOn))
            {
                StartCoroutine(Effects.Lerp(0.2f,
                    new Action<float>(p => { Player.GetModifier<ScatterModifier>()?.OnRoundStart(); })));
            }
        }
    }

    [MethodRpc((uint)ExtensionRpc.SetLawyerClient)]
    public static void RpcSetLawyerClient(PlayerControl player, PlayerControl client)
    {
        if (player.Data.Role is not LawyerRole)
        {
            Error("RpcSetLawyerClient - Invalid lawyer");
            return;
        }

        if (client == null)
        {
            return;
        }

        var role = player.GetRole<LawyerRole>();

        if (role == null)
        {
            return;
        }

        role.Client = client;

        client.AddModifier<LawyerTargetModifier>(player.PlayerId);
        
        var lawyerRole = RoleManager.Instance.GetRole((RoleTypes)RoleId.Get<LawyerRole>());
        if (!player.HasModifier<LawyerRevealModifier>())
        {
            player.AddModifier<LawyerRevealModifier>(lawyerRole);
        }
        
        if (!client.HasModifier<ClientRevealModifier>())
        {
            var clientRole = client.Data?.Role as RoleBehaviour;
            if (clientRole != null)
            {
                client.AddModifier<ClientRevealModifier>(clientRole);
            }
        }
    }
}