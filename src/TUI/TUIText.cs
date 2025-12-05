using SEPackager.Compression;

namespace SEPackager.TUI
{
    internal class TUIText
    {
        public static void ViewLogo()
        {
            Console.Clear();
            Console.Write("""
                
                  ███████╗███████╗██████╗
                  ██╔════╝██╔════╝██╔══██╗
                  ███████╗█████╗  ██████╔╝
                  ╚════██║██╔══╝  ██╔═══╝
                  ███████║███████╗██║
                  ╚══════╝╚══════╝╚═╝  Packager
                
                  -----------------------------

                """);
        }

        public static void ViewMain()
        {
            ViewLogo();

            Console.Write("""
                  Choose an option:

                  1. Compress                   (In progress..)
                  2. Decompress                 (Unavaible)
                  3. Add to SEP                 (Unavaible)
                  4. Delete from SEP            (Unavaible)

                  0. Exit

                
                """);
        }

        public static void ViewCompres()
        {
            ViewLogo();

            Console.Write($"""
                  Compression

                  Enter the name of the archive.
                
                

                  Choose the max size of the parts in MegaBytes. (100MB default)
                  > 



                """);
        }

        public static void ViewSubCompres()
        {
            Console.Write("""


                  Put the files inside the input folder and press enter to proceed.

                """);

            Console.ReadKey();
            Console.Clear();

            Console.Write("  Preparing the files for compression... ");
            FileCheck.GetFileExtensions();
            Console.WriteLine("Done.");

            Console.Write("  Compressing the files...\n");
            Archive.Write();
        }
    }
}