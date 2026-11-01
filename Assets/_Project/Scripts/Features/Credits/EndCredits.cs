using System;

namespace SibJam.Credits
{
    // Сервис финальных титров: показывает их на сервисном канвасе (CreditsView) один раз
    public class EndCredits
    {
        private readonly CreditsView _view;

        public EndCredits(CreditsView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public bool IsShowing { get; private set; }

        public void Show()
        {
            if (IsShowing)
                return;

            IsShowing = true;
            _view.Play();
        }
    }
}
