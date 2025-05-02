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
        public List<CardImage> CardImages = new List<CardImage>();
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
            for (int i = 0; i < cardDbfIds.Count; i++)
            {
                if (i < CardImages.Count)
                {
                    // update CardImage with existing instance
                    var cardImage = CardImages[i];
                    var cardFromDbfId = Database.GetCardFromDbfId(cardDbfIds[i], false);
                    cardImage.SetCardIdFromCard(cardFromDbfId);
                    cardImage.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    // create new CardImage instance
                    var cardImage = new CardImage();
                    Core.OverlayCanvas.Children.Add(cardImage);
                    Canvas.SetTop(cardImage, Settings.Default.TrinketCardTop);
                    Canvas.SetLeft(cardImage, Settings.Default.TrinketCardLeft);
                    cardImage.Visibility = System.Windows.Visibility.Visible;

                    cardImage.RenderTransform = new ScaleTransform(Settings.Default.TrinketCardScale / 100,
                        Settings.Default.TrinketCardScale / 100);

                    var cardFromDbfId = Database.GetCardFromDbfId(cardDbfIds[i], false);
                    cardImage.SetCardIdFromCard(cardFromDbfId);

                    CardImages.Add(cardImage);
                }
            }

            // hide any remaining CardImages
            for (int i = cardDbfIds.Count; i < CardImages.Count; i++)
            {
                CardImages[i].Visibility = System.Windows.Visibility.Hidden;
            }

            if (MoveManager == null)
            {
                MoveManager = new MoveCardManager(CardImages.FirstOrDefault(), SettingsView.IsUnlocked);
                Settings.Default.PropertyChanged += SettingsChanged;
                SettingsChanged(null, null);
            }
        }

        // On scaling change update the card
        private void SettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var offset = 0;
            foreach (var cardImage in CardImages)
            {
                cardImage.RenderTransform = new ScaleTransform(Settings.Default.TrinketCardScale / 100,
                    Settings.Default.TrinketCardScale / 100);
                Canvas.SetTop(cardImage, Settings.Default.TrinketCardTop + offset);
                Canvas.SetLeft(cardImage, Settings.Default.TrinketCardLeft);

                offset += 100;
            }
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

            // if (!currentTrinkets.Any())
            // {
            //     Log.Warn("No trinket DbfId found whereas game is already started !");
            // }

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
            foreach (var cardImage in CardImages)
            {
                cardImage.SetCardIdFromCard(null);
                Core.OverlayCanvas.Children.Remove(cardImage);
            }

            CardImages.Clear();

            if (MoveManager != null)
            {
                Log.Info("Destroying the MoveManager...");
                MoveManager.Dispose();
                MoveManager = null;
            }

            Settings.Default.PropertyChanged -= SettingsChanged;
        }
    }
}