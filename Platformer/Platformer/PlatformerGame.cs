#region File Description
//-----------------------------------------------------------------------------
// PlatformerGame.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Input.Touch;
using System.ComponentModel;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;


namespace Platformer
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class PlatformerGame : Microsoft.Xna.Framework.Game
    {
        private const String LEVEL_URL = "http://paxdevdemo.com/levels.json";
        private const String USER_NAME = "juliene@amazon.com";

        // Resources for drawing.
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        // Global content.
        private SpriteFont hudFont;

        private Texture2D winOverlay;
        private Texture2D loseOverlay;
        private Texture2D diedOverlay;
        private Texture2D loadingOverlay;
        private Texture2D loadingBackground;

        // Meta-level game state.
        private List<LevelData> levelsData;
        private int levelIndex = -1;
        private Level currentLevel;
        private bool wasContinuePressed;
        
        private bool isLoading = true;
        public String LoadingStatus { get; set; }
        private BackgroundWorker loadingWorker;

        // When the time remaining is less than the warning time, it blinks on the hud
        private static readonly TimeSpan WarningTime = TimeSpan.FromSeconds(5);

        // We store our input states so that we only poll once per frame, 
        // then we use the same input state wherever needed
        private GamePadState gamePadState;
        private KeyboardState keyboardState;

        public PlatformerGame()
        {
            LoadingStatus = "";
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            loadingWorker = new BackgroundWorker();
            loadingWorker.DoWork += new DoWorkEventHandler(loadingWorker_DoWork);
            loadingWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(loadingWorker_RunWorkerCompleted);
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load fonts
            hudFont = Content.Load<SpriteFont>("Fonts/Hud");

            // Load overlay textures
            winOverlay = Content.Load<Texture2D>("Overlays/you_win");
            loseOverlay = Content.Load<Texture2D>("Overlays/you_lose");
            diedOverlay = Content.Load<Texture2D>("Overlays/you_died");
            loadingOverlay = Content.Load<Texture2D>("Overlays/loading");
            loadingBackground = Content.Load<Texture2D>("Backgrounds/Layer0_0");

            //Known issue that you get exceptions if you use Media PLayer while connected to your PC
            //See http://social.msdn.microsoft.com/Forums/en/windowsphone7series/thread/c8a243d2-d360-46b1-96bd-62b1ef268c66
            //Which means its impossible to test this from VS.
            //So we have to catch the exception and throw it away
            try
            {
                MediaPlayer.IsRepeating = true;
                MediaPlayer.Play(Content.Load<Song>("Sounds/Music"));
            }
            catch { }

            // start a network request to get all the levels in the background
            loadingWorker.RunWorkerAsync();
        }

        
        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            
            // Handle polling for our input and handling high-level input
            HandleInput(gameTime);

            if (!isLoading)
            {
                // update our level, passing down the GameTime along with all of our input states
                currentLevel.Update(gameTime, keyboardState, gamePadState, Window.CurrentOrientation);
            }
            base.Update(gameTime);
        }

        private void loadingWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(LEVEL_URL);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            // Get the stream associated with the response.
            Stream receiveStream = response.GetResponseStream();

            // Pipes the stream to a higher level stream reader with the required encoding format. 
            StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);
            JArray array = (JArray)JArray.ReadFrom(new JsonTextReader(readStream));

            levelsData = LevelData.ParseJson(array);

            LoadingStatus = "Received "  + levelsData.Count + " levels";
            
            response.Close();
            readStream.Close();
            Thread.Sleep(2000);
        }

        private void loadingWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            LoadNextLevel();
            isLoading = false;
        }



        private void HandleInput(GameTime gameTime)
        {
            // get all of our input states
            keyboardState = Keyboard.GetState();
            gamePadState = GamePad.GetState(PlayerIndex.One);
            

            // Exit the game when back is pressed.
            if (gamePadState.Buttons.Back == ButtonState.Pressed || keyboardState.IsKeyDown(Keys.Escape))
                Exit();

            bool continuePressed =
                keyboardState.IsKeyDown(Keys.Space) ||
                gamePadState.IsButtonDown(Buttons.A);
            bool saveReplayPressed = keyboardState.IsKeyDown(Keys.Y) ||
                gamePadState.IsButtonDown(Buttons.Y);



            // Perform the appropriate action to advance the game and
            // to get the player back to playing.
            if (!isLoading && !wasContinuePressed && continuePressed)
            {
                if (!currentLevel.Player.IsAlive)
                {
                    currentLevel.StartNewLife(gameTime);
                }
                else if (currentLevel.TimeRemaining == TimeSpan.Zero)
                {
                    if (currentLevel.ReachedExit) {
                        if(currentLevel.Player.GhostData == null || currentLevel.Score > currentLevel.Player.GhostData.HighScore) {
                            currentLevel.Player.ReplayData.SaveRecordedData(currentLevel.LevelData.Id);
                        }
                        LoadNextLevel();
                    } else {
                        ReloadCurrentLevel();
                    }
                }
            }
            wasContinuePressed = continuePressed;
        }

        private void LoadNextLevel()
        {
            // move to the next level
            levelIndex = (levelIndex + 1) % levelsData.Count;

            // Unloads the content for the current level before loading the next one.
            if (currentLevel != null)
                currentLevel.Dispose();

            // Load the level.
            currentLevel = new Level(Services, levelsData[levelIndex]);
        }

        private void ReloadCurrentLevel()
        {
            LoadNextLevel();
        }

        /// <summary>
        /// Draws the game from background to foreground.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(new Color(107,200,207));
            
            spriteBatch.Begin();
            if (!isLoading)
            {
                currentLevel.Draw(GraphicsDevice, gameTime, spriteBatch);
                DrawHud();
            }
            else
            {
                DrawLoadingScreen();
            }
            
            spriteBatch.End();
            base.Draw(gameTime);
        }

        
        private void DrawLoadingScreen()
        {
            spriteBatch.Draw(loadingBackground, Vector2.Zero, Color.White);
            Rectangle titleSafeArea = GraphicsDevice.Viewport.TitleSafeArea;
            Vector2 center = new Vector2(titleSafeArea.X + titleSafeArea.Width / 2.0f,
                                         titleSafeArea.Y + titleSafeArea.Height / 2.0f);

            Vector2 statusSize = new Vector2(loadingOverlay.Width, loadingOverlay.Height);
            spriteBatch.Draw(loadingOverlay, center - statusSize / 2, Color.White);

            DrawShadowedString(hudFont, LoadingStatus, 
                center + new Vector2(-hudFont.MeasureString(LoadingStatus).X/2, statusSize.Y/2 - 45),
                Color.Yellow);
        }

        private void DrawHud()
        {
            Rectangle titleSafeArea = GraphicsDevice.Viewport.TitleSafeArea;
            Vector2 hudLocation = new Vector2(titleSafeArea.X, titleSafeArea.Y);
            Vector2 center = new Vector2(titleSafeArea.X + titleSafeArea.Width / 2.0f,
                                         titleSafeArea.Y + titleSafeArea.Height / 2.0f);

            // Draw time remaining. Uses modulo division to cause blinking when the
            // player is running out of time.
            string timeString = "TIME: " + currentLevel.TimeRemaining.Minutes.ToString("00") + ":" + currentLevel.TimeRemaining.Seconds.ToString("00");
            Color timeColor;
            if (currentLevel.TimeRemaining > WarningTime ||
                currentLevel.ReachedExit ||
                (int)currentLevel.TimeRemaining.TotalSeconds % 2 == 0)
            {
                timeColor = Color.Yellow;
            }
            else
            {
                timeColor = Color.Red;
            }
            DrawShadowedString(hudFont, timeString, hudLocation, timeColor);

            // Draw score
            float timeHeight = hudFont.MeasureString(timeString).Y;
            String scoreString = "SCORE: " + currentLevel.Score.ToString();
            if (currentLevel.Player.GhostData != null)
                scoreString += " / " + currentLevel.Player.GhostData.HighScore.ToString();
            DrawShadowedString(hudFont, scoreString, hudLocation + new Vector2(0.0f, timeHeight * 1.2f), Color.Yellow);
           
            // Determine the status overlay message to show.
            Texture2D status = null;
            if (!currentLevel.IsLoading && currentLevel.TimeRemaining == TimeSpan.Zero)
            {
                if (currentLevel.ReachedExit)
                {
                    status = winOverlay;
                }
                else
                {
                    status = loseOverlay;
                }
            }
            else if (!currentLevel.Player.IsAlive)
            {
                status = diedOverlay;
            }

            if (status != null)
            {
                // Draw status message.
                Vector2 statusSize = new Vector2(status.Width, status.Height);
                spriteBatch.Draw(status, center - statusSize / 2, Color.White);
            }
        }

        private void DrawShadowedString(SpriteFont font, string value, Vector2 position, Color color)
        {
            spriteBatch.DrawString(font, value, position + new Vector2(1.0f, 1.0f), Color.Black);
            spriteBatch.DrawString(font, value, position, color);
        }
    }
}
