namespace SibJam
{
    public interface ICursorShower
    {
        public bool IsShow { get; }

        public void Show();

        public void Hide();
    }
}