using System;
using Dalamud.Interface.Colors;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace FFLogsViewer.GUI.Config;

public class MiscTab
{
    public static void Draw()
    {
        if (ImGui.Button(Service.Localization.GetString("OpenGithubRepo")))
        {
            Util.OpenLink("https://github.com/NukoOoOoOoO/FFLogsViewer");
        }

        var hasChanged = false;

        var contextMenu = Service.Configuration.ContextMenu;
        if (ImGui.Checkbox($@"{Service.Localization.GetString("Misc_EnableContextMenu")}##ContextMenu", ref contextMenu))
        {
            if (contextMenu)
            {
                ContextMenu.Enable();
            }
            else
            {
                ContextMenu.Disable();
            }

            Service.Configuration.ContextMenu = contextMenu;
            hasChanged = true;
        }

        Util.SetHoverTooltip(Service.Localization.GetString("Misc_EnableContextMenu_Help"));

        ImGui.BeginDisabled(!Service.Configuration.ContextMenu);

        ImGui.Indent();

        ImGui.BeginDisabled(Service.Configuration.ContextMenuStreamer);

        var contextMenuButtonName = Service.Configuration.ContextMenuButtonName;
        if (ImGui.InputText("Button name##ContextMenuButtonName", ref contextMenuButtonName, 50))
        {
            Service.Configuration.ContextMenuButtonName = contextMenuButtonName;
            hasChanged = true;
        }

        var openInBrowser = Service.Configuration.OpenInBrowser;
        if (ImGui.Checkbox("Open in browser##OpenInBrowser", ref openInBrowser))
        {
            Service.Configuration.OpenInBrowser = openInBrowser;
            hasChanged = true;
        }

        Util.DrawHelp("The button in context menus opens" +
                             "\nFF Logs in your default browser instead" +
                             "\nof opening the plugin window.");

        ImGui.EndDisabled();

        ImGui.BeginDisabled(Service.Configuration.OpenInBrowser);

        var contextMenuStreamer = Service.Configuration.ContextMenuStreamer;
        if (ImGui.Checkbox("Streamer mode##ContextMenuStreamer", ref contextMenuStreamer))
        {
            Service.Configuration.ContextMenuStreamer = contextMenuStreamer;
            hasChanged = true;
        }

        Util.DrawHelp("When the main window is open, opening a context menu" +
                             "\nwill automatically search for the selected player." +
                             "\nThis mode does not add a button to the context menu.");

        ImGui.BeginDisabled(Service.Configuration.ContextMenuAlwaysPartyView);

        var contextMenuPartyView = Service.Configuration.ContextMenuPartyView;
        if (ImGui.Checkbox("Open the party view when appropriate##ContextMenuPartyView", ref contextMenuPartyView))
        {
            Service.Configuration.ContextMenuPartyView = contextMenuPartyView;
            hasChanged = true;
        }

        Util.DrawHelp("If the context menu button is used from a party list-related window," +
                      "\nopen the party view instead of the single view." +
                      "\nThis will still load the selected player's data in the single view.");

        ImGui.EndDisabled();

        var contextMenuAlwaysPartyView = Service.Configuration.ContextMenuAlwaysPartyView;
        if (ImGui.Checkbox("Always open the party view##ContextMenuAlwaysPartyView", ref contextMenuAlwaysPartyView))
        {
            Service.Configuration.ContextMenuAlwaysPartyView = contextMenuAlwaysPartyView;
            hasChanged = true;
        }

        ImGui.EndDisabled();

        ImGui.Unindent();

        ImGui.EndDisabled();

        var showTomestoneOption = Service.Configuration.ShowTomestoneOption;
        if (ImGui.Checkbox("Show Tomestone option when opening a link", ref showTomestoneOption))
        {
            Service.Configuration.ShowTomestoneOption = showTomestoneOption;
            hasChanged = true;
        }

        var isCachingEnabled = Service.Configuration.IsCachingEnabled;
        if (ImGui.Checkbox("Enable caching", ref isCachingEnabled))
        {
            Service.Configuration.IsCachingEnabled = isCachingEnabled;
            hasChanged = true;
        }

        Util.DrawHelp("Build a cache of fetched characters to avoid using too much API points (see Layout tab for more info on points).\n" +
                      "The cache is cleared every hour, you can also manually clear it in the main window.");

        ImGui.Text("API client:");

        var configurationClientId = Service.Configuration.ClientId;
        if (ImGui.InputText($"{Service.Localization.GetString("Misc_API_ClientID")}##ClientId", ref configurationClientId, 50))
        {
            Service.Configuration.ClientId = configurationClientId;
            Service.FFLogsClient.SetToken();
            hasChanged = true;
        }

        var configurationClientSecret = Service.Configuration.ClientSecret;
        if (ImGui.InputText($"{Service.Localization.GetString("Misc_API_ClientSecret")}##ClientSecret", ref configurationClientSecret, 50))
        {
            Service.Configuration.ClientSecret = configurationClientSecret;
            Service.FFLogsClient.SetToken();
            hasChanged = true;
        }

        if (Service.FFLogsClient.IsTokenValid)
        {
            ImGui.TextColored(ImGuiColors.HealerGreen, "This client is valid.");
        }
        else
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, "This client is NOT valid.");
            if (FFLogsClient.IsConfigSet())
            {
                using var color = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                ImGui.TextWrapped("If you are certain that the API client is valid, this may indicate that FF Logs is unreachable.\nMake sure you can open it in your browser before trying again.");
                color.Pop();
                if (ImGui.Button("Open FF Logs"))
                {
                    Util.OpenLink("https://www.fflogs.com/");
                }

                ImGui.SameLine();
                if (ImGui.Button("Try again"))
                {
                    Service.FFLogsClient.SetToken();
                }
            }
        }

        if (ImGui.CollapsingHeader(Service.Localization.GetString("Misc_API_Client_Tutorial")))
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Bullet();
            ImGui.Text(Service.Localization.GetString("Misc_API_Client_Tutorial_1"));
            ImGui.SameLine();
            if (ImGui.Button($"{Service.Localization.GetString("ClickHere")}##APIClientLink"))
            {
                Util.OpenLink("https://www.fflogs.com/api/clients/");
            }

            ImGui.AlignTextToFramePadding();
            ImGui.Bullet();
            ImGui.Text(Service.Localization.GetString("Misc_API_Client_Tutorial_2"));

            ImGui.AlignTextToFramePadding();
            ImGui.Bullet();
            ImGui.Text(Service.Localization.GetString("Misc_API_Client_Tutorial_3"));
            ImGui.SameLine();
            if (ImGui.Button($"{Service.Localization.GetString("Copy")}##APIClientCopyName"))
            {
                CopyToClipboard("Plugin");
            }

            ImGui.AlignTextToFramePadding();
            ImGui.Bullet();
            ImGui.Text(Service.Localization.GetString("Misc_API_Client_Tutorial_4"));
            ImGui.SameLine();
            if (ImGui.Button($"{Service.Localization.GetString("Copy")}##APIClientCopyUrl"))
            {
                CopyToClipboard("https://www.example.com");
            }

            ImGui.AlignTextToFramePadding();
            ImGui.Bullet();
            ImGui.Text(Service.Localization.GetString("Misc_API_Client_Tutorial_5"));

            ImGui.AlignTextToFramePadding();
            ImGui.Bullet();
            ImGui.Text(Service.Localization.GetString("Misc_API_Client_Tutorial_6"));
        }

        if (hasChanged)
        {
            Service.Configuration.Save();
        }
    }

    private static void CopyToClipboard(string text)
    {
        try
        {
            ImGui.SetClipboardText(text);
            Service.NotificationManager.AddNotification(new Notification { Content = $"Copied to clipboard: {text}", Type = NotificationType.Success });
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error(ex, "Could not set clipboard text.");
            Service.NotificationManager.AddNotification(new Notification { Title = "Could not copy to clipboard", Content = text, Type = NotificationType.Error, Minimized = false });
        }
    }
}
