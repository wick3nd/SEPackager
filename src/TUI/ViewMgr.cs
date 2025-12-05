namespace SEPackager.TUI
{
    internal class ViewMgr
    {
        public static void ShowMain()
        {
            TUIText.ViewMain();
            InputMgr.ListenMain();
        }

        public static void ShowCompres()
        {
            Program.CreateFolders();

            TUIText.ViewCompres();
            InputMgr.ListenCompres();

            TUIText.ViewSubCompres();
        }
    }
}