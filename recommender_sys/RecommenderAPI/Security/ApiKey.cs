public static class ApiKey
{
    private static readonly string KEY = "lUTd2jfk45lloEv9a1dff4ZxX";

    /// Checks if the passed key is valid.
    public static bool IsValid(string k)
    {
        return k == KEY;
    }
}