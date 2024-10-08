﻿using System;
using Claw.Modules;

namespace Claw.Physics
{
    /// <summary>
    /// Uma classe com física básica de plataforma.
    /// </summary>
    public class PlatBody : BaseModule, IStep
    {
		#region Módulo
		public int StepOrder
		{
			get => _stepOrder;
			set
			{
				if (_stepOrder != value) StepOrderChanged?.Invoke(this);

				_stepOrder = value;
			}
		}
		private int _stepOrder;

		public event Action<IStep> StepOrderChanged;
		#endregion

		#region Propriedades e configurações de física
		public static float CoyoteTime = .1f, CornerTolerance = 4;
        public static string TileCollisionLayer = "col";

        private float previousBottom, coyoteCounter = 0;
        private Vector2 speed = Vector2.Zero;

        public bool Grounded { get; protected set; } = false;
        public bool OnGhost { get; protected set; } = false;
        public bool WasGrounded { get; private set; } = false;
        public bool CanJump { get; private set; } = false;
        public bool IgnoreGhost { get; protected set; } = false;
        public bool ApplyPhysics { get; protected set; } = true;

        public virtual float GroundSpeed { get; } = 6;
        public virtual float AirSpeed { get; } = 8;
        public float WalkSpeed => Grounded && speed.Y >= 0 ? GroundSpeed : AirSpeed;
        public virtual float JumpSpeed { get; } = 12;
        public virtual float Gravity { get; } = 24;
        public virtual Vector2 MinSpeed { get; } = new Vector2(-24);
        public virtual Vector2 MaxSpeed { get; } = new Vector2(24);

        private Vector2 Grid => Game.Tilemap.GridSize;

        public virtual Rectangle Bounds => new Rectangle(Transform.Position, Grid);
		#endregion

        public PlatBody(bool instantlyAdd = true) : base(instantlyAdd) { }

		public override void Initialize() { }

		/// <summary>
		/// Retorna a velocidade do <see cref="PlatBody"/>.
		/// </summary>
		public Vector2 GetSpeed() => speed;
        /// <summary>
        /// Acrescenta um valor na velocidade do <see cref="PlatBody"/>.
        /// </summary>
        public void Impulse(Vector2 impulse, bool resetXSpeed = false, bool resetYSpeed = false)
        {
            if (resetXSpeed) speed.X = 0;

            if (resetYSpeed) speed.Y = 0;
            
            speed = Vector2.Clamp(speed + impulse, MinSpeed * Grid, MaxSpeed * Grid);
        }
        /// <summary>
        /// Zera a velocidade do <see cref="PlatBody"/>.
        /// </summary>
        public void Stop() => speed = Vector2.Zero;
        /// <summary>
        /// Zera a velocidade do <see cref="PlatBody"/>.
        /// </summary>
        public void Stop(bool horizontal, bool vertical)
        {
            if (horizontal) speed.X = 0;

            if (vertical) speed.Y = 0;
        }

        /// <summary>
        /// Faz com que o <see cref="PlatBody"/> pule.
        /// </summary>
        /// <param name="boost">Multiplica a força do pulo</param>
        public void Jump(float boost = 1)
        {
            Impulse(new Vector2(0, -JumpSpeed * boost * Grid.Y), false, true);

            coyoteCounter = 0;
            CanJump = false;
            Grounded = false;
            IgnoreGhost = false;
        }
        /// <summary>
        /// Faz com que o <see cref="PlatBody"/> ande.
        /// </summary>
        public void Walk(float direction) => Impulse(new Vector2(Math.Sign(direction) * WalkSpeed * Grid.X, 0), true, false);

        public virtual void Step()
        {
            if (ApplyPhysics)
            {
                WasGrounded = Grounded;

                GetInput();
                HorizontalMovement();
                VerticalMovement();
            }
        }

        /// <summary>
        /// É chamado antes de fazer as checagens de colisão.
        /// </summary>
        protected virtual void GetInput() { }

        /// <summary>
        /// É chamado sempre que ocorre uma colisão na horizontal.
        /// </summary>
        /// <returns>True para zerar a velocidade e corrigir a posição.</returns>
        protected virtual bool OnHorizontalCollision(int platType, Vector2 collisionPoint) => true;
        /// <summary>
        /// É chamado sempre que ocorre uma colisão na vertical.
        /// </summary>
        /// <returns>True para zerar a velocidade, corrigir a posição e fazer a checagem de Grounded.</returns>
        protected virtual bool OnVerticalCollision(int platType, Vector2 collisionPoint) => true;

        /// <summary>
        /// É chamado após a checagem de colisão com tiles e antes da movimentação.
        /// </summary>
        protected virtual void CustomHorizontalHandler() { }
        /// <summary>
        /// É chamado após a checagem de colisão com tiles e antes da movimentação.
        /// </summary>
        protected virtual void CustomVerticalHandler() { }

        private void HorizontalMovement()
        {
            HandleX();
            CustomHorizontalHandler();

            Transform.Position += new Vector2(speed.X * Time.DeltaTime, 0);
        }
        private void VerticalMovement()
        {
            if (Grounded) coyoteCounter = CoyoteTime;
            
            if (!Grounded)
            {
                Impulse(new Vector2(0, Gravity * Grid.Y * Time.DeltaTime));
                
                coyoteCounter -= Time.UnscaledDeltaTime;
            }

            HandleY();
            CustomVerticalHandler();

            Transform.Position += new Vector2(0, speed.Y * Time.DeltaTime);
            CanJump = Grounded || coyoteCounter > 0;
        }

        private void HandleX()
        {
            if (speed.X != 0)
            {
                Rectangle area = Bounds;
                float offset = Transform.Position.X - area.Location.X;
                int tile = 0;
                float checkX = (speed.X < 0 ? area.Left : area.Right) + speed.X * Time.DeltaTime;

                for (float y = area.Top; y < area.Bottom - 1; y++)
                {
                    if (Game.Tilemap[TileCollisionLayer].CheckCollision(new Vector2(checkX, y), out tile))
                    {
                        if (tile == 1 || (tile == 5 && speed.X < 0) || (tile == 4 && speed.X > 0))
                        {
                            int reposDir = Math.Sign(-speed.X);
                            float repos = reposDir == -1 ? area.Width : Grid.X;

                            if (OnHorizontalCollision(tile, new Vector2(checkX, y)))
                            {
                                Transform.Position = new Vector2((Mathf.ToGrid(checkX, (int)Grid.X) + repos * reposDir) + offset, Transform.Position.Y);
                                speed.X = 0;
                            }

                            break;
                        }
                    }
                }
            }
        }
        private void HandleY()
        {
            Grounded = false;
            Rectangle area = Bounds;
            float offset = Transform.Position.Y - area.Location.Y;

            int tile = 0;
            float checkY = (speed.Y < 0 ? area.Top : area.Bottom) + speed.Y * Time.DeltaTime;
            float dir = speed.Y != 0 ? speed.Y : 1;
            
            for (float x = area.Left; x < area.Right; x++)
            {
                if (Game.Tilemap[TileCollisionLayer].CheckCollision(new Vector2(x, checkY), out tile))
                {
                    float tilePos = Mathf.ToGrid(checkY, (int)Grid.Y);

                    if (tile == 1 || (tile == 2 && !IgnoreGhost && speed.Y >= 0 && previousBottom <= tilePos) || (tile == 3 && speed.Y < 0))
                    {
                        IgnoreGhost = false;
                        int reposDir = Math.Sign(-dir);
                        float repos = reposDir == -1 ? area.Height : Grid.Y;

                        if (OnVerticalCollision(tile, new Vector2(x, checkY)) && CornerCorrection(tile, new Vector2(x, checkY)))
                        {
                            Transform.Position = new Vector2(Transform.Position.X, (tilePos + repos * reposDir) + offset);

                            if (!Grounded && Game.Tilemap[TileCollisionLayer].CheckCollision(new Vector2(x, area.Bottom + 1), out tile))
                            {
                                Grounded = tile == 1 || (tile == 2 && !IgnoreGhost && previousBottom <= tilePos);
                                OnGhost = Grounded && tile == 2;
                            }

                            speed.Y = 0;
                        }

                        break;
                    }
                    else if (tile == 2 && previousBottom > tilePos) IgnoreGhost = false;
                }
            }

            previousBottom = area.Bottom;
        }

        private bool CornerCorrection(int platType, Vector2 collisionPoint)
        {
            if (CornerTolerance > 0 && (platType == PlatTypes.Block || platType == PlatTypes.DontPassDown) && speed.Y < 0)
            {
                Rectangle area = Bounds;
                Vector2 tilePos = Mathf.ToGrid(collisionPoint, Grid);
                float playerSide = collisionPoint.X > area.Center.X ? area.Right : area.Left;
                float tileSide = playerSide == area.Right ? tilePos.X : tilePos.X + Grid.X;
                float dist = Math.Abs(playerSide - tileSide);

                if (dist <= CornerTolerance)
                {
                    float repos = playerSide >= tileSide ? -area.Width : Grid.X;
                    float offset = Transform.Position.X - area.Location.X;
                    Vector2 targetPos = new Vector2(tilePos.X + repos + offset, Transform.Position.Y);
                    
                    if (!Game.Tilemap[TileCollisionLayer].CheckCollision(new Vector2(targetPos.X, collisionPoint.Y), out int tile) || tile == PlatTypes.DontPassUp)
                    {
                        Transform.Position = targetPos;

                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Tipos de plataforma (representam o index 1D do tile).
        /// </summary>
        public static class PlatTypes
        {
            public static int Block, DontPassUp, DontPassDown, DontPassRight, DontPassLeft;

            static PlatTypes() => Setup();

            /// <summary>
            /// Altera os tiles que representam cada tipo de plataforma.
            /// </summary>
            public static void Setup(int block = 1, int dontPassUp = 2, int dontPassDown = 3, int dontPassRight = 4, int dontPassLeft = 5)
            {
                Block = block;
                DontPassUp = dontPassUp;
                DontPassDown = dontPassDown;
                DontPassRight = dontPassRight;
                DontPassLeft = dontPassLeft;
            }
        }
    }
}