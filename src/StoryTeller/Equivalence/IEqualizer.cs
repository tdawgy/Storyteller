namespace StoryTeller.Equivalence
{
    public interface IEqualizer
    {
        bool Matches(object one, object two);
    }
}