namespace SEPackager
{
    internal class ViewMgr
    {
        public static void ShowMain()
        {
            TUI.ViewMain();
            InputMgr.ListenMain();
        }

        public static void ShowCompres()
        {
            Program.CreateFolders();

            TUI.ViewCompres();
            InputMgr.ListenCompres();

            TUI.ViewSubCompres();
        }
    }
}
