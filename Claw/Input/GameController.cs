﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Claw.Input
{
    /// <summary>
    /// Representa um controle.
    /// </summary>
    internal class GameController : IDisposable
    {
        public int Id;
        public ControllerTypes Type;
        public float LeftTrigger, RightTrigger;
        public Vector2 LeftThumbStick, RightThumbStick;

        private const int MaxAxis = 32767;
        private IntPtr sdlController;
        private ControllerState controllerNewState = new ControllerState(), controllerOldState;

        public GameController(IntPtr controller)
        {
            sdlController = controller;
            Id = SDL.SDL_JoystickInstanceID(SDL.SDL_GameControllerGetJoystick(sdlController));
            Type = (ControllerTypes)SDL.SDL_GameControllerGetType(sdlController);
        }
        ~GameController() => Dispose();

        public void Dispose()
        {
            sdlController = IntPtr.Zero;
            Id = 0;
            Type = ControllerTypes.Unknown;
        }

        public void Update()
        {
            controllerOldState = controllerNewState;
            controllerNewState = ControllerState.GetState(sdlController);

            LeftThumbStick.X = Mathf.Clamp(SDL.SDL_GameControllerGetAxis(sdlController, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX), -MaxAxis, MaxAxis) / MaxAxis; // Thumb sticks vão de -32768 à 32767
            LeftThumbStick.Y = Mathf.Clamp(SDL.SDL_GameControllerGetAxis(sdlController, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY), -MaxAxis, MaxAxis) / MaxAxis;
            RightThumbStick.X = Mathf.Clamp(SDL.SDL_GameControllerGetAxis(sdlController, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX), -MaxAxis, MaxAxis) / MaxAxis;
            RightThumbStick.Y = Mathf.Clamp(SDL.SDL_GameControllerGetAxis(sdlController, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY), -MaxAxis, MaxAxis) / MaxAxis;
            LeftTrigger = SDL.SDL_GameControllerGetAxis(sdlController, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT) / MaxAxis;
            RightTrigger = SDL.SDL_GameControllerGetAxis(sdlController, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT) / MaxAxis;

            controllerNewState.buttonStates.Add(Buttons.LeftTrigger, LeftTrigger >= .5f);
            controllerNewState.buttonStates.Add(Buttons.RightTrigger, RightTrigger >= .5f);
        }

        /// <summary>
        /// Verifica se o id recebido é o mesmo deste controle.
        /// </summary>
        public bool Compare(int id) => id == Id;

        /// <summary>
        /// Diz se o botão está sendo pressionado.
        /// </summary>
        public bool IsCurrentButtonDown(Buttons button) => controllerNewState.buttonStates[button];
        /// <summary>
        /// Diz se o botão estava sendo pressionado.
        /// </summary>
        public bool IsOldButtonDown(Buttons button) => controllerOldState.buttonStates[button];

        /// <summary>
        /// Muda a vibração do controle.
        /// </summary>
        /// <param name="duration">Duração da vibração, em milissegundos.</param>
        /// <param name="leftMotor">Intensidade da vibração do motor esquerdo (de 0 a 0xffff).</param>
        /// <param name="rightMotor">Intensidade da vibração do motor direito (de 0 a 0xffff).</param>
        public void SetVibration(uint duration, ushort leftMotor, ushort rightMotor) => SDL.SDL_GameControllerRumble(sdlController, leftMotor, rightMotor, duration);

        /// <summary>
        /// Representa o estado do controle.
        /// </summary>
        private class ControllerState
        {
            private static Array buttonsValues;

            static ControllerState() => buttonsValues = Enum.GetValues(typeof(Buttons));

            public Dictionary<Buttons, bool> buttonStates;

            public static ControllerState GetState(IntPtr controller)
            {
                ControllerState state = new ControllerState();
                state.buttonStates = new Dictionary<Buttons, bool>();

                foreach (int i in buttonsValues)
                {
                    Buttons button = (Buttons)i;

                    if (button != Buttons.LeftTrigger && button != Buttons.RightTrigger) state.buttonStates.Add(button, SDL.SDL_GameControllerGetButton(controller, (SDL.SDL_GameControllerButton)i) == 1);
                }

                return state;
            }
        }
    }
}