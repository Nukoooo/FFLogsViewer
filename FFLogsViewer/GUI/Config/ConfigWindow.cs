using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace FFLogsViewer.GUI.Config;

public class ConfigWindow : Window
{
    public LayoutTab LayoutTab = new();

    public ConfigWindow()
        : base("Configuration##FFLogsViewerConfigWindow")
    {
        this.RespectCloseHotkey = true;

        this.Flags = ImGuiWindowFlags.AlwaysAutoResize;
    }

    public override void Draw()
    {
        ImGui.BeginTabBar("ConfigTabs");

        if (ImGui.BeginTabItem(Service.Localization.GetString("Misc")))
        {
            MiscTab.Draw();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem(Service.Localization.GetString("Layout")))
        {
            this.LayoutTab.Draw();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem(Service.Localization.GetString("Stats")))
        {
            StatsTab.Draw();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem(Service.Localization.GetString("Style")))
        {
            StyleTab.Draw();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Open With"))
        {
            OpenWithTab.Draw();
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }
}
