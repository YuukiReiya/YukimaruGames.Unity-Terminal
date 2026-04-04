using System;
using UnityEngine;
using YukimaruGames.Terminal.UI.Core;

namespace YukimaruGames.Terminal.Infrastructure.UI
{
    public sealed class CursorFlashSpeedAccessor : ICursorFlashSpeedProvider,ICursorFlashSpeedMutator
    {
        private float _cursorFlashSpeed;
        public event Action<float> OnChangedFlashSpeed;
        public float FlashSpeed
        {
            get => _cursorFlashSpeed;
            set => SetFlashSpeed(value);
        }

        public CursorFlashSpeedAccessor(float flashSpeed)
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
            OnChangedFlashSpeed?.Invoke(_cursorFlashSpeed);
        }
    }
}
