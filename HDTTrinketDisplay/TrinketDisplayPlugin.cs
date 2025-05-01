using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Plugins;
using System;
using System.Windows.Controls;

namespace HDTTrinketDisplay
{
    public class TrinketDisplayPlugin : IPlugin
    {
        public string Name => "HDTTrinketDisplay";

        public string Description => "Displays the current Battlegrounds trinket on your overlay";

        public string ButtonText => "SETTINGS";

        public string Author => "Mouchoir & Tignus";

        public Version Version => new Version(1, 2, 0);

        public MenuItem MenuItem => CreateMenu();

        private MenuItem CreateMenu()
        {
            MenuItem settingsMenuItem = new MenuItem { Header = "Trinket Display Settings" };

            settingsMenuItem.Click += (sender, args) =>
            {
                SettingsView.Flyout.IsOpen = true;
            };

            return settingsMenuItem;
        }
        public TrinketDisplay TrinketDisplay;

        public void OnButtonPress() => SettingsView.Flyout.IsOpen = true;

        public void OnLoad()
        {
            TrinketDisplay = new TrinketDisplay();
            GameEvents.OnGameStart.Add(TrinketDisplay.HandleGameStart);
            GameEvents.OnGameEnd.Add(TrinketDisplay.ClearCard);

            // Processing GameStart logic in case plugin was loaded/unloaded after starting a game without restarting HDT
            TrinketDisplay.HandleGameStart();
        }

        public void OnUnload()
        {
            Settings.Default.Save();
            TrinketDisplay.ClearCard();
            TrinketDisplay = null;
        }

        public void OnUpdate()
        {
        }
    }
}
