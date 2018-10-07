using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Spi;

namespace rm
{
    class Program
    {
        static void Usage()
        {
            Console.Error.WriteLine("usage: rm {Unicodefile with one filename per line}");
        }
        static int Main(string[] args)
        {
            Opts opts;
            if ((opts = CommandlineOpts.GetOpts(args)) == null)
            {
                return 1;
            }

            IEnumerable<string> FullFilenames = CreateFilenames(opts.FilenameWithFiles, opts.baseDirectory);

            bool hasErrors = false;
            if (opts.dryrun)
            {
                foreach (string FullFilename in FullFilenames)
                {
                    Console.Out.WriteLine($"would delete [{FullFilename}]");
                }
            }
            else
            {
                ulong deleted, notFound, error; deleted = notFound = error = 0;
                using (var delWriter = new ConsoleAndFileWriter(@".\rmDeleted.txt"))
                using (var notFoundWriter = new ConsoleAndFileWriter(@".\rmNotFound.txt"))
                using (var errorWriter = new ConsoleAndFileWriter(@".\rmError.txt"))
                {
                    hasErrors = RemoveFiles.Run(FullFilenames,
                        OnDeleted: filename => { delWriter.WriteLine(filename); deleted += 1; },
                        OnNotFound: filename => { notFoundWriter.WriteLine(filename); notFound += 1; },
                        OnError: (LastErrorCode, Api, Message) => { errorWriter.WriteLine($"{LastErrorCode}\t{Api}\t{Message}"); error += 1; }
                        );
                    Console.Error.WriteLine($"deleted: {deleted}, notfound: {notFound}, errors: {error}");
                }
            }

            return hasErrors ? 99 : 0;
        }

        private static IEnumerable<string> CreateFilenames(String inputfilename, string basedir)
        {
            IEnumerable<string> FilenameFromInputfile =
                            Misc.ReadLines(inputfilename)
                            .Select(line => Misc.GetLastTsvColumn(line));

            IEnumerable<string> FullFilenames = null;

            if (!String.IsNullOrEmpty(basedir))
            {
                string LongBase = Misc.GetLongFilenameNotation(basedir);
                FullFilenames = FilenameFromInputfile.Select(filename => Path.Combine(LongBase, filename));
            }
            else
            {
                FullFilenames = FilenameFromInputfile;
            }

            return FullFilenames;
        }
    }
}
