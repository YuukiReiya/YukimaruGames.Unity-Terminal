using System;
using UnityEngine;
using YukimaruGames.Terminal.UI.Presentation;

namespace YukimaruGames.Terminal.Infrastructure
{
    public sealed class CursorFlashSpeedProvider : ICursorFlashSpeedProvider
    {
        private float _cursorFlashSpeed;
        public event Action<float> OnChangedFlashSpeed;

        public CursorFlashSpeedProvider(float flashSpeed)
        {
            _cursorFlashSpeed = flashSpeed;
        }
        
        public float GetFlashSpeed() => _cursorFlashSpeed;

        public void SetFlashSpeed(float value)
        {
            if (Mathf.Approximately(_cursorFlashSpeed, value))
            {
                return;
            }

            _cursorFlashSpeed = Mathf.Max(0, value);
            OnChangedFlashSpeed?.Invoke(value);
        }
    }
}
