using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PixelDrawer
{
    public class ClothGame : Game
    {
        private Matrix _viewMatrix;
        public Matrix ViewMatrix
        {
            get
            {
                CalculateViewMatrixIfNeeded();
                return _viewMatrix;
            }
        }

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Texture2D _screenTexture;
        private PixelDrawer _drawer;

        private readonly Rectangle _gameSizeDefault = new(0, 0, 640, 480);
        private readonly Point _targetResolution = new(1280, 960);
        private Vector3 _screenRatio;
        private Matrix _screenScale;

        private VerletSolver _verletSolver;
        private Cloth _cloth;

        private Vector3 _viewPosition;
        private Vector3 _viewRotation;
        private Vector3 _viewScale;
        private bool _isViewMatrixDirty;

        private MouseState _lastMouseState;
        private List<VerletBody> _bullets;
        private int _bulletsCount = 10;

        public ClothGame() 
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _screenTexture = new Texture2D(GraphicsDevice, _gameSizeDefault.Width, _gameSizeDefault.Height);

            _drawer = new PixelDrawer();
            _drawer.SetScreenTexture(_screenTexture);

            SetResolution(_targetResolution.X, _targetResolution.Y);

            _viewPosition = new Vector3(_gameSizeDefault.Width / 2, _gameSizeDefault.Height / 2, 0);
            _viewRotation = new Vector3(0, 0, 0);
            _viewScale = new Vector3(0.5f);
            _isViewMatrixDirty = true;

            _verletSolver = new VerletSolver();
            _cloth = new Cloth(_verletSolver);
            _bullets = new List<VerletBody>(_bulletsCount);

            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            // Add game logic.
            _verletSolver.Update(gameTime);

            // Keyboars controls.
            var keyboardState = Keyboard.GetState();
            if (keyboardState.IsKeyDown(Keys.Left))
            {
                _viewRotation.X -= 1;
                _isViewMatrixDirty = true;
            }
            else if (keyboardState.IsKeyDown(Keys.Right))
            {
                _viewRotation.X += 1;
                _isViewMatrixDirty = true;
            }

            // Mouse controls.
            var mouseState = Mouse.GetState();

            var isLeftButtonClicked = mouseState.LeftButton == ButtonState.Pressed
                && _lastMouseState.LeftButton == ButtonState.Released;
            if (isLeftButtonClicked)
            {
                Shoot(mouseState.Position);
            }

            _lastMouseState = mouseState;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            _drawer.Fill(Color.Black);
            _drawer.ClearZBuffer();

            // Add draw logic.
            _cloth.Draw(_drawer, ViewMatrix);
            DrawBullets();

            _drawer.ApplyPixels();

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _screenScale);
            _spriteBatch.Draw(_screenTexture, Vector2.Zero, Color.White);
            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawBullets()
        {
            foreach (var bullet in _bullets)
            {
                var bulletPosition = bullet.CurrentPosition;
                var lastPosition = Vector3.Transform(bulletPosition, ViewMatrix);

                var segmentsCount = 12;
                var step = 360f / segmentsCount;
                for (int i = 0; i <= segmentsCount; i++)
                {
                    var angle = i * step / 180 * MathF.PI;
                    var direction = new Vector3()
                    {
                        X = MathF.Cos(angle),
                        Y = MathF.Sin(angle)
                    };

                    var newPosition = bulletPosition + bullet.Radius * direction;
                    newPosition = Vector3.Transform(newPosition, ViewMatrix);
                    _drawer.Line(lastPosition, newPosition, Color.Red);

                    lastPosition = newPosition;
                }

                for (int i = 0; i <= segmentsCount; i++)
                {
                    var angle = i * step / 180 * MathF.PI;
                    var direction = new Vector3()
                    {
                        X = MathF.Cos(angle),
                        Z = MathF.Sin(angle)
                    };

                    var newPosition = bulletPosition + bullet.Radius * direction;
                    newPosition = Vector3.Transform(newPosition, ViewMatrix);
                    _drawer.Line(lastPosition, newPosition, Color.Red);

                    lastPosition = newPosition;
                }

                for (int i = 0; i <= segmentsCount; i++)
                {
                    var angle = i * step / 180 * MathF.PI;
                    var direction = new Vector3()
                    {
                        Z = MathF.Cos(angle),
                        Y = MathF.Sin(angle)
                    };

                    var newPosition = bulletPosition + bullet.Radius * direction;
                    newPosition = Vector3.Transform(newPosition, ViewMatrix);
                    _drawer.Line(lastPosition, newPosition, Color.Red);

                    lastPosition = newPosition;
                }
            }
        }

        private void Shoot(Point mousePosition)
        {
            if (_bullets.Count >= _bulletsCount)
            {
                _verletSolver.RemoveBody(_bullets[0]);
                _bullets.RemoveAt(0);
            }

            var clickPosition = new Vector3()
            {
                X = mousePosition.X,
                Y = mousePosition.Y,
            } / _screenRatio;

            var invertViewMatrix = Matrix.Invert(ViewMatrix);
            var firePosition = Vector3.Transform(clickPosition, invertViewMatrix);
            firePosition.Z = 250;

            var bullet = new VerletBody()
            {
                Radius = 50,
                Mass = 100,
                CurrentPosition = firePosition,
                LastPosition = firePosition,
            };
            bullet.CurrentPosition -= new Vector3(0, 0, 25);
            _bullets.Add(bullet);
            _verletSolver.AddBody(bullet);
        }

        private void SetResolution(int width, int height)
        {
            _graphics.PreferredBackBufferWidth = width;
            _graphics.PreferredBackBufferHeight = height;
            _graphics.ApplyChanges();

            _screenRatio = new Vector3()
            {
                X = (float)width / _gameSizeDefault.Width,
                Y = (float)height / _gameSizeDefault.Height,
                Z = 1
            };
            _screenScale = Matrix.CreateScale(_screenRatio);
        }

        private void CalculateViewMatrixIfNeeded()
        {
            if (_isViewMatrixDirty)
            {
                _isViewMatrixDirty = false;

                var radiansAngle = _viewRotation / 180 * MathF.PI;

                _viewMatrix = Matrix.CreateScale(_viewScale)
                    * Matrix.CreateFromQuaternion(Quaternion.CreateFromYawPitchRoll(radiansAngle.X, radiansAngle.Y, radiansAngle.Z))
                    * Matrix.CreateTranslation(_viewPosition);
            }
        }
    }
}
