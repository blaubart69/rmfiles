using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace rm
{
    class Opts
    {
        public string FilenameWithFiles;
        public string baseDirectory;
        public bool dryrun;
    }
    class CommandlineOpts
    {
        static void ShowHelp(Mono.Options.OptionSet p)
        {
            Console.WriteLine("Usage: rm {filename} [OPTINS]");
            Console.WriteLine("removes files given in a file");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Error);
        }
        public static Opts GetOpts(string[] args)
        {
            bool show_help = false;
            Opts tmpOpts = new Opts();
            Opts resultOpts = null;
            var p = new Mono.Options.OptionSet() {
                { "b|basedir=", "prints out little statistics",             v => tmpOpts.baseDirectory = v },
                { "n|dryrun",  "show what would be deleted",                v => tmpOpts.dryrun = (v != null) },
                { "h|help",     "show this message and exit",               v => show_help = v != null }            };

            try
            {
                List<string> FilenameWithFilenames = p.Parse(args);
                if (FilenameWithFilenames.Count != 1)
                {
                    Console.Error.WriteLine("only one filename with filenames within the file");
                    show_help = true;
                }
                else
                {
                    resultOpts = tmpOpts;
                    resultOpts.FilenameWithFiles = FilenameWithFilenames[0];
                }
            }
            catch (Mono.Options.OptionException oex)
            {
                Console.WriteLine(oex.Message);
            }
            if (show_help)
            {
                ShowHelp(p);
            }
            return resultOpts;
        }
    }
}
