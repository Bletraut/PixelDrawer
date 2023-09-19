using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace PixelDrawer
{
    public class Game1 : Game
    {
        private enum ModelType
        {
            None,
            Cat,
            CatPlane,
            Head
        }

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private readonly Rectangle _gameSizeDefault = new(0, 0, 320, 240);
        private readonly Rectangle _gameSizeHigh = new(0, 0, 320 * 2, 240 * 2);
        private readonly Rectangle _gameSizeLow = new(0, 0, 320 / 2, 240 / 2);

        private readonly Point _targetResolution = new(1280, 960);

        private readonly Vector2 _textInfoMargin = new(10, 10);

        private Vector3 _screenRatio;
        private Matrix _screenScale;

        private Texture2D _screenTexture;
        private SpriteFont _mainFont;

        private Texture2D _materialTexture;
        private PixelDrawer _drawer;

        private Point _lastMousePosition;
        private Model3D _characterModel;
        private Vector3 _characterPosition;
        private Vector3 _characterRotation;
        private Vector3 _characterScale = Vector3.One;
        private Vector3[] _characterModifiedVerticies;
        private bool _isModelChanged;

        private float _zBufferClearValue = -10f;

        private ModelType _currentModelType;
        private Rectangle _currentGameSize;
        private KeyboardState _lastKeyboardState;

        private float _defaultTextHeight;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            _drawer = new PixelDrawer();

            ChangeGameSize();
            ChangeModel();

            base.Initialize();
        }

        private bool IsKeyPressed(Keys key) => !_lastKeyboardState.IsKeyDown(key) && Keyboard.GetState().IsKeyDown(key);

        private void ChangeGameSize()
        {
            if (_currentGameSize == _gameSizeDefault)
                _currentGameSize = _gameSizeHigh;
            else if (_currentGameSize == _gameSizeHigh)
                _currentGameSize = _gameSizeLow;
            else
                _currentGameSize = _gameSizeDefault;

            _screenTexture?.Dispose();
            _screenTexture = new Texture2D(GraphicsDevice, _currentGameSize.Width, _currentGameSize.Height);
            _drawer.SetScreenTexture(_screenTexture);

            SetResolution(_targetResolution.X, _targetResolution.Y);

            _isModelChanged = true;
            SetModelPosition();
        }

        private void ChangeModel()
        {
            if (_currentModelType == ModelType.Cat)
                SetModel(ModelType.Head);
            else if (_currentModelType == ModelType.Head)
                SetModel(ModelType.CatPlane);
            else
                SetModel(ModelType.Cat);
        }

        private void SetModelPosition()
        {
            switch (_currentModelType)
            {
                case ModelType.Cat:
                    _characterPosition = new Vector3(_currentGameSize.Width / 2, _currentGameSize.Height / 2, 0f);
                    break;
                case ModelType.CatPlane:
                    _characterPosition = new Vector3(_currentGameSize.Width / 2, _currentGameSize.Height / 2, 0f);
                    break;
                case ModelType.Head:
                    _characterPosition = new Vector3(_currentGameSize.Width / 2, _currentGameSize.Height, 0f);
                    break;
            }
        }

        private void SetModel(ModelType modelType)
        {
            if (_currentModelType == modelType) 
                return;

            _isModelChanged = true;
            _currentModelType = modelType;

            switch (_currentModelType)
            {
                case ModelType.Cat:
                    _materialTexture = Content.Load<Texture2D>("Cat_diffuse");
                    _characterModel = Model3D.LoadModel(@"Models/Cat/Cat.obj");

                    _characterRotation = new Vector3(90, 180, 0);
                    _characterScale = new Vector3(5f);
                    break;
                case ModelType.CatPlane:
                    _materialTexture = Content.Load<Texture2D>("Cat_diffuse");
                    _characterModel = Model3D.LoadPlaneModel();

                    _characterScale = new Vector3(0.5f);
                    break;
                case ModelType.Head:
                    _materialTexture = Content.Load<Texture2D>("Head_diffuse");
                    _characterModel = Model3D.LoadModel(@"Models/Head/head.obj");

                    _characterRotation = new Vector3(180, 180, 0);
                    _characterScale = new Vector3(2000f);
                    break;
            }
            SetModelPosition();
            _characterModifiedVerticies = new Vector3[_characterModel.Verticies.Length];

            _drawer.SetMaterialTexture(_materialTexture);
        }

        private void DrawModel(Model3D model, Vector3 position, Vector3 eulerAngles, Vector3 scale)
        {
            _drawer.ClearZBuffer();
            _drawer.Fill(Color.Black);

            var radianAngles = eulerAngles / 180 * MathF.PI;
            var modelRotation = Quaternion.CreateFromYawPitchRoll(radianAngles.X, radianAngles.Y, radianAngles.Z);
            var modelMatrix = Matrix.CreateScale(scale)
                * Matrix.CreateFromQuaternion(modelRotation)
                * Matrix.CreateTranslation(position);

            for (int i = 0; i < model.Verticies.Length; i++)
            {
                _characterModifiedVerticies[i] = Vector3.Transform(model.Verticies[i], modelMatrix);
            }
            for (int i = 0; i < model.Indexes.Length; i += 6)
            {
                var v1 = _characterModifiedVerticies[model.Indexes[i]];
                var v2 = _characterModifiedVerticies[model.Indexes[i + 2]];
                var v3 = _characterModifiedVerticies[model.Indexes[i + 4]];

                var uv1 = model.UVs[model.Indexes[i + 1]];
                var uv2 = model.UVs[model.Indexes[i + 3]];
                var uv3 = model.UVs[model.Indexes[i + 5]];

                _drawer.FilledTriangle(v1, v2, v3, uv1, uv2, uv3, Color.White);
                //_drawer.Triangle(v1, v2, v3, Color.White);
            }

            _drawer.ApplyPixels();
        }

        private void SetResolution(int width, int height)
        {
            _graphics.PreferredBackBufferWidth = width;
            _graphics.PreferredBackBufferHeight = height;
            _graphics.ApplyChanges();

            _screenRatio = new Vector3()
            {
                X = (float)width / _currentGameSize.Width,
                Y = (float)height / _currentGameSize.Height,
                Z = 1
            };
            _screenScale = Matrix.CreateScale(_screenRatio);
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _mainFont = Content.Load<SpriteFont>("MainFont");
            _defaultTextHeight = _mainFont.MeasureString("A").X;

            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here
            var speed = new Vector3(1, 1, 1);
            var scale = new Vector3(0.5f);
            var zBufferSpeed = 1f;

            var keyboarState = Keyboard.GetState();

            if (IsKeyPressed(Keys.F1))
            {
                ChangeGameSize();
            }
            else if (IsKeyPressed(Keys.F2))
            {
                ChangeModel();
            }

            if (keyboarState.IsKeyDown(Keys.C))
            {
                _zBufferClearValue += zBufferSpeed;
                _drawer.ZBufferClearValue = _zBufferClearValue;
                _isModelChanged = true;
            }
            else if (keyboarState.IsKeyDown(Keys.V))
            {
                _zBufferClearValue -= zBufferSpeed;
                _drawer.ZBufferClearValue = _zBufferClearValue;
                _isModelChanged = true;
            }

            if (keyboarState.IsKeyDown(Keys.Z))
            {
                _characterScale += scale;
                _isModelChanged = true;
            }
            else if (keyboarState.IsKeyDown(Keys.X))
            {
                _characterScale -= scale;
                _isModelChanged = true;
            }
            if (keyboarState.IsKeyDown(Keys.Up))
            {
                _characterRotation.Y -= speed.Y;
                _isModelChanged = true;
            }
            else if (keyboarState.IsKeyDown(Keys.Down))
            {
                _characterRotation.Y += speed.Y;
                _isModelChanged = true;
            }
            if (keyboarState.IsKeyDown(Keys.Left))
            {
                _characterRotation.X -= speed.X;
                _isModelChanged = true;
            }
            else if (keyboarState.IsKeyDown(Keys.Right))
            {
                _characterRotation.X += speed.X;
                _isModelChanged = true;
            }

            if (_isModelChanged)
            {
                _isModelChanged = false;
                DrawModel(_characterModel, _characterPosition, _characterRotation, _characterScale);
            }

            _lastKeyboardState = keyboarState;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _screenScale);
            _spriteBatch.Draw(_screenTexture, Vector2.Zero, Color.White);
            _spriteBatch.End();

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            var fps = Math.Round(1f / gameTime.ElapsedGameTime.TotalSeconds);
            var screenInfo = $"{_currentGameSize.Width}x{_currentGameSize.Height} FPS={fps} F1=Resolution, F2=Model";
            _spriteBatch.DrawString(_mainFont, screenInfo, _textInfoMargin, Color.White);

            var zClearValue = float.IsInfinity(_drawer.ZBufferClearValue) ? "-Infinity" : _drawer.ZBufferClearValue.ToString();
            var modelInfo = $"{_currentModelType}, R:{_characterRotation} S:{_characterScale} Z:{zClearValue}";
            _spriteBatch.DrawString(_mainFont, modelInfo, new Vector2(_textInfoMargin.X, _defaultTextHeight + _textInfoMargin.Y * 2), Color.White);

            var controlsInfo = $"ARROWS=Rotate, ZX=+/-Scale, CV=+/-ZBufferClearValue";
            var downPosition = new Vector2()
            {
                X = 10,
                Y = _graphics.PreferredBackBufferHeight - _defaultTextHeight - _textInfoMargin.Y,
            };
            _spriteBatch.DrawString(_mainFont, controlsInfo, downPosition, Color.White);

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}