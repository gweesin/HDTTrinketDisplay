using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static HearthDb.Enums.GameTag;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.Hearthstone.Entities;
using Hearthstone_Deck_Tracker.Utility.Logging;
using Hearthstone_Deck_Tracker.API;
using System.Windows.Controls;
using Hearthstone_Deck_Tracker.Controls;
using System.Windows.Media;
using Hearthstone_Deck_Tracker.Utility.Assets;

namespace HDTTrinketDisplay
{
    public class TrinketDisplay
    {
        public CardImage CardImage;
        public static MoveCardManager MoveManager;
        private IEnumerable<Entity> lastTrinkets = new List<Entity>();

        public TrinketDisplay()
        {
        }

        public async Task AwaitGameEntity()
        {
            const int maxAttempts = 40;
            const int delayBetweenAttempts = 250;
            const int gameFadeInDelay = 1000;

            // Loop until enough heroes are loaded
            for (var i = 0; i < maxAttempts; i++)
            {
                await Task.Delay(delayBetweenAttempts);

                var loadedHeroes = Core.Game.Player.PlayerEntities
                    .Where(x => x.IsHero && (x.HasTag(BACON_HERO_CAN_BE_DRAFTED) || x.HasTag(BACON_SKIN)));

                if (loadedHeroes.Count() >= 2)
                {
                    await Task.Delay(gameFadeInDelay);
                    break;
                }
            }
        }

        public void InitializeView(List<int> cardDbfIds)
        {
            foreach (var cardDbfId in cardDbfIds)
            {
                // Do not recreate card if it already exists via a double call to HandleGameStart() (cf OnLoad)
                if (CardImage == null)
                {
                    CardImage = new CardImage();

                    Core.OverlayCanvas.Children.Add(CardImage);
                    Canvas.SetTop(CardImage, Settings.Default.TrinketCardTop);
                    Canvas.SetLeft(CardImage, Settings.Default.TrinketCardLeft);
                    CardImage.Visibility = System.Windows.Visibility.Visible;

                    MoveManager = new MoveCardManager(CardImage, SettingsView.IsUnlocked);
                    Settings.Default.PropertyChanged += SettingsChanged;
                    SettingsChanged(null, null);
                }

                var cardFromDbfId = Database.GetCardFromDbfId(cardDbfId, false);
                CardImage.SetCardIdFromCard(cardFromDbfId);
            }
        }

        // On scaling change update the card
        private void SettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            CardImage.RenderTransform = new ScaleTransform(Settings.Default.TrinketCardScale / 100,
                Settings.Default.TrinketCardScale / 100);
            Canvas.SetTop(CardImage, Settings.Default.TrinketCardTop);
            Canvas.SetLeft(CardImage, Settings.Default.TrinketCardLeft);
        }

        public async Task IntervalLoop()
        {
            const int delayBetweenAttempts = 1000;

            // Loop until enough heroes are loaded
            while (true)
            {
                await Task.Delay(delayBetweenAttempts);

                DisplayTrinkets();
            }
        }

        private void DisplayTrinkets()
        {
            var currentTrinkets = Core.Game.Player.Trinkets;

            if (currentTrinkets.Count() != lastTrinkets.Count() ||
                !currentTrinkets.Select(t => t.Card.DbfId).SequenceEqual(lastTrinkets.Select(t => t.Card.DbfId)))
            {
                var trinketDbfIds = currentTrinkets.Select(t => t.Card.DbfId).ToList();
                Log.Info($"Trinket found: {currentTrinkets}");
                InitializeView(trinketDbfIds);
            }

            if (!currentTrinkets.Any())
            {
                Log.Warn("No trinket DbfId found whereas game is already started !");
            }

            lastTrinkets = currentTrinkets.ToList();
        }

        public async void HandleGameStart()
        {
            if (Core.Game.CurrentGameMode != GameMode.Battlegrounds)
                return;

            await AwaitGameEntity();

            Entity gameEntity = Core.Game.GameEntity;
            if (gameEntity == null)
                return;

            await IntervalLoop();
        }

        public void ClearCard()
        {
            CardImage.SetCardIdFromCard(null);
            Core.OverlayCanvas.Children.Remove(CardImage);
            CardImage = null;

            Log.Info("Destroying the MoveManager...");
            MoveManager.Dispose();
            MoveManager = null;

            Settings.Default.PropertyChanged -= SettingsChanged;
        }
    }
}