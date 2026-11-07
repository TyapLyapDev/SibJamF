namespace SibJam.Cutscenes
{
    // Идёт ли сейчас какая-нибудь катсцена
    public class ActiveCutscenes
    {
        private int _count;

        public bool IsAnyPlaying => _count > 0;

        public void Enter() => _count++;

        public void Exit() => _count--;
    }
}
