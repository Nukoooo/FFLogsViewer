using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFLogsViewer.Manager;
using FFLogsViewer.Model;
using ImGuiNET;
using Newtonsoft.Json.Linq;

namespace FFLogsViewer;

public class CharData
{
    public Metric? LoadedMetric;
    public CharacterError? CharError;
    public string FirstName = string.Empty;
    public string WorldName = string.Empty;
    public string RegionName = string.Empty;
    public string LoadedFirstName = string.Empty;
    public string LoadedWorldName = string.Empty;
    public uint JobId;
    public uint LoadedJobId;
    public volatile bool IsDataLoading;
    public volatile bool IsDataReady;

    public string Abbreviation
    {
        get
        {
            if (this.FirstName == string.Empty)
            {
                return "-";
            }

            return $"{this.FirstName[0]}";
        }
    }

    public List<Encounter> Encounters = [];

    public CharData(string? firstName = null,  string? worldName = null, uint? jobId = null)
    {
        if (firstName != null)
        {
            this.FirstName = firstName;
        }

        if (worldName != null)
        {
            this.WorldName = worldName;
        }

        if (jobId != null)
        {
            this.JobId = (uint)jobId;
        }
    }

    public void SetInfo(string firstName, string worldName)
    {
        this.FirstName = firstName;
        this.WorldName = worldName;
    }

    public bool SetInfo(IPlayerCharacter playerCharacter)
    {
        if (!playerCharacter.HomeWorld.IsValid)
        {
            this.CharError = CharacterError.GenericError;
            Service.PluginLog.Error("SetInfo character world was null");
            return false;
        }

        this.FirstName = playerCharacter.Name.TextValue;
        this.WorldName = playerCharacter.HomeWorld.Value.Name.ToString();
        return true;
    }

    public bool IsInfoSet()
    {
        return this.FirstName != string.Empty && this.WorldName != string.Empty;
    }

    public void FetchTargetChar()
    {
        var target = Service.TargetManager.Target;
        if (target is IPlayerCharacter targetCharacter && target.ObjectKind != ObjectKind.Companion)
        {
            if (this.SetInfo(targetCharacter))
            {
                this.FetchLogs();
            }
        }
        else
        {
            this.CharError = CharacterError.InvalidTarget;
        }
    }

    public void FetchLogs()
    {
        if (this.IsDataLoading)
        {
            return;
        }

        this.CharError = null;

        if (!this.IsInfoSet())
        {
            this.CharError = CharacterError.MissingInputs;
            return;
        }

        var regionName = CharDataManager.GetRegionCode(this.WorldName);
        if (regionName == string.Empty)
        {
            this.CharError = CharacterError.InvalidWorld;
            return;
        }

        this.RegionName = regionName;

        this.IsDataLoading = true;
        this.SetJobId();
        this.ResetData();
        Task.Run(async () =>
        {
            var rawData = await Service.FFLogsClient.FetchLogs(this).ConfigureAwait(false);
            if (rawData == null)
            {
                this.IsDataLoading = false;
                Service.FFLogsClient.InvalidateCache(this);
                this.CharError = CharacterError.Unreachable;
                Service.PluginLog.Error("rawData is null");
                return;
            }

            if (rawData.data?.characterData?.character == null)
            {
                this.IsDataLoading = false;

                if (rawData.error != null)
                {
                    if (rawData.error == "Unauthenticated.")
                    {
                        this.CharError = CharacterError.Unauthenticated;
                        Service.FFLogsClient.InvalidateCache(this);
                        Service.PluginLog.Information($"Unauthenticated: {rawData}");
                        return;
                    }

                    if (rawData.status != null && rawData.status == 429)
                    {
                        this.CharError = CharacterError.OutOfPoints;
                        Service.FFLogsClient.InvalidateCache(this);
                        Service.PluginLog.Information($"Ran out of points: {rawData}");
                        return;
                    }

                    this.CharError = CharacterError.GenericError;
                    Service.FFLogsClient.InvalidateCache(this);
                    Service.PluginLog.Information($"Generic error: {rawData}");
                    return;
                }

                if (rawData.errors != null)
                {
                    this.CharError = CharacterError.MalformedQuery;
                    Service.FFLogsClient.InvalidateCache(this);
                    Service.PluginLog.Information($"Malformed GraphQL query: {rawData}");
                    return;
                }

                this.CharError = CharacterError.CharacterNotFoundFFLogs;
                Service.FFLogsClient.InvalidateCache(this);
                return;
            }

            var character = rawData.data.characterData.character;

            if (character.hidden == "true")
            {
                this.IsDataLoading = false;
                this.CharError = CharacterError.HiddenLogs;
                Service.FFLogsClient.InvalidateCache(this);
                return;
            }

            this.Encounters = [];

            var properties = character.Properties();
            foreach (var prop in properties)
            {
                if (prop.Value != null && prop.Name != "hidden")
                {
                    this.ParseZone(prop.Value);
                }
            }

            this.IsDataReady = true;
            this.LoadedFirstName = this.FirstName;
            this.LoadedWorldName = this.WorldName;
            if (Service.MainWindow.Job.Name != "All jobs")
            {
                this.LoadedJobId = Service.MainWindow.Job.Name == "Current job"
                                       ? this.JobId
                                       : Service.MainWindow.Job.Id;
            }
            else
            {
                this.LoadedJobId = 0;
            }
        }).ContinueWith(t =>
        {
            Service.MainWindow.ResetSize();
            this.IsDataLoading = false;
            if (!t.IsFaulted) return;
            if (t.Exception == null) return;
            this.CharError = CharacterError.NetworkError;
            Service.FFLogsClient.InvalidateCache(this);
            foreach (var e in t.Exception.Flatten().InnerExceptions)
            {
                Service.PluginLog.Error(e, "Network error");
            }
        });
    }

    public bool ParseTextForChar(string rawText)
    {
        var placeholder = CharDataManager.FindPlaceholder(rawText);
        if (placeholder != null)
        {
            rawText = placeholder;
        }

        // TODO: 是否需要给国服更改
        /*
        rawText = rawText.Replace("'s party for", " ");
        rawText = rawText.Replace("You join", " ");
        rawText = Regex.Replace(rawText, @"\[.*?\]", " ");
        rawText = Regex.Replace(rawText, "[^A-Za-z '-]", " ");
        rawText = string.Concat(rawText.Select(x => char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ');
        rawText = Regex.Replace(rawText, @"\s+", " ");
        */
        if (!Regex.IsMatch(rawText, "@[^\x00-\x7F]{1,6}"))
            return false;

        var splitedText = rawText.Split("@");
        var firstName = splitedText[0];
        var serverName = splitedText[1];

        this.FirstName = firstName[Math.Max(0, firstName.Length - 6)..]; // Maximum name length for Chinese region is 6, same goes for Korean I think
        this.WorldName = serverName;

        return true;
    }

    public void FetchClipboardCharacter()
    {
        string clipboardRawText;
        try
        {
            clipboardRawText = ImGui.GetClipboardText();
            if (clipboardRawText == null)
            {
                this.CharError = CharacterError.ClipboardError;
                return;
            }
        }
        catch
        {
            this.CharError = CharacterError.ClipboardError;
            return;
        }

        this.FetchCharacter(clipboardRawText);
    }

    public void FetchCharacter(string text)
    {
        Service.MainWindow.IsPartyView = false;

        if (!this.ParseTextForChar(text))
        {
            this.CharError = CharacterError.CharacterNotFound;
            return;
        }

        this.FetchLogs();
    }

    public void FetchCharacter(string fullName, ushort worldId)
    {
        var world = Util.GetWorld(worldId);
        if (!Util.IsWorldValid(worldId))
        {
            Service.PluginLog.Error($"{worldId}");
            this.CharError = CharacterError.InvalidWorld;
            return;
        }

        var playerName = $"{fullName}@{world.Name}";
        this.FetchCharacter(playerName);
    }

    public void ResetData()
    {
        this.Encounters = [];
        this.IsDataReady = false;
        this.LoadedMetric = null;
    }

    private void SetJobId()
    {
        if (Service.MainWindow.IsPartyView)
        {
            return; // job id was just set from the team list
        }

        // search in the object table first as it updates faster and is always accurate
        var fullName = $"{this.FirstName}";
        for (var i = 0; i < 200; i += 2)
        {
            var obj = Service.ObjectTable[i];
            if (obj is IPlayerCharacter playerCharacter
                && playerCharacter.Name.TextValue == fullName
                && playerCharacter.HomeWorld.ValueNullable?.Name == this.WorldName)
            {
                this.JobId = playerCharacter.ClassJob.RowId;
                return;
            }
        }

        // if not in object table, search in the team list (can give 0 if normal party member in another zone)
        Service.TeamManager.UpdateTeamList();
        var member = Service.TeamManager.TeamList.FirstOrDefault(member => member.FirstName == this.FirstName
                                                              && member.World == this.WorldName);
        if (member != null)
        {
            this.JobId = member.JobId;
            return;
        }

        this.JobId = 0; // avoid stale job id if the current one is not retrievable
    }

    private void ParseZone(dynamic zone)
    {
        if (zone.rankings == null)
        {
            return;
        }

        // metric not valid for this zone
        if (zone.rankings.Count == 0)
        {
            this.Encounters.Add(new Encounter { ZoneId = zone.zone, IsValid = false });
        }

        float? bestAllStarsPointsZone = null;
        int? bestAllStarsRankZone = null;
        float? bestAllStarsRankPercentZone = null;
        if (zone.allStars != null)
        {
            foreach (var allStar in zone.allStars)
            {
                // best all stars are based on highest ASP%, JTokenType check is to protect from new logs, may not happen
                if (allStar.rank.Type != JTokenType.String
                    && allStar.rankPercent.Type != JTokenType.String
                    && (bestAllStarsRankPercentZone == null || bestAllStarsRankPercentZone < (int)allStar.rankPercent))
                {
                    bestAllStarsPointsZone = allStar.points;
                    bestAllStarsRankZone = allStar.rank;
                    bestAllStarsRankPercentZone = allStar.rankPercent;
                }
            }
        }

        foreach (var ranking in zone.rankings)
        {
            if (ranking.encounter == null)
            {
                continue;
            }

            var encounter = new Encounter
            {
                ZoneId = zone.zone,
                Id = ranking.encounter.id,
                Difficulty = zone.difficulty,
                Metric = zone.metric,
                BestAllStarsPointsZone = bestAllStarsPointsZone,
                BestAllStarsRankZone = bestAllStarsRankZone,
                BestAllStarsRankPercentZone = bestAllStarsRankPercentZone,
            };

            if (ranking.spec != null)
            {
                encounter.IsLockedIn = ranking.lockedIn;
                encounter.Best = ranking.rankPercent;
                encounter.Median = ranking.medianPercent;
                encounter.Kills = ranking.totalKills;
                encounter.Fastest = ranking.fastestKill;
                encounter.BestAmount = ranking.bestAmount;
                var jobName = Regex.Replace(ranking.spec.ToString(), "([a-z])([A-Z])", "$1 $2");
                encounter.Job = GameDataManager.Jobs.FirstOrDefault(job => job.Name == jobName);
                var bestJobName = Regex.Replace(ranking.bestSpec.ToString(), "([a-z])([A-Z])", "$1 $2");
                encounter.BestJob = GameDataManager.Jobs.FirstOrDefault(job => job.Name == bestJobName);
                var allStars = ranking.allStars;
                if (allStars != null)
                {
                    encounter.AllStarsPoints = allStars.points;

                    // both "-" if fresh log
                    if (allStars.rank.Type != JTokenType.String && allStars.rankPercent.Type != JTokenType.String)
                    {
                        encounter.AllStarsRank = allStars.rank;
                        encounter.AllStarsRankPercent = allStars.rankPercent;
                    }
                }
            }

            this.Encounters.Add(encounter);
        }
    }
}
