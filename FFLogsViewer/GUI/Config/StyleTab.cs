using ImGuiNET;

namespace FFLogsViewer.GUI.Config;

public class StyleTab
{
    public static void Draw()
    {
        var style = Service.Configuration.Style;
        var hasStyleChanged = false;
        ImGui.Text(Service.Localization.GetString("Style_MainWindow"));

        ImGui.Indent();

        if (ImGui.BeginCombo("Default view", Service.Configuration.IsDefaultViewParty ? "Party view" : "Single view"))
        {
            if (ImGui.Selectable("Single view"))
            {
                Service.Configuration.IsDefaultViewParty = false;
                Service.MainWindow.IsPartyView = false;
                hasStyleChanged = true;
            }

            if (ImGui.Selectable("Party view"))
            {
                Service.Configuration.IsDefaultViewParty = true;
                Service.MainWindow.IsPartyView = true;
                hasStyleChanged = true;
            }

            ImGui.EndCombo();
        }

        Util.DrawHelp("Default view when opening the window with /fflogs if the view has not been changed yet since last plugin restart.");

        var hideInCombat = Service.Configuration.HideInCombat;
        if (ImGui.Checkbox($@"{Service.Localization.GetString("Misc_HideInCombat")}##HideInCombat", ref hideInCombat))
        {
            Service.Configuration.HideInCombat = hideInCombat;
            hasStyleChanged = true;
        }

        if (ImGui.Checkbox(Service.Localization.GetString("Style_CloseWindowWithESC"), ref style.IsCloseHotkeyRespected))
        {
            Service.MainWindow.RespectCloseHotkey = style.IsCloseHotkeyRespected;
            hasStyleChanged = true;
        }

        var tmpWindowFlags = (int)style.MainWindowFlags;
        if (ImGui.CheckboxFlags(Service.Localization.GetString("Style_NoTitleBar"), ref tmpWindowFlags, (int)ImGuiWindowFlags.NoTitleBar))
        {
            style.MainWindowFlags = (ImGuiWindowFlags)tmpWindowFlags;
            Service.MainWindow.Flags = style.MainWindowFlags;
            hasStyleChanged = true;
        }

        if (ImGui.RadioButton(Service.Localization.GetString("Style_FixedSize"), style.IsSizeFixed))
        {
            style.IsSizeFixed = true;
            style.MainWindowFlags &= ~ImGuiWindowFlags.AlwaysAutoResize;
            Service.MainWindow.Flags = style.MainWindowFlags;
            hasStyleChanged = true;
        }

        ImGui.SameLine();
        var isAutoResizeFlagSet = (style.MainWindowFlags & ImGuiWindowFlags.AlwaysAutoResize) != 0;
        if (ImGui.RadioButton(Service.Localization.GetString("Style_AutoResize"), isAutoResizeFlagSet && !style.IsSizeFixed))
        {
            style.IsSizeFixed = false;
            style.MainWindowFlags |= ImGuiWindowFlags.AlwaysAutoResize;
            Service.MainWindow.Flags = style.MainWindowFlags;
            Service.MainWindow.ResetSize();
            hasStyleChanged = true;
        }

        Util.DrawHelp(Service.Localization.GetString("Style_AutoResize_Help"));

        ImGui.SameLine();
        if (ImGui.RadioButton(Service.Localization.GetString("Style_FixedMinSize"), !isAutoResizeFlagSet && !style.IsSizeFixed))
        {
            style.IsSizeFixed = false;
            style.MainWindowFlags &= ~ImGuiWindowFlags.AlwaysAutoResize;
            Service.MainWindow.Flags = style.MainWindowFlags;
            hasStyleChanged = true;
        }

        Util.DrawHelp(Service.Localization.GetString("Style_FixedMinSize_Help"));

        if (style.IsSizeFixed)
        {
            ImGui.Indent();

            if (ImGui.CheckboxFlags(Service.Localization.GetString("Style_NoResize"), ref tmpWindowFlags, (int)ImGuiWindowFlags.NoResize))
            {
                style.MainWindowFlags = (ImGuiWindowFlags)tmpWindowFlags;
                Service.MainWindow.Flags = style.MainWindowFlags;
                hasStyleChanged = true;
            }

            ImGui.Unindent();
        }

        if (!isAutoResizeFlagSet && !style.IsSizeFixed)
        {
            ImGui.Indent();
            hasStyleChanged |= ImGui.SliderFloat(Service.Localization.GetString("Style_MinSize"), ref Service.Configuration.Style.MinMainWindowWidth, 1, 2000);

            ImGui.Unindent();
        }

        ImGui.Unindent();

        ImGui.Text(Service.Localization.GetString("Style_MainWindowTable"));

        ImGui.Indent();

        ImGui.AlignTextToFramePadding();
        ImGui.Text(Service.Localization.GetString("Misc_LogDecimalDigit"));
        for (var i = 0; i <= 2; i++)
        {
            ImGui.SameLine();
            if (ImGui.RadioButton(i + "##NbOfDecimalDigits", Service.Configuration.NbOfDecimalDigits == i))
            {
                Service.Configuration.NbOfDecimalDigits = i;
                hasStyleChanged = true;
            }
        }

        hasStyleChanged |= ImGui.Checkbox("Abbreviate job names", ref style.AbbreviateJobNames);
        hasStyleChanged |= ImGui.Checkbox("Header separator", ref style.IsHeaderSeparatorDrawn);

        var tmpTableFlags2 = (int)style.MainTableFlags;
        if (ImGui.CheckboxFlags($"{Service.Localization.GetString("Style_AlternateRowBackground")}##TableFlag", ref tmpTableFlags2, (int)ImGuiTableFlags.RowBg))
        {
            style.MainTableFlags = (ImGuiTableFlags)tmpTableFlags2;
            hasStyleChanged = true;
        }

        if (ImGui.Button(Service.Localization.GetString("Style_BordersCustomization")))
        {
            ImGui.OpenPopup("##Borders");
        }

        if (ImGui.BeginPopup("##Borders", ImGuiWindowFlags.NoMove))
        {
            var tmpTableFlags = (int)style.MainTableFlags;
            var hasChanged = false;
            hasChanged |= ImGui.CheckboxFlags($"{Service.Localization.GetString("Style_BordersCustomization_Borders")}##TableFlag", ref tmpTableFlags, (int)ImGuiTableFlags.Borders);
            hasChanged |= ImGui.CheckboxFlags($"{Service.Localization.GetString("Style_BordersCustomization_BordersH")}##TableFlag", ref tmpTableFlags, (int)ImGuiTableFlags.BordersH);
            hasChanged |= ImGui.CheckboxFlags($"{Service.Localization.GetString("Style_BordersCustomization_BordersV")}##TableFlag", ref tmpTableFlags, (int)ImGuiTableFlags.BordersV);
            hasChanged |= ImGui.CheckboxFlags($"{Service.Localization.GetString("Style_BordersCustomization_BordersInner")}##TableFlag", ref tmpTableFlags, (int)ImGuiTableFlags.BordersInner);
            hasChanged |= ImGui.CheckboxFlags($"{Service.Localization.GetString("Style_BordersCustomization_BordersOuter")}##TableFlag", ref tmpTableFlags, (int)ImGuiTableFlags.BordersOuter);
            hasChanged |= ImGui.CheckboxFlags($"{Service.Localization.GetString("Style_BordersCustomization_BordersInnerH")}##TableFlag", ref tmpTableFlags, (int)ImGuiTableFlags.BordersInnerH);
            hasChanged |= ImGui.CheckboxFlags($"{Service.Localization.GetString("Style_BordersCustomization_BordersInnerV")}##TableFlag", ref tmpTableFlags, (int)ImGuiTableFlags.BordersInnerV);
            hasChanged |= ImGui.CheckboxFlags($"{Service.Localization.GetString("Style_BordersCustomization_BordersOuterH")}##TableFlag", ref tmpTableFlags, (int)ImGuiTableFlags.BordersOuterH);
            hasChanged |= ImGui.CheckboxFlags($"{Service.Localization.GetString("Style_BordersCustomization_BordersOuterV")}##TableFlag", ref tmpTableFlags, (int)ImGuiTableFlags.BordersOuterV);
            if (hasChanged)
            {
                style.MainTableFlags = (ImGuiTableFlags)tmpTableFlags;
                hasStyleChanged = true;
            }
        }

        ImGui.Unindent();

        ImGui.Text("Party view:");

        ImGui.Indent();

        if (ImGui.Checkbox("Include yourself in the party view", ref style.IsLocalPlayerInPartyView))
        {
            Service.CharDataManager.UpdatePartyMembers();
            hasStyleChanged = true;
        }

        ImGui.Unindent();
        if (hasStyleChanged)
        {
            Service.Configuration.Save();
        }
    }
}
