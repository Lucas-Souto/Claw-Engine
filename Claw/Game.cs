﻿using System;
using System.Collections.Generic;
using Claw.Graphics;
using Claw.Audio;

namespace Claw
{
    /// <summary>
    /// Classe central do jogo.
    /// </summary>
    public class Game : IDisposable
    {
        public static Game Instance { get; private set; }
        public Window Window { get; private set; }
        public Renderer Renderer { get; private set; }
        public AudioManager Audio { get; private set; }
        public Tilemap Tilemap
        {
            get => tilemap;
            set
            {
                if (value != tilemap)
                {
                    if (tilemap != null) tilemap.RemoveAll();

                    if (value != null) value.AddAll();

                    tilemap = value;
                }
            }
        }
        public GameComponentCollection Components => components;
        private bool isRunning;
        private Tilemap tilemap;
        private GameComponentCollection components;

        public Game() { }
        ~Game() => Dispose();

        public void Dispose()
        {
            Window?.Dispose();
            Renderer?.Dispose();
            Audio?.Dispose();

            if (components != null)
            {
                for (int i = 0; i < components.Count; i++)
                {
                    if (components[i] is IDisposable dispose) dispose.Dispose();
                }
                
                components = null;
            }

            Window = null;
            Renderer = null;
            Audio = null;
            isRunning = false;
        }

        /// <summary>
        /// Tenta inicializar o jogo e, se obter sucesso, executa o <see cref="Initialize"/> e o game loop.
        /// </summary>
        public void Run()
        {
            if (isRunning) return;
            else if (Instance != null) throw new Exception("Não é possível rodar duas instâncias de jogo ao mesmo tempo!");

            if (SDL.SDL_Init(SDL.SDL_INIT_EVERYTHING) == 0)
            {
                int result = SDL.SDL_CreateWindowAndRenderer(800, 600, 0, out IntPtr window, out IntPtr renderer);

                if (result == 0)
                {
                    isRunning = true;
                    Instance = this;
                    Window = new Window(window);
                    Renderer = new Renderer(renderer);
                    Audio = new AudioManager();
                    Renderer.ClearColor = Color.CornflowerBlue;
                    components = new GameComponentCollection();
                }
            }

            if (isRunning)
            {
                Input.Input.SetControllers();
                Initialize();

                if (Texture.Pixel == null) Texture.Pixel = new Texture(1, 1, 0xffffffff);
                
                GameLoop();
            }
        }
        /// <summary>
        /// Fecha o jogo.
        /// </summary>
        public void Close() => isRunning = false;

        protected virtual void Initialize() { }
        protected virtual void Step() { }
        protected virtual void Render() { }
        
        private void GameLoop()
        {
            uint frameStart;
            int frameTime = 0;

            while (isRunning)
            {
                frameStart = SDL.SDL_GetTicks();

                Input.Input.Update();
                Time.Update(frameTime);
                Step();

                Renderer.Clear();
                Draw.UpdateCamera();
                Render();
                Renderer.Present();

                frameTime = (int)(SDL.SDL_GetTicks() - frameStart);

                if (Time.FrameDelay > frameTime) SDL.SDL_Delay((uint)(Time.FrameDelay - frameTime));

                HandleEvents();
            }

            Clear();
        }
        private void HandleEvents()
        {
            SDL.SDL_Event sdlEvent;

            while(SDL.SDL_PollEvent(out sdlEvent) != 0)
            {
                switch (sdlEvent.type)
                {
                    case SDL.SDL_EventType.SDL_QUIT:
                        isRunning = false;
                        Instance = null;

                        return;
                    case SDL.SDL_EventType.SDL_WINDOWEVENT:
                        switch (sdlEvent.window.windowEvent)
                        {
                            case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED: Window.ClientResized?.Invoke(this, EventArgs.Empty); break;
                        }
                        break;
                    case SDL.SDL_EventType.SDL_MOUSEWHEEL: Input.Input.UpdateScroll(sdlEvent.wheel); break;
                    case SDL.SDL_EventType.SDL_MOUSEMOTION: Input.Input.UpdateMouseMotion(sdlEvent.motion); break;
                    case SDL.SDL_EventType.SDL_CONTROLLERDEVICEADDED: Input.Input.AddController(sdlEvent.cdevice.which); break;
                    case SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMOVED: Input.Input.RemoveController(sdlEvent.cdevice.which); break;
                }

                SDL.SDL_PumpEvents();
            }
        }
        private void Clear()
        {
            Dispose();
            SDL.SDL_Quit();
        }
    }
}