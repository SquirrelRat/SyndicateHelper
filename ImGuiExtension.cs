using System.Collections.Generic;
using ImGuiNET;

namespace SyndicateHelper
{
    public static class ImGuiExtension
    {
        /// <summary>
        /// Creates a dropdown combobox for selecting from a list of options
        /// </summary>
        public static string ComboBox(string label, string currentSelectedItem, List<string> objectList, out bool didChange,
            ImGuiComboFlags comboFlags = ImGuiComboFlags.HeightRegular)
        {
            if (ImGui.BeginCombo(label, currentSelectedItem, comboFlags))
            {
                foreach (var obj in objectList)
                {
                    var isSelected = currentSelectedItem == obj;

                    if (ImGui.Selectable(obj, isSelected))
                    {
                        didChange = true;
                        ImGui.EndCombo();
                        return obj;
                    }

                    if (isSelected) ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            didChange = false;
            return currentSelectedItem;
        }
    }
}
