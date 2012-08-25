#region File Description
//-----------------------------------------------------------------------------
// Player.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Platformer
{
    /// <summary>
    /// Our fearless adventurer!
    /// </summary>
    class Player
    {

        // Ghosts
        private ReplayData ghostData;
        public ReplayData GhostData
        {
            get { return ghostData; }
        }

        private ReplayData replayData;
        public ReplayData ReplayData
        {
            get { return replayData; }
        }



        // Animations
        private Animation idleAnimation;
        private Animation runAnimation;
        private Animation jumpAnimation;
        private Animation celebrateAnimation;
        private Animation dieAnimation;

        private List<AnimationPlayer> sprites = new List<AnimationPlayer>(10);

        // Sounds
        private SoundEffect killedSound;
        private SoundEffect jumpSound;
        private SoundEffect fallSound;

        public Level Level
        {
            get { return level; }
        }
        Level level;


        public List<PlayerState> playerStates = new List<PlayerState>(10);
        public List<PlayerState> PlayerStates { get { return playerStates; } }

        public bool IsAlive { get { return playerStates[0] != PlayerState.Dead; } }

        // Physics state
        private List<Vector2> playerPositions = new List<Vector2>(10);
        public List<Vector2> PlayerPositions
        {
            get
            {
                return playerPositions;
            }
        }
        private List<Vector2> playerVelocities = new List<Vector2>(10);
        public List<Vector2> PlayerVelocities
        {
            get
            {
                return playerVelocities;
            }
        }



        private List<float> previousBottom = new List<float>(10);
        private List<bool> flips = new List<bool>(10);


        public Vector2 Velocity
        {
            get { return playerVelocities[0]; }
        }

        // Constants for controling horizontal movement
        private const float MoveAcceleration = 13000.0f;
        private const float MaxMoveSpeed = 1750.0f;
        private const float GroundDragFactor = 0.48f;
        private const float AirDragFactor = 0.58f;

        // Constants for controlling vertical movement
        private const float MaxJumpTime = 0.35f;
        private const float JumpLaunchVelocity = -3500.0f;
        private const float GravityAcceleration = 3400.0f;
        private const float MaxFallSpeed = 550.0f;
        private const float JumpControlPower = 0.14f;

        // Input configuration
        private const float MoveStickScale = 1.0f;
        private const float AccelerometerScale = 1.5f;
        private const Buttons JumpButton = Buttons.A;

        /// <summary>
        /// Gets whether or not the player's feet are on the ground.
        /// </summary>
        public bool IsOnGround
        {
            get { return isOnGround[0]; }
        }

        private List<bool> isOnGround = new List<bool>(10);
        private List<bool> wasJumping = new List<bool>(10);
        private List<float> jumpTime = new List<float>(10);

        private List<float> movement = new List<float>(10);
        private List<bool> isJumping = new List<bool>(10);


        private Rectangle localBounds;


        /// <summary>
        /// Gets a rectangle which bounds this player in world space.
        /// </summary>
        public Rectangle GetBoundingRectangle(Vector2 position)
        {
            int left = (int)Math.Round(position.X - sprites[0].Origin.X) + localBounds.X;
            int top = (int)Math.Round(position.Y - sprites[0].Origin.Y) + localBounds.Y;

            return new Rectangle(left, top, localBounds.Width, localBounds.Height);

        }

        /// <summary>
        /// Constructors a new player.
        /// </summary>

        public Player(Level level, Vector2 start, ReplayData replay)
        {


            this.replayData = new ReplayData();

            int positionsToCreate = (replay == null ? 0 : replay.StreamCount);

            this.ghostData = replay;
            for (int i = 0; i <= positionsToCreate; i++)
            {
                this.sprites.Add(new AnimationPlayer());
                this.playerPositions.Add(Vector2.Zero);
                this.playerVelocities.Add(Vector2.Zero);
                this.isOnGround.Add(true);
                this.previousBottom.Add(0.0f);
                this.jumpTime.Add(0.0f);
                this.isJumping.Add(false);
                this.wasJumping.Add(false);
                this.movement.Add(0.0f);
                this.flips.Add(true);
                this.playerStates.Add(PlayerState.Alive);
            }

            this.level = level;

            LoadContent();

            Reset(null, start);
        }



        /// <summary>
        /// Loads the player sprite sheet and sounds.
        /// </summary>
        public void LoadContent()
        {
            // Load animated textures.
            idleAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Idle"), 0.1f, true);
            runAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Run"), 0.1f, true);
            jumpAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Jump"), 0.1f, false);
            celebrateAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Celebrate"), 0.1f, false);
            dieAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Die"), 0.1f, false);

            // Calculate bounds within texture size.            
            int width = (int)(idleAnimation.FrameWidth * 0.4);
            int left = (idleAnimation.FrameWidth - width) / 2;
            int height = (int)(idleAnimation.FrameWidth * 0.8);
            int top = idleAnimation.FrameHeight - height;
            localBounds = new Rectangle(left, top, width, height);

            // Load sounds.            
            killedSound = Level.Content.Load<SoundEffect>("Sounds/PlayerKilled");
            jumpSound = Level.Content.Load<SoundEffect>("Sounds/PlayerJump");
            fallSound = Level.Content.Load<SoundEffect>("Sounds/PlayerFall");
        }

        /// <summary>
        /// Resets the player to life.
        /// </summary>
        /// <param name="position">The position to come to life at.</param>
        public void Reset(GameTime gameTime, Vector2 position)
        {
            if (gameTime != null)
                this.replayData.NewPlayerInputStream(gameTime.TotalGameTime - level.TotalTimeLevelStarted);
            else
                this.replayData.NewPlayerInputStream(TimeSpan.Zero);

            for (int i = 0; i < playerPositions.Count; i++)
                playerPositions[i] = position;
            for (int i = 0; i < playerVelocities.Count; i++)
                playerVelocities[i] = Vector2.Zero;

            for (int i = 0; i < sprites.Count; i++)
            {
                playerStates[i] = PlayerState.Alive;
                playAnimationForSprite(i, idleAnimation);
            }
        }

        private void playAnimationForSprite(int i, Animation animation)
        {
            AnimationPlayer player = sprites[i];
            player.PlayAnimation(animation);
            sprites[i] = player;
        }

        /// <summary>
        /// Handles input, performs physics, and animates the player sprite.
        /// </summary>
        /// <remarks>
        /// We pass in all of the input states so that our game is only polling the hardware
        /// once per frame. We also pass the game's orientation because when using the accelerometer,
        /// we need to reverse our motion when the orientation is in the LandscapeRight orientation.
        /// </remarks>
        public virtual void Update(
            GameTime gameTime,
            KeyboardState keyboardState,
            GamePadState gamePadState,
            DisplayOrientation orientation)
        {

            GetInput(gameTime, keyboardState, gamePadState, orientation);

            for (int i = 0; i < isOnGround.Count; i++)
            {
                if (playerStates[i] == PlayerState.Alive && isOnGround[i])
                {
                    if (Math.Abs(playerVelocities[i].X) - 0.02f > 0)
                    {
                        playAnimationForSprite(i, runAnimation);
                    }
                    else
                    {
                        playAnimationForSprite(i, idleAnimation);
                    }
                }
            }

            ApplyPhysics(gameTime);

            // Clear input.
            for (int i = 0; i < movement.Count; i++)
                movement[i] = 0.0f;
            for (int i = 0; i < isJumping.Count; i++)
                isJumping[i] = false;
        }

        /// <summary>
        /// Gets player horizontal movement and jump commands from input.
        /// </summary>
        protected virtual void GetInput(
            GameTime gameTime,
            KeyboardState keyboardState,
            GamePadState gamePadState,
            DisplayOrientation orientation)
        {

            // Get analog horizontal movement.
            movement[0] = gamePadState.ThumbSticks.Left.X * MoveStickScale;

            // Ignore small movements to prevent running in place.
            if (Math.Abs(movement[0]) < 0.5f)
                movement[0] = 0.0f;

            // If any digital horizontal movement input is found, override the analog movement.
            if (gamePadState.IsButtonDown(Buttons.DPadLeft) ||
                keyboardState.IsKeyDown(Keys.Left) ||
                keyboardState.IsKeyDown(Keys.A))
            {
                movement[0] = -1.0f;
            }
            else if (gamePadState.IsButtonDown(Buttons.DPadRight) ||
                     keyboardState.IsKeyDown(Keys.Right) ||
                     keyboardState.IsKeyDown(Keys.D))
            {
                movement[0] = 1.0f;
            }

            // Check if the player wants to jump.
            isJumping[0] =
                gamePadState.IsButtonDown(JumpButton) ||
                keyboardState.IsKeyDown(Keys.Space) ||
                keyboardState.IsKeyDown(Keys.Up) ||
                keyboardState.IsKeyDown(Keys.W);


            replayData.RecordPlayerInput(gameTime.TotalGameTime - level.TotalTimeLevelStarted, movement[0], isJumping[0]);

            if (ghostData != null)
            {
                List<PlayerGhostData> ghostInput = ghostData.GetNextPlayerInputs(gameTime.TotalGameTime - level.TotalTimeLevelStarted);
                for (int i = 0; i < ghostInput.Count; i++)
                {
                    if (playerStates[i + 1] == PlayerState.Alive)
                    {
                        movement[i + 1] = ghostInput[i].Mouvement;
                        isJumping[i + 1] = ghostInput[i].IsJumping;
                    }
                }
            }

        }

        /// <summary>
        /// Updates the player's velocity and position based on input, gravity, etc.
        /// </summary>
        public void ApplyPhysics(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            for (int i = 0; i < playerPositions.Count; i++)
            {
                if (playerStates[i] != PlayerState.Alive)
                    continue;

                Vector2 previousPosition = playerPositions[i];

                // Base velocity is a combination of horizontal movement control and
                // acceleration downward due to gravity.
                playerVelocities[i] = new Vector2(playerVelocities[i].X + movement[i] * MoveAcceleration * elapsed,
                        MathHelper.Clamp(playerVelocities[i].Y + GravityAcceleration * elapsed, -MaxFallSpeed, MaxFallSpeed));


                playerVelocities[i] = new Vector2(playerVelocities[i].X, DoJump(playerVelocities[i].Y, gameTime, i));

                // Apply pseudo-drag horizontally.
                if (isOnGround[i])
                    playerVelocities[i] = new Vector2(playerVelocities[i].X * GroundDragFactor, playerVelocities[i].Y);
                else
                    playerVelocities[i] = new Vector2(playerVelocities[i].X * AirDragFactor, playerVelocities[i].Y);

                // Prevent the player from running faster than his top speed.            
                playerVelocities[i] = new Vector2(MathHelper.Clamp(playerVelocities[i].X, -MaxMoveSpeed, MaxMoveSpeed), playerVelocities[i].Y);


                // Apply velocity.
                playerPositions[i] = playerPositions[i] + playerVelocities[i] * elapsed;
                playerPositions[i] = new Vector2((float)Math.Round(playerPositions[i].X), (float)Math.Round(playerPositions[i].Y));

                // If the player is now colliding with the level, separate them.
                HandleCollisions(i);

                // If the collision stopped us from moving, reset the velocity to zero.
                if (playerPositions[i].X == previousPosition.X)
                    playerVelocities[i] = new Vector2(0, playerVelocities[i].Y);

                if (playerPositions[i].Y == previousPosition.Y)
                    playerVelocities[i] = new Vector2(playerVelocities[i].X, 0);
            }
        }

        /// <summary>
        /// Calculates the Y velocity accounting for jumping and
        /// animates accordingly.
        /// </summary>
        /// <remarks>
        /// During the accent of a jump, the Y velocity is completely
        /// overridden by a power curve. During the decent, gravity takes
        /// over. The jump velocity is controlled by the jumpTime field
        /// which measures time into the accent of the current jump.
        /// </remarks>
        /// <param name="velocityY">
        /// The player's current velocity along the Y axis.
        /// </param>
        /// <returns>
        /// A new Y velocity if beginning or continuing a jump.
        /// Otherwise, the existing Y velocity.
        /// </returns>
        private float DoJump(float velocityY, GameTime gameTime, int playerIndex)
        {
            // If the player wants to jump
            if (isJumping[playerIndex])
            {
                // Begin or continue a jump
                if ((!wasJumping[playerIndex] && isOnGround[playerIndex]) || jumpTime[playerIndex] > 0.0f)
                {
                    if (playerIndex == 0 && jumpTime[playerIndex] == 0.0f)
                        jumpSound.Play();

                    jumpTime[playerIndex] += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    playAnimationForSprite(playerIndex, jumpAnimation);
                }

                // If we are in the ascent of the jump
                if (0.0f < jumpTime[playerIndex] && jumpTime[playerIndex] <= MaxJumpTime)
                {
                    // Fully override the vertical velocity with a power curve that gives players more control over the top of the jump
                    velocityY = JumpLaunchVelocity * (1.0f - (float)Math.Pow(jumpTime[playerIndex] / MaxJumpTime, JumpControlPower));
                }
                else
                {
                    // Reached the apex of the jump
                    jumpTime[playerIndex] = 0.0f;
                }
            }
            else
            {
                // Continues not jumping or cancels a jump in progress
                jumpTime[playerIndex] = 0.0f;
            }
            wasJumping[playerIndex] = isJumping[playerIndex];

            return velocityY;
        }

        /// <summary>
        /// Detects and resolves all collisions between the player and his neighboring
        /// tiles. When a collision is detected, the player is pushed away along one
        /// axis to prevent overlapping. There is some special logic for the Y axis to
        /// handle platforms which behave differently depending on direction of movement.
        /// </summary>
        private void HandleCollisions(int i)
        {
            // Get the player's bounding rectangle and find neighboring tiles.
            Rectangle bounds = GetBoundingRectangle(playerPositions[i]);
            int leftTile = (int)Math.Floor((float)bounds.Left / Tile.Width);
            int rightTile = (int)Math.Ceiling(((float)bounds.Right / Tile.Width)) - 1;
            int topTile = (int)Math.Floor((float)bounds.Top / Tile.Height);
            int bottomTile = (int)Math.Ceiling(((float)bounds.Bottom / Tile.Height)) - 1;

            // Reset flag to search for ground collision.
            isOnGround[i] = false;


            // For each potentially colliding tile,
            for (int y = topTile; y <= bottomTile; ++y)
            {
                for (int x = leftTile; x <= rightTile; ++x)
                {
                    // If this tile is collidable,
                    TileCollision collision = Level.GetCollision(x, y);
                    if (collision != TileCollision.Passable)
                    {
                        // Determine collision depth (with direction) and magnitude.
                        Rectangle tileBounds = Level.GetBounds(x, y);
                        Vector2 depth = RectangleExtensions.GetIntersectionDepth(bounds, tileBounds);
                        if (depth != Vector2.Zero)
                        {
                            float absDepthX = Math.Abs(depth.X);
                            float absDepthY = Math.Abs(depth.Y);

                            // Resolve the collision along the shallow axis.
                            if (absDepthY < absDepthX || collision == TileCollision.Platform)
                            {
                                // If we crossed the top of a tile, we are on the ground.
                                if (previousBottom[i] <= tileBounds.Top)
                                {
                                    isOnGround[i] = true;
                                }

                                // Ignore platforms, unless we are on the ground.
                                if (collision == TileCollision.Impassable || isOnGround[i])
                                {
                                    // Resolve the collision along the Y axis.
                                    playerPositions[i] = new Vector2(playerPositions[i].X, playerPositions[i].Y + depth.Y);

                                    // Perform further collisions with the new bounds.
                                    bounds = GetBoundingRectangle(playerPositions[i]);
                                }
                            }
                            else if (collision == TileCollision.Impassable) // Ignore platforms.
                            {
                                // Resolve the collision along the X axis.
                                playerPositions[i] = new Vector2(playerPositions[i].X + depth.X, playerPositions[i].Y);

                                // Perform further collisions with the new bounds.
                                bounds = GetBoundingRectangle(playerPositions[i]);
                            }
                        }
                    }
                }
            }

            // Save the new bounds bottom.
            previousBottom[i] = bounds.Bottom;
        }

        /// <summary>
        /// Called when the player has been killed.
        /// </summary>
        /// <param name="killedBy">
        /// The enemy who killed the player. This parameter is null if the player was
        /// not killed by an enemy (fell into a hole).
        /// </param>
        public void OnKilled(int playerIndex, Enemy killedBy)
        {
            playerStates[playerIndex] = PlayerState.Dead;
            if (playerIndex == 0)
            {
                if (killedBy != null)
                    killedSound.Play();
                else
                    fallSound.Play();
            }

            playAnimationForSprite(playerIndex, dieAnimation);
        }

        /// <summary>
        /// Called when this player reaches the level's exit.
        /// </summary>
        public void OnReachedExit(int playerIndex)
        {
            playAnimationForSprite(playerIndex, celebrateAnimation);
            playerStates[playerIndex] = PlayerState.ReachedExit;
        }

        /// <summary>
        /// Draws the animated player.
        /// </summary>
        public virtual void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Draw player + ghosts
            for (int i = 0; i < PlayerPositions.Count; i++)
            {
                // Flip the sprite to face the way we are moving.
                if (PlayerVelocities[i].X > 0)
                    flips[i] = true;
                else if (PlayerVelocities[i].X < 0)
                    flips[i] = false;

                // because animation player is a struct, which is copied by value
                // when access in a collection, we have to do this or draw will 
                // modify the value without updating the one in the collection

                AnimationPlayer sprite = sprites[i];
                sprite.Draw(gameTime, spriteBatch, playerPositions[i], flips[i] ? SpriteEffects.FlipHorizontally : SpriteEffects.None, i > 0);
                sprites[i] = sprite;

            }


        }
    }
}
