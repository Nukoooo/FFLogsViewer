using System;
using System.Linq;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Hooking;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;

namespace FFLogsViewer.Manager;

public unsafe class OpenWithManager
{
    public bool HasLoadingFailed;
    public bool HasBeenEnabled;

    private DateTime wasOpenedLast = DateTime.Now;
    private short isJoiningPartyFinderOffset;
    private nint charaCardAtkCreationAddress;
    private nint processInspectPacketAddress;
    private nint socialDetailAtkCreationAddress;
    private nint processPartyFinderDetailPacketAddress;
    private nint atkUnitBaseFinalizeAddress;

    private delegate void* CharaCardAtkCreationDelegate(nint agentCharaCard);
    private Hook<CharaCardAtkCreationDelegate>? charaCardAtkCreationHook;

    private delegate void* ProcessInspectPacketDelegate(void* someAgent, void* a2, nint packetData);
    private Hook<ProcessInspectPacketDelegate>? processInspectPacketHook;

    private delegate void* SocialDetailAtkCreationDelegate(void* someAgent, nint data, long a3, void* a4);
    private Hook<SocialDetailAtkCreationDelegate>? socialDetailAtkCreationHook;

    private delegate void* ProcessPartyFinderDetailPacketDelegate(nint something, nint packetData);
    private Hook<ProcessPartyFinderDetailPacketDelegate>? processPartyFinderDetailPacketHook;

    private delegate void AtkUnitBaseFinalizeDelegate(AtkUnitBase* addon);
    private Hook<AtkUnitBaseFinalizeDelegate>? atkUnitBaseFinalizeHook;

    public OpenWithManager()
    {
        this.ScanSigs();
        this.EnableHooks();
    }

    public void ReloadState()
    {
        this.EnableHooks();
    }

    public void Dispose()
    {
        this.charaCardAtkCreationHook?.Dispose();
        this.processInspectPacketHook?.Dispose();
        this.socialDetailAtkCreationHook?.Dispose();
        this.processPartyFinderDetailPacketHook?.Dispose();
        this.atkUnitBaseFinalizeHook?.Dispose();
    }

    private static bool IsEnabled()
    {
        if (Service.Configuration.OpenWith.Key != VirtualKey.NO_KEY)
        {
            if (Service.Configuration.OpenWith.IsDisabledWhenKeyHeld)
            {
                if (Service.KeyState[Service.Configuration.OpenWith.Key])
                {
                    return false;
                }
            }
            else
            {
                if (!Service.KeyState[Service.Configuration.OpenWith.Key])
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void Open(nint fullNamePtr, ushort worldId)
    {
        if (!IsEnabled())
        {
            return;
        }

        var world = Service.DataManager.GetExcelSheet<World>()?.FirstOrDefault(x => x.RowId == worldId);
        if (world is not { IsPublic: true })
        {
            return;
        }

        var fullName = MemoryHelper.ReadStringNullTerminated(fullNamePtr);
        if (fullName == string.Empty)
        {
            return;
        }

        if (Service.Configuration.OpenWith.ShouldIgnoreSelf
            && Service.ClientState.LocalPlayer?.Name.TextValue == fullName
            && Service.ClientState.LocalPlayer?.HomeWorld.Id == worldId)
        {
            return;
        }

        if (Service.Configuration.OpenWith.ShouldOpenMainWindow)
        {
            this.wasOpenedLast = DateTime.Now;
            Service.MainWindow.Open(false);
            Service.MainWindow.ResetSize();
        }

        if (Service.MainWindow.IsOpen)
        {
            Service.CharDataManager.DisplayedChar.FetchCharacter(fullName, worldId);
        }
    }

    private void ScanSigs()
    {
        try
        {
            this.charaCardAtkCreationAddress = Service.SigScanner.ScanText("E8 ?? ?? ?? ?? 4C 8B 74 24 ?? 48 8B 74 24 ?? 48 8B 5C 24 ?? 48 83 C4 30");
            this.processInspectPacketAddress = Service.SigScanner.ScanText("48 89 5C 24 ?? 56 41 56 41 57 48 83 EC 20 8B DA");
            this.socialDetailAtkCreationAddress = Service.SigScanner.ScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? BE");
            this.processPartyFinderDetailPacketAddress = Service.SigScanner.ScanText("E9 ?? ?? ?? ?? CC CC CC CC CC CC 48 89 5C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 55 48 8D AC 24");
            this.atkUnitBaseFinalizeAddress = Service.SigScanner.ScanText("E8 ?? ?? ?? ?? 45 33 C9 8D 57 01 41 B8");

            try
            {
                this.isJoiningPartyFinderOffset = *(short*)(Service.SigScanner.ScanModule("48 8B F1 66 C7 81") + 6);
            }
            catch (Exception ex)
            {
                Service.PluginLog.Error(ex, "isJoiningPartyFinderOffset sig scan failed.");
            }
        }
        catch (Exception ex)
        {
            this.HasLoadingFailed = true;
            Service.PluginLog.Error(ex, "OpenWith sig scan failed.");
        }
    }

    private void EnableHooks()
    {
        if (this.HasLoadingFailed || this.HasBeenEnabled || !Service.Configuration.OpenWith.IsAnyEnabled())
        {
            return;
        }

        try
        {
            this.charaCardAtkCreationHook = Service.GameInteropProvider.HookFromAddress<CharaCardAtkCreationDelegate>(this.charaCardAtkCreationAddress, this.CharaCardAtkCreationDetour);
            this.charaCardAtkCreationHook.Enable();

            this.processInspectPacketHook = Service.GameInteropProvider.HookFromAddress<ProcessInspectPacketDelegate>(this.processInspectPacketAddress, this.ProcessInspectPacketDetour);
            this.processInspectPacketHook.Enable();

            this.socialDetailAtkCreationHook = Service.GameInteropProvider.HookFromAddress<SocialDetailAtkCreationDelegate>(this.socialDetailAtkCreationAddress, this.SocialDetailAtkCreationDetour);
            this.socialDetailAtkCreationHook.Enable();

            this.processPartyFinderDetailPacketHook = Service.GameInteropProvider.HookFromAddress<ProcessPartyFinderDetailPacketDelegate>(this.processPartyFinderDetailPacketAddress, this.ProcessPartyFinderDetailPacketDetour);
            this.processPartyFinderDetailPacketHook.Enable();

            this.atkUnitBaseFinalizeHook = Service.GameInteropProvider.HookFromAddress<AtkUnitBaseFinalizeDelegate>(this.atkUnitBaseFinalizeAddress, this.AktUnitBaseFinalizeDetour);
            this.atkUnitBaseFinalizeHook.Enable();
        }
        catch (Exception ex)
        {
            this.HasLoadingFailed = true;
            Service.PluginLog.Error(ex, "OpenWith hooks enabling failed.");
        }

        this.HasBeenEnabled = true;
    }

    private void* CharaCardAtkCreationDetour(nint agentCharaCard)
    {
        try
        {
            if (Service.Configuration.OpenWith.IsAdventurerPlateEnabled
                && Service.GameGui.GetAddonByName("BannerEditor", 0) == IntPtr.Zero
                && Service.GameGui.GetAddonByName("CharaCardDesignSetting", 0) == IntPtr.Zero)
            {
                // To get offsets: 6.21 process chara card network packet 40 55 53 57 48 8D AC 24 ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 85 ?? ?? ?? ?? 48 83 79 ?? ?? 48 8B DA
                var fullNamePtr = *(nint*)(*(nint*)(agentCharaCard + 40) + 88);
                var worldId = *(ushort*)(*(nint*)(agentCharaCard + 40) + 192);

                this.Open(fullNamePtr, worldId);
            }
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error(ex, "Exception in CharaCardAtkCreationDetour.");
        }

        return this.charaCardAtkCreationHook!.Original(agentCharaCard);
    }

    private void* ProcessInspectPacketDetour(void* someAgent, void* a2, nint packetData)
    {
        try
        {
            if (Service.Configuration.OpenWith.IsExamineEnabled)
            {
                // To get offsets: 6.21 process inspect network packet 48 89 5C 24 ?? 56 41 56 41 57 48 83 EC 20 8B DA
                var fullNamePtr = packetData + 640;
                var worldId = *(ushort*)(packetData + 50);

                this.Open(fullNamePtr, worldId);
            }
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error(ex, "Exception in ProcessInspectPacketDetour.");
        }

        return this.processInspectPacketHook!.Original(someAgent, a2, packetData);
    }

    private void* SocialDetailAtkCreationDetour(void* someAgent, nint data, long a3, void* a4)
    {
        try
        {
            // a3 != 0 => editing
            if (Service.Configuration.OpenWith.IsSearchInfoEnabled && a3 == 0)
            {
                // To get offsets: look in the function
                var fullNamePtr = data + 42;
                var worldId = *(ushort*)(data + 32);

                this.Open(fullNamePtr, worldId);
            }
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error(ex, "Exception in SocialDetailAtkCreationDetour.");
        }

        return this.socialDetailAtkCreationHook!.Original(someAgent, data, a3, a4);
    }

    private void* ProcessPartyFinderDetailPacketDetour(nint something, nint packetData)
    {
        try
        {
            if (Service.Configuration.OpenWith.IsPartyFinderEnabled)
            {
                // To get offsets: 6.28, look in this function
                var hasFailed = *(byte*)(packetData + 84) == 0; // (*(byte*)(packetData + 83) & 1) == 0 for World parties (?)
                var isJoining = this.isJoiningPartyFinderOffset != 0 && *(byte*)(something + this.isJoiningPartyFinderOffset) != 0;

                if (!hasFailed && !isJoining)
                {
                    var fullName = packetData + 712;
                    var worldId = *(ushort*)(packetData + 74); // is not used in the function, just search it again if it breaks

                    this.Open(fullName, worldId);
                }
            }
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error(ex, "Exception in ProcessPartyFinderDetailPacketDetour.");
        }

        return this.processPartyFinderDetailPacketHook!.Original(something, packetData);
    }

    private void AktUnitBaseFinalizeDetour(AtkUnitBase* addon)
    {
        try
        {
            if (IsEnabled() && Service.Configuration.OpenWith.ShouldCloseMainWindow)
            {
                if ((Service.Configuration.OpenWith.IsAdventurerPlateEnabled && MemoryHelper.ReadStringNullTerminated((nint)addon->Name) == "CharaCard")
                    || (Service.Configuration.OpenWith.IsExamineEnabled && MemoryHelper.ReadStringNullTerminated((nint)addon->Name) == "CharacterInspect")
                    || (Service.Configuration.OpenWith.IsSearchInfoEnabled && MemoryHelper.ReadStringNullTerminated((nint)addon->Name) == "SocialDetailB")
                    || (Service.Configuration.OpenWith.IsPartyFinderEnabled && MemoryHelper.ReadStringNullTerminated((nint)addon->Name) == "LookingForGroupDetail"))
                {
                    // do not close the window if it was just opened, avoid issue of race condition with the addon closing
                    if (DateTime.Now - this.wasOpenedLast > TimeSpan.FromMilliseconds(100))
                    {
                        Service.MainWindow.IsOpen = false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error(ex, "Exception in AktUnitBaseFinalizeDetour.");
        }

        this.atkUnitBaseFinalizeHook!.Original(addon);
    }
}
