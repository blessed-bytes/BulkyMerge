namespace BulkyMerge;

public class BulkyMergeException : Exception
{
    public BulkyMergeException(string message) : base(message) { }

    public BulkyMergeException(string message, Exception inner) : base(message, inner) { }
}
