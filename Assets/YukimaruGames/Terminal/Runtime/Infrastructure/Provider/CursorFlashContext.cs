using System;
using UnityEngine;
using YukimaruGames.Terminal.UI.Presentation;

namespace YukimaruGames.Terminal.Infrastructure
{
    public sealed class CursorFlashContext : ICursorFlashProvider, ICursorFlashMutator
    {
        private float _cursorFlashSpeed;
        public float FlashSpeed
        {
            get => _cursorFlashSpeed;
            set => SetFlashSpeed(value);
        }
        public event Action<float> OnChangedFlashSpeed;

        public CursorFlashContext(float flashSpeed)
        {
            _cursorFlashSpeed = flashSpeed;
        }

        private void SetFlashSpeed(float value)
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
