using CommonInterfaces;  // Add reference to CommonInterfaces

namespace CommonLibrary
{
    public static class StatusHelper
    {
        public static IStatusLogger? StatusLogger { get; set; }

        public static void AppendStatus(string message)
        {
            StatusLogger?.AppendStatus(message);
        }
    }
}
