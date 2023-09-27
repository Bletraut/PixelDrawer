using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PixelDrawer
{
    public class ClothGame : Game
    {
        private Matrix _viewProjectionMatrix;
        public Matrix ViewProjectionMatrix
        {
            get
            {
                CalculateViewMatrixIfNeeded();
                return _viewProjectionMatrix;
            }
        }

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Texture2D _screenTexture;
        private PixelDrawer _drawer;

        private readonly Rectangle _gameDefaultSize = new(0, 0, 640, 480);
        private readonly Point _targetResolution = new(1280, 960);
        private Vector3 _screenRatio;
        private Matrix _screenScale;
        private Vector3 _gameHalfSize;

        private VerletSolver _verletSolver;
        private Cloth _cloth;

        private readonly float _viewDistance = 800;
        private Vector3 _viewPosition;
        private Vector3 _viewRotation;

        private Matrix _viewMatrix;
        private Matrix _projectionMatrix;
        private bool _isViewMatrixDirty;

        private MouseState _lastMouseState;
        private List<VerletBody> _bullets;
        private int _bulletsCount = 10;
        private Model3D _bulletModel;
        private Model3D _simpleModel;

        private Texture2D _fabricTexture;
        private Texture2D _bulletTexture;

        public ClothGame() 
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _screenTexture = new Texture2D(GraphicsDevice, _gameDefaultSize.Width, _gameDefaultSize.Height);

            _drawer = new PixelDrawer();
            _drawer.SetScreenTexture(_screenTexture);

            _fabricTexture = Content.Load<Texture2D>("Fabric");
            _bulletTexture = Content.Load<Texture2D>("Bullet");

            SetResolution(_targetResolution.X, _targetResolution.Y);
            //SetResolution(_gameDefaultSize.Width, _gameDefaultSize.Height);
            _gameHalfSize = new Vector3(_gameDefaultSize.Width / 2f, _gameDefaultSize.Height / 2f, 1f);

            _viewRotation = new Vector3(90, 0, 0);
            _isViewMatrixDirty = true;

            _verletSolver = new VerletSolver();
            _cloth = new Cloth(_verletSolver, _gameDefaultSize);

            _bullets = new List<VerletBody>(_bulletsCount);
            _bulletModel = Model3D.LoadModel(@"Models/Cloth/LowPolySphere.obj");

            _simpleModel = Model3D.LoadModel(@"Models/Head/Grid.obj");

            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            // Add game logic.
            _verletSolver.Update(gameTime);

            // Keyboars controls.
            var keyboardState = Keyboard.GetState();
            if (keyboardState.IsKeyDown(Keys.A))
            {
                _viewRotation.X -= 1f;
                _isViewMatrixDirty = true;
            }
            else if (keyboardState.IsKeyDown(Keys.D))
            {
                _viewRotation.X += 1f;
                _isViewMatrixDirty = true;
            }
            if (keyboardState.IsKeyDown(Keys.W))
            {
                _viewRotation.Y -= 3;
                _isViewMatrixDirty = true;
            }
            else if (keyboardState.IsKeyDown(Keys.S))
            {
                _viewRotation.Y += 3;
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
            _drawer.SetMaterialTexture(_fabricTexture);
            _cloth.Draw(_drawer, ViewProjectionMatrix);
            _drawer.SetMaterialTexture(_bulletTexture);
            DrawBullets();
            //DrawSimpleModel();

            _drawer.ApplyPixels();

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _screenScale);
            _spriteBatch.Draw(_screenTexture, Vector2.Zero, Color.White);
            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawSimpleModel()
        {
            var modelMatrix = Matrix.CreateRotationX(MathHelper.Pi)
                * Matrix.CreateScale(700) 
                * ViewProjectionMatrix;

            var positions = new Vector3[_simpleModel.Verticies.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                var v = Vector4.Transform(new Vector4(_simpleModel.Verticies[i], 1f), modelMatrix);
                positions[i] = new Vector3(v.X, v.Y, v.Z) / v.W;
                positions[i].Z = v.Z;
                positions[i] *= _gameHalfSize;
                positions[i] += _gameHalfSize;
            }

            for (int i = 0; i < _simpleModel.Indexes.Length; i += 6)
            {
                var v1 = positions[_simpleModel.Indexes[i]];
                var v2 = positions[_simpleModel.Indexes[i + 2]];
                var v3 = positions[_simpleModel.Indexes[i + 4]];

                var uv1 = _simpleModel.UVs[_simpleModel.Indexes[i + 1]];
                var uv2 = _simpleModel.UVs[_simpleModel.Indexes[i + 3]];
                var uv3 = _simpleModel.UVs[_simpleModel.Indexes[i + 5]];

                var shadowColor = Color.White;
                var normal = Vector3.Cross(v2 - v1, v3 - v1);
                normal.Normalize();
                var lightness = shadowColor * Math.Clamp(Vector3.Dot(normal, new Vector3(0f, 1f, 0)), 0.35f, 1f);
                lightness.A = 255;

                _drawer.FilledTriangle(v1, v2, v3, uv1, uv2, uv3, lightness);
            }
        }

        private void DrawBullets()
        {
            foreach (var bullet in _bullets)
            {
                var rotationRadians = bullet.CurrentPosition / 180 * MathF.PI;

                var modelMatrix = Matrix.CreateScale(bullet.Radius)
                    * Matrix.CreateFromYawPitchRoll(rotationRadians.X, rotationRadians.Y, rotationRadians.Z)
                    * Matrix.CreateTranslation(bullet.CurrentPosition)
                    * ViewProjectionMatrix;

                var positions = new Vector3[_bulletModel.Verticies.Length];
                for (int i = 0; i < positions.Length; i++)
                {
                    var v = Vector4.Transform(new Vector4(_bulletModel.Verticies[i], 1f), modelMatrix);
                    positions[i] = new Vector3(v.X, v.Y, v.Z) / v.W;
                    positions[i].Z = v.Z;
                    positions[i] *= _gameHalfSize;
                    positions[i] += _gameHalfSize;
                }

                for (int i = 0; i < _bulletModel.Indexes.Length; i += 6)
                {
                    var v1 = positions[_bulletModel.Indexes[i]];
                    var v2 = positions[_bulletModel.Indexes[i + 2]];
                    var v3 = positions[_bulletModel.Indexes[i + 4]];

                    var uv1 = _bulletModel.UVs[_bulletModel.Indexes[i + 1]];
                    var uv2 = _bulletModel.UVs[_bulletModel.Indexes[i + 3]];
                    var uv3 = _bulletModel.UVs[_bulletModel.Indexes[i + 5]];

                    _drawer.FilledTriangle(v1, v2, v3, uv1, uv2, uv3, Color.White, CullingMode.None);
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

            var shootDirection = Vector3.Normalize(-_viewPosition);
            var firePosition = _viewPosition + shootDirection * 150;

            var bullet = new VerletBody()
            {
                Radius = 50,
                Mass = 100,
                CurrentPosition = firePosition,
                LastPosition = firePosition,
            };
            _bullets.Add(bullet);
            _verletSolver.AddBody(bullet);

            var clickPosition = new Vector3()
            {
                X = mousePosition.X,
                Y = mousePosition.Y,
            };

            shootDirection = ScreenToWorld(new Vector2(clickPosition.X, clickPosition.Y));
            bullet.CurrentPosition += shootDirection * 25f;
        }

        public Vector3 ScreenToWorld(Vector2 screenPosition)
        {
            var minPointSource = GraphicsDevice.Viewport.Unproject(new Vector3(screenPosition, 0f), _projectionMatrix, _viewMatrix, Matrix.Identity);
            var maxPointSource = GraphicsDevice.Viewport.Unproject(new Vector3(screenPosition, 1f), _projectionMatrix, _viewMatrix, Matrix.Identity);

            var direction = maxPointSource - minPointSource;
            direction.Y *= -1;

            return Vector3.Normalize(direction);
        }

        private void SetResolution(int width, int height)
        {
            _graphics.PreferredBackBufferWidth = width;
            _graphics.PreferredBackBufferHeight = height;
            _graphics.ApplyChanges();

            _screenRatio = new Vector3()
            {
                X = (float)width / _gameDefaultSize.Width,
                Y = (float)height / _gameDefaultSize.Height,
                Z = 1
            };
            _screenScale = Matrix.CreateScale(_screenRatio);
        }

        private void CalculateViewMatrixIfNeeded()
        {
            if (_isViewMatrixDirty)
            {
                _isViewMatrixDirty = false;

                var direction = new Vector3()
                {
                    X = MathF.Cos(MathHelper.ToRadians(_viewRotation.X)),
                    Y = 0,
                    Z = MathF.Sin(MathHelper.ToRadians(_viewRotation.X)),
                };
                _viewPosition = _viewDistance * direction;
                _viewPosition.Y = _viewRotation.Y;

                _viewMatrix = Matrix.CreateLookAt(_viewPosition, Vector3.Zero, Vector3.Up);

                _projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(60),
                    (float)_gameDefaultSize.Width / _gameDefaultSize.Height, 0.3f, 50f);

                _viewProjectionMatrix = _viewMatrix * _projectionMatrix;
            }
        }
    }
}
