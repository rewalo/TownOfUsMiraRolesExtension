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
using Reactor.Networking.Rpc;
using Reactor.Utilities;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Game;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Events;
using TownOfUs.Options;
using TownOfUs.Options.Roles.Neutral;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Roles.Other;
using TownOfUs.Utilities;
using TouMiraRolesExtension.Modifiers;
using TouMiraRolesExtension.Networking;
using TouMiraRolesExtension.Options.Roles.Neutral;
using TouMiraRolesExtension.Utilities;
using TouMiraRolesExtension.Assets;
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
using MiraAPI.Networking;
using TownOfUs.Modules;
using Reactor.Utilities.Extensions;

namespace TouMiraRolesExtension.Roles.Neutral;

public sealed class LawyerRole(IntPtr cppPtr) : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable,
    IAssignableTargets, ICrewVariant
{
    public PlayerControl? Client { get; set; }
    public bool ClientVoted { get; set; }
    public bool AboutToWin { get; set; }

    [HideFromIl2Cpp] public List<byte> Voters { get; set; } = [];
    [HideFromIl2Cpp] public int ObjectionsUsed { get; set; }
    [HideFromIl2Cpp] public int ObjectionsUsedThisMeeting { get; set; }
    [HideFromIl2Cpp] public bool HasObjected { get; set; }
    [HideFromIl2Cpp] public List<byte> ObjectedVoters { get; set; } = [];

    private MeetingMenu? meetingMenu;

    public int Priority { get; set; } = 2;

    public void HideObjectionButtons()
    {
        meetingMenu?.HideButtons();
    }

    public void AssignTargets()
    {
        if (!OptionGroupSingleton<RoleOptions>.Instance.IsClassicRoleAssignment)
        {
            return;
        }

        var lawyers = PlayerControl.AllPlayerControls.ToArray()
            .Where(x => x.IsRole<LawyerRole>() && !x.HasDied())
            .ToList();

        var assignedClients = new HashSet<byte>();

        var lawyerOptions = OptionGroupSingleton<LawyerOptions>.Instance;
        var killerChance = (int)lawyerOptions.KillerClientChance;
        Random rnd = new();
        var chance = rnd.Next(1, 101);

        foreach (var lawyer in lawyers)
        {
            PlayerControl? target = null;

            if (chance <= killerChance)
            {
                var killers = PlayerControl.AllPlayerControls.ToArray()
                    .Where(x => !x.IsRole<LawyerRole>() && !x.HasDied() &&
                                (x.IsImpostorAligned() || x.Is(RoleAlignment.NeutralKilling)) &&
                                !x.HasModifier<ExecutionerTargetModifier>() &&
                                !x.HasModifier<GuardianAngelTargetModifier>() &&
                                !x.HasModifier<AllianceGameModifier>() &&
                                !SpectatorRole.TrackedSpectators.Contains(x.Data.PlayerName) &&
                                !assignedClients.Contains(x.PlayerId)).ToList();

                if (killers.Count > 0)
                {
                    var shuffled = killers.OrderBy(x => rnd.Next()).ToList();
                    target = shuffled[0];
                }
            }

            if (target == null)
            {
                var allPlayers = PlayerControl.AllPlayerControls.ToArray()
                    .Where(x => !x.IsRole<LawyerRole>() && !x.HasDied() &&
                                !x.HasModifier<ExecutionerTargetModifier>() &&
                                !x.HasModifier<GuardianAngelTargetModifier>() &&
                                !x.HasModifier<AllianceGameModifier>() &&
                                !SpectatorRole.TrackedSpectators.Contains(x.Data.PlayerName) &&
                                !assignedClients.Contains(x.PlayerId)).ToList();

                if (allPlayers.Count > 0)
                {
                    var shuffled = allPlayers.OrderBy(x => rnd.Next()).ToList();
                    target = shuffled[0];
                }
            }

            if (target != null)
            {
                assignedClients.Add(target.PlayerId);
                RpcSetLawyerClient(lawyer, target);
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

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities
    {
        get
        {
            var maxObjections = (int)OptionGroupSingleton<LawyerOptions>.Instance.MaxObjections;
            if (maxObjections <= 0)
            {
                return [];
            }

            return new List<CustomButtonWikiDescription>
            {
                new(TouLocale.Get("ExtensionRoleLawyerObject", "Object"),
                    TouLocale.Get("ExtensionRoleLawyerObjectWikiDescription"),
                    TouExtensionAssets.ObjectionButtonSprite)
            };
        }
    }

    private string ClientString(bool capitalize = false)
    {
        string desc;
        if (Client != null)
        {
            desc = capitalize
                ? TouLocale.GetParsed("ExtensionRoleLawyerIntroBlurb")
                : TouLocale.GetParsed("ExtensionRoleLawyerTabDescription");
            desc = desc.Replace("<client>", Client.Data.PlayerName);
        }
        else
        {
            desc = TouLocale.GetParsed("ExtensionRoleLawyerIntroBlurb");
        }

        return capitalize ? desc.ToTitleCase() : desc;
    }

    public Color RoleColor => TownOfUsColors.Lawyer;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralBenign;

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

    public bool MetWinCon => Client != null && !Client.HasDied();

    public bool WinConditionMet()
    {
        // IMPORTANT:
        // This method is used by TownOfUs' NeutralRoleWinCondition to decide whether the game should end NOW.
        // Lawyer should NOT end the game just because their client is alive; Lawyer "steals" another win.
        // We therefore latch win state via AboutToWin, which is set right before triggering LawyerGameOver.
        if (Player.HasDied() || !AboutToWin)
        {
            return false;
        }

        return Client != null && !Client.HasDied();
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);

        if (Client == null)
        {
            Client = LawyerUtils.GetClientForLawyer(Player);
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

        if (Player.AmOwner)
        {
            var maxObjections = (int)OptionGroupSingleton<LawyerOptions>.Instance.MaxObjections;
            if (maxObjections > 0)
            {
                meetingMenu = new MeetingMenu(this, OnObjectClick, MeetingAbilityType.Click,
                    TouExtensionAssets.ObjectionButtonSprite, TouExtensionAssets.ObjectionButtonSprite, IsExemptForObjection)
                {
                    Position = Vector3.zero
                };
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
            var client = LawyerUtils.GetClientForLawyer(Player);
            if (client != null)
            {
                client.RpcRemoveModifier<LawyerTargetModifier>();
            }
        }

        if (!Player.HasModifier<BasicGhostModifier>() && ClientVoted)
        {
            Player.AddModifier<BasicGhostModifier>();
        }

        if (Player.AmOwner)
        {
            meetingMenu?.Dispose();
            meetingMenu = null;
        }
    }

    public override void OnMeetingStart()
    {
        RoleBehaviourStubs.OnMeetingStart(this);
        ObjectionsUsedThisMeeting = 0; HasObjected = false;
        ObjectedVoters.Clear();

        if (Player.AmOwner && meetingMenu != null && Client != null)
        {
            var maxObjectionsPerMeeting = (int)OptionGroupSingleton<LawyerOptions>.Instance.MaxObjectionsPerMeeting;
            var maxObjections = (int)OptionGroupSingleton<LawyerOptions>.Instance.MaxObjections;

            if (maxObjectionsPerMeeting > 0 || maxObjections > 0)
            {
                meetingMenu.GenButtons(MeetingHud.Instance,
                    Player.AmOwner && !Player.HasDied() && Client != null && !Client.HasDied());

                Coroutines.Start(ScaleObjectionButton());
                Coroutines.Start(UpdateObjectionButton());
            }
        }
    }

    private IEnumerator ScaleObjectionButton()
    {
        yield return new WaitForSeconds(0.1f);

        if (meetingMenu == null || Client == null || Client.HasDied())
        {
            yield break;
        }

        var meeting = MeetingHud.Instance;
        if (meeting == null)
        {
            yield break;
        }

        var voteArea = meeting.playerStates.FirstOrDefault(pva => pva.TargetPlayerId == Client.PlayerId);
        if (voteArea == null || voteArea.NameText == null)
        {
            yield break;
        }

        if (meetingMenu.Buttons.TryGetValue(Client.PlayerId, out var button) && button != null)
        {
            voteArea.NameText.ForceMeshUpdate();

            float textWidth = 0f;
            if (voteArea.NameText.textBounds.size.x > 0)
            {
                textWidth = voteArea.NameText.textBounds.size.x / 2f;
            }
            else if (voteArea.NameText.preferredWidth > 0)
            {
                textWidth = voteArea.NameText.preferredWidth / 2f;
            }

            var nameTextLocalPos = voteArea.NameText.transform.localPosition;
            button.transform.localPosition = new Vector3(nameTextLocalPos.x + textWidth + 0.15f, nameTextLocalPos.y, -1f);
            button.transform.localScale = new Vector3(0.07f, 0.07f, 1f);
        }
    }

    private IEnumerator UpdateObjectionButton()
    {
        while (MeetingHud.Instance != null)
        {
            yield return new WaitForSeconds(0.1f);

            if (meetingMenu == null || Client == null || Client.HasDied())
            {
                continue;
            }

            var meeting = MeetingHud.Instance;
            var maxObjections = (int)OptionGroupSingleton<LawyerOptions>.Instance.MaxObjections;
            var maxObjectionsPerMeeting = (int)OptionGroupSingleton<LawyerOptions>.Instance.MaxObjectionsPerMeeting;

            if (maxObjections <= 0 && maxObjectionsPerMeeting <= 0)
            {
                if (meetingMenu.Buttons.TryGetValue(Client.PlayerId, out var button) && button != null)
                {
                    meetingMenu.HideSingle(Client.PlayerId);
                }
                continue;
            }

            bool objectionsExhausted = false;
            if (maxObjections > 0 && ObjectionsUsed >= maxObjections)
            {
                objectionsExhausted = true;
            }
            if (maxObjectionsPerMeeting > 0 && ObjectionsUsedThisMeeting >= maxObjectionsPerMeeting)
            {
                objectionsExhausted = true;
            }

            bool hideButton = objectionsExhausted ||
                meeting.state == MeetingHud.VoteStates.Proceeding ||
                meeting.state == MeetingHud.VoteStates.Results;

            // Hide button in the last 20 seconds of the meeting
            var discussionTime = GameOptionsManager.Instance.currentNormalGameOptions.DiscussionTime;
            if (meeting.discussionTimer > discussionTime - 20f)
            {
                hideButton = true;
            }

            if (hideButton)
            {
                if (meetingMenu.Buttons.TryGetValue(Client.PlayerId, out var button) && button != null)
                {
                    meetingMenu.HideSingle(Client.PlayerId);
                }
            }
            else if (meeting.state == MeetingHud.VoteStates.Voted ||
                     meeting.state == MeetingHud.VoteStates.NotVoted)
            {
                var voteArea = meeting.playerStates.FirstOrDefault(pva => pva.TargetPlayerId == Client.PlayerId);
                if (voteArea != null && voteArea.NameText != null &&
                    meetingMenu.Buttons.TryGetValue(Client.PlayerId, out var button) && button != null)
                {
                    voteArea.NameText.ForceMeshUpdate();

                    float textWidth = 0f;
                    if (voteArea.NameText.textBounds.size.x > 0)
                    {
                        textWidth = voteArea.NameText.textBounds.size.x / 2f;
                    }
                    else if (voteArea.NameText.preferredWidth > 0)
                    {
                        textWidth = voteArea.NameText.preferredWidth / 2f;
                    }

                    var nameTextLocalPos = voteArea.NameText.transform.localPosition;
                    button.transform.localPosition = new Vector3(nameTextLocalPos.x + textWidth + 0.15f, nameTextLocalPos.y, -1f);
                }
            }
        }
    }

    public override void OnVotingComplete()
    {
        RoleBehaviourStubs.OnVotingComplete(this);

        if (Player.AmOwner)
        {
            meetingMenu?.HideButtons();
        }
    }

    private static bool IsExempt(PlayerVoteArea voteArea)
    {
        var player = GameData.Instance.GetPlayerById(voteArea.TargetPlayerId)?.Object;
        return !player || !player?.Data || player!.Data.Disconnected || player.Data.IsDead;
    }

    private bool IsExemptForObjection(PlayerVoteArea voteArea)
    {
        if (Client == null || Client.HasDied() || voteArea.TargetPlayerId != Client.PlayerId)
        {
            return true;
        }

        return IsExempt(voteArea);
    }

    private void OnObjectClick(PlayerVoteArea voteArea, MeetingHud meeting)
    {
        if (meeting.state != MeetingHud.VoteStates.Voted && meeting.state != MeetingHud.VoteStates.NotVoted)
        {
            return;
        }

        if (IsExemptForObjection(voteArea))
        {
            return;
        }

        if (Client == null || Client.HasDied() || Player.HasDied())
        {
            return;
        }

        var maxObjections = (int)OptionGroupSingleton<LawyerOptions>.Instance.MaxObjections;
        var maxObjectionsPerMeeting = (int)OptionGroupSingleton<LawyerOptions>.Instance.MaxObjectionsPerMeeting;

        if (maxObjections > 0 && ObjectionsUsed >= maxObjections)
        {
            return;
        }
        if (maxObjectionsPerMeeting > 0 && ObjectionsUsedThisMeeting >= maxObjectionsPerMeeting)
        {
            return;
        }

        if (voteArea.TargetPlayerId != Client.PlayerId)
        {
            return;
        }

        var hasAnyVotes = meeting.playerStates.Any(pva =>
    pva.VotedFor != 255 && !pva.AmDead);
        if (!hasAnyVotes)
        {
            return;
        }

        // Prevent objection in the last 20 seconds of the meeting
        var discussionTime = GameOptionsManager.Instance.currentNormalGameOptions.DiscussionTime;
        if (meeting.discussionTimer > discussionTime - 20f)
        {
            return;
        }

        RpcObjectVotes(Player);
    }

    [MethodRpc((uint)ExtensionRpc.LawyerObject)]
    public static void RpcObjectVotes(PlayerControl lawyer)
    {
        var lawyerRole = lawyer.GetRole<LawyerRole>();
        if (lawyerRole == null || lawyerRole.Client == null || lawyerRole.Client.HasDied())
        {
            return;
        }

        var maxObjections = (int)OptionGroupSingleton<LawyerOptions>.Instance.MaxObjections;
        if (lawyerRole.ObjectionsUsed >= maxObjections)
        {
            return;
        }

        lawyerRole.ObjectionsUsed++; lawyerRole.ObjectionsUsedThisMeeting++; lawyerRole.HasObjected = true;

        TouAudio.PlaySound(TouExtensionAudio.ObjectionSound);

        var lawyerName = lawyer.Data.PlayerName;
        var title = $"<color=#{TownOfUsColors.Lawyer.ToHtmlStringRGBA()}>{TouLocale.Get("ExtensionRoleLawyer")}</color>";
        var message = TouLocale.Get("ExtensionLawyerObjectionNotification")
            .Replace("<lawyer>", lawyerName);

        MiscUtils.AddFakeChat(lawyer.Data, title, message, false, true);

        var meeting = MeetingHud.Instance;
        if (meeting == null)
        {
            return;
        }

        foreach (var voteArea in meeting.playerStates)
        {
            if (voteArea.VotedFor != 255 && !voteArea.AmDead)
            {
                var voter = MiscUtils.PlayerById(voteArea.TargetPlayerId);
                if (voter == null)
                {
                    continue;
                }

                voteArea.UnsetVote();

                var voteData = voter.GetVoteData();
                var removedCount = voteData.Votes.Count;
                voteData.Votes.Clear();
                voteData.VotesRemaining += removedCount;

                if (!lawyerRole.ObjectedVoters.Contains(voteArea.TargetPlayerId))
                {
                    lawyerRole.ObjectedVoters.Add(voteArea.TargetPlayerId);
                }

                if (voter.AmOwner)
                {
                    meeting.ClearVote();
                }
            }
        }

        if (AmongUsClient.Instance.AmHost)
        {
            meeting.SetDirtyBit(1U);
        }

        var maxObjectionsPerMeeting = (int)OptionGroupSingleton<LawyerOptions>.Instance.MaxObjectionsPerMeeting;
        bool objectionsExhausted = false;
        if (maxObjections > 0 && lawyerRole.ObjectionsUsed >= maxObjections)
        {
            objectionsExhausted = true;
        }
        if (maxObjectionsPerMeeting > 0 && lawyerRole.ObjectionsUsedThisMeeting >= maxObjectionsPerMeeting)
        {
            objectionsExhausted = true;
        }

        if (objectionsExhausted && lawyerRole.meetingMenu != null && lawyerRole.Client != null)
        {
            lawyerRole.meetingMenu.HideSingle(lawyerRole.Client.PlayerId);
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
                var showAnim = MeetingHud.Instance == null && ExileController.Instance == null;
                var murderResultFlags = MurderResultFlags.Succeeded | MurderResultFlags.DecisionByHost;

                DeathHandlerModifier.UpdateDeathHandlerImmediate(Player,
                    TouLocale.Get("ExtensionLawyerDiedClientDeath"),
                    DeathEventHandlers.CurrentRound,
                    (!MeetingHud.Instance && !ExileController.Instance)
                        ? DeathHandlerOverride.SetTrue
                        : DeathHandlerOverride.SetFalse,
                    lockInfo: DeathHandlerOverride.SetTrue);

                Player.CustomMurder(
                    Player,
                    murderResultFlags,
                    false,
                    showAnim,
                    false,
                    showAnim,
                    false);
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

        var existingModifiers = client.GetModifiers<LawyerTargetModifier>().ToList();
        foreach (var modifier in existingModifiers)
        {
            client.RpcRemoveModifier<LawyerTargetModifier>();

            var previousLawyer = PlayerControl.AllPlayerControls.ToArray()
    .FirstOrDefault(p => p != null && p.PlayerId == modifier.OwnerId && p.IsRole<LawyerRole>());
            if (previousLawyer != null)
            {
                var previousLawyerRole = previousLawyer.GetRole<LawyerRole>();
                if (previousLawyerRole != null && previousLawyerRole.Client?.PlayerId == client.PlayerId)
                {
                    previousLawyerRole.Client = null;
                }
            }
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