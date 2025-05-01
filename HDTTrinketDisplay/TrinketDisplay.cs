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
using ControlzEx.Standard;

namespace HDTTrinketDisplay
{
    public class TrinketDisplay
    {
        public CardImage CardImage;
        public static MoveCardManager MoveManager;

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

        public void InitializeView(int cardDbfId)
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

            CardImage.SetCardIdFromCard(Database.GetCardFromDbfId(cardDbfId, false));
        }

        // On scaling change update the card
        private void SettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            CardImage.RenderTransform = new ScaleTransform(Settings.Default.TrinketCardScale / 100, Settings.Default.TrinketCardScale / 100);
            Canvas.SetTop(CardImage, Settings.Default.TrinketCardTop);
            Canvas.SetLeft(CardImage, Settings.Default.TrinketCardLeft);
        }

        public async void HandleGameStart()
        {
            if (Core.Game.CurrentGameMode != GameMode.Battlegrounds)
                return;

            await AwaitGameEntity();

            Entity gameEntity = Core.Game.GameEntity;
            if (gameEntity == null)
                return;

            int? trinketDbfId = BattlegroundsUtils.GetBattlegroundsAnomalyDbfId(gameEntity);

            if (trinketDbfId.HasValue)
            {
                Log.Info("Trinket DbfId found: " + trinketDbfId.Value);
                InitializeView(trinketDbfId.Value);
            }
            else
            {
                Log.Warn("No trinket DbfId found whereas game is already started !");
            }
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
