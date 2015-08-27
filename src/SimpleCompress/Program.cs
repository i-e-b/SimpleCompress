namespace SimpleCompress
{
    using System;

    public class Program
    {
		static void Main(string[] args){
            if (args.Length != 3 || (args[0] != "pack" && args[0] != "unpack"))
            {
                Console.WriteLine(string.Join(", ",args));
                ShowHelp();
                return;
            }

            if (args[0] == "pack") Compress.FolderToFile(args[1], args[2]);
            else Decompress.FromFileToFolder(args[1], args[2]);

            Console.WriteLine("Done.");
        }

        static void ShowHelp()
        {
            Console.WriteLine(
@"Simple Compress
    Usage:
        sc pack <src directory> <target file>
        sc unpack <src file> <target directory>
");
        }
    }
}
