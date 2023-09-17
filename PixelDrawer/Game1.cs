using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace PixelDrawer
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private readonly Rectangle _gameSize = new(0, 0, 320, 240);
        private Vector3 _screenRatio;
        private Matrix _screenScale;

        private Texture2D _screenTexture;
        private Texture2D _materialTexture;
        private PixelDrawer _drawer;

        private Point _lastMousePosition;
        private Model3D _characterModel;
        private Vector3 _characterPosition;
        private Vector3 _characterRotation;
        private Vector3 _characterScale = Vector3.One;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            SetResolution(1280, 960);

            _screenTexture = new Texture2D(GraphicsDevice, _gameSize.Width, _gameSize.Height);
            _materialTexture = Content.Load<Texture2D>("Cat_diffuse");
            //_materialTexture = Content.Load<Texture2D>("Head_diffuse");
            _drawer = new PixelDrawer(_screenTexture, _materialTexture);

            _characterModel = Model3D.LoadModel(@"Models/Cat/Cat.obj");
            //_characterModel = Model3D.LoadModel(@"Models/Head/head.obj");
            //_characterModel = Model3D.LoadPlaneModel();
            _characterPosition = new Vector3(_screenTexture.Width / 2, _screenTexture.Height / 2, 0f);
            _characterRotation = new Vector3(90, 180, 0);
            _characterScale = new Vector3(5f);

            DrawModel(_characterModel, _characterPosition, _characterRotation, _characterScale);

            base.Initialize();
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

            for (int i = 0; i < model.Indexes.Length; i += 6)
            {
                var v1 = Vector3.Transform(model.Verticies[model.Indexes[i]], modelMatrix);
                var v2 = Vector3.Transform(model.Verticies[model.Indexes[i + 2]], modelMatrix);
                var v3 = Vector3.Transform(model.Verticies[model.Indexes[i + 4]], modelMatrix);

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
                X = (float)width / _gameSize.Width,
                Y = (float)height / _gameSize.Height,
                Z = 1
            };
            _screenScale = Matrix.CreateScale(_screenRatio);
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here
            var speed = new Vector3(1, 1, 1);

            var keyboarState = Keyboard.GetState();
            if (keyboarState.IsKeyDown(Keys.Up))
            {
                _characterRotation.Y -= speed.Y;
                DrawModel(_characterModel, _characterPosition, _characterRotation, _characterScale);
            }
            else if (keyboarState.IsKeyDown(Keys.Down))
            {
                _characterRotation.Y += speed.Y;
                DrawModel(_characterModel, _characterPosition, _characterRotation, _characterScale);
            }
            else if (keyboarState.IsKeyDown(Keys.Left))
            {
                _characterRotation.X -= speed.X;
                DrawModel(_characterModel, _characterPosition, _characterRotation, _characterScale);
            }
            else if (keyboarState.IsKeyDown(Keys.Right))
            {
                _characterRotation.X += speed.X;
                DrawModel(_characterModel, _characterPosition, _characterRotation, _characterScale);
            }
            //else
            //{
            //    _characterRotation += new Vector3(1, 1, 1);
            //    DrawModel(_characterModel, _characterPosition, _characterRotation, _characterScale);
            //}


            //var mousePosition = Mouse.GetState().Position;
            //if (mousePosition != _lastMousePosition)
            //{
            //    var screenMousePosition = mousePosition.ToVector2() / _screenRatio.X;

            //    _drawer.Fill(Color.Yellow);
            //    _drawer.Line(_screenTexture.Bounds.Center.ToVector2(), screenMousePosition, Color.Red);
            //    _drawer.ApplyPixels();

            //    _lastMousePosition = mousePosition;
            //}

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _screenScale);
            _spriteBatch.Draw(_screenTexture, Vector2.Zero, Color.White);
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}