namespace SimpleCompress
{
    using System;

    /// <summary>
    /// Execution point if running as a command line tool
    /// </summary>
    public class Program
    {
		static void Main(string[] args){
            // sanity check args
            if (args.Length < 3 || (args[0] != "pack" && args[0] != "unpack"))
            {
                HelpAndExit(args);
            }

            string signPath = null;
            string password = null;

            // look for signing
            if (args.Length > 3)
            {
                switch (args[3])
                {
                    case "-sign":
                        if (args[0] != "pack" || args.Length < 6) HelpAndExit(args);
                        signPath = args[4];
                        password = args[5];
                        break;

                    case "-verify":
                        if (args[0] != "unpack" || args.Length < 5) HelpAndExit(args);
                        signPath = args[4];
                        break;

                    default: HelpAndExit(args);
                        return;
                }
            }

            if (args[0] == "pack") Compress.FolderToFile(args[1], args[2], signPath, password);
            else Decompress.FromFileToFolder(args[1], args[2], signPath);

            Console.WriteLine("Done.");
        }

        private static void HelpAndExit(string[] args)
        {
            Console.WriteLine(string.Join(", ", args));
            ShowHelp();
            Environment.Exit(1);
        }

        static void ShowHelp()
        {
            Console.WriteLine(
@"Simple Compress
    Usage:
        sc pack <src directory> <target file> [flags]
        sc unpack <src file> <target directory> [flags]

    Flags:
        -sign <pfx path> <password>
            (pack) sign the output with the private key of a pfx file
        -verify <cer path>
            (unpack) verify that a package is correctly signed,
            using the public key in a cer file
");
        }
    }
}
