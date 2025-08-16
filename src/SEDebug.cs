namespace SEPpackager
{
    public enum SEDebugState
    {
        Log,
        Info,
        Warning,
        Error
    };

    class SEDebug
    {
        public static void Log<T>(SEDebugState state, T text)
        {
            var previousColor = Console.ForegroundColor;

            switch (state)
            {
                case SEDebugState.Log: Console.ForegroundColor = ConsoleColor.White;
                    break;

                case SEDebugState.Info: Console.ForegroundColor = ConsoleColor.Cyan;
                    break;

                case SEDebugState.Warning: Console.ForegroundColor = ConsoleColor.DarkYellow;
                    break;

                case SEDebugState.Error: Console.ForegroundColor = ConsoleColor.DarkRed;
                    break;
            }

            Console.Write($"  > [{state}]    {text}\n");

            Console.ForegroundColor = previousColor;
        }
    }
}