using System;
using System.IO;
using System.Runtime.InteropServices;

namespace rm
{
    class Program
    {
        [Flags]
        public enum FileAttributes : uint
        {
            Readonly = 0x00000001,
            Hidden = 0x00000002,
            System = 0x00000004,
            Directory = 0x00000010,
            Archive = 0x00000020,
            Device = 0x00000040,
            Normal = 0x00000080,
            Temporary = 0x00000100,
            SparseFile = 0x00000200,
            ReparsePoint = 0x00000400,
            Compressed = 0x00000800,
            Offline = 0x00001000,
            NotContentIndexed = 0x00002000,
            Encrypted = 0x00004000,
            Write_Through = 0x80000000,
            Overlapped = 0x40000000,
            NoBuffering = 0x20000000,
            RandomAccess = 0x10000000,
            SequentialScan = 0x08000000,
            DeleteOnClose = 0x04000000,
            BackupSemantics = 0x02000000,
            PosixSemantics = 0x01000000,
            OpenReparsePoint = 0x00200000,
            OpenNoRecall = 0x00100000,
            FirstPipeInstance = 0x00080000
        }

        [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DeleteFileW([MarshalAs(UnmanagedType.LPWStr)]string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern uint GetFileAttributes(string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool SetFileAttributes(string lpFileName, [MarshalAs(UnmanagedType.U4)] FileAttributes dwFileAttributes);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool SetFileAttributes(string lpFileName, uint dwFileAttributes);

        static string CheckArgs(string[] args)
        {
            if (args.Length != 1 )
            {
                Usage();
                return null;
            }
            if ( !File.Exists(args[0]) )
            {
                Console.Error.WriteLine("E: file [{0}] does not exist", args[0]);
                return null;
            }
            return args[0];
        }
        static void Usage()
        {
            Console.Error.WriteLine("usage: rm {Unicodefile with one filename per line}");
        }
        static int Main(string[] args)
        {
            string Filename;
            if ((Filename=CheckArgs(args)) == null)
            {
                return 1;
            }
            bool hasErrors = false;
            using ( TextReader tr = new StreamReader(Filename) )
            {
                string line;
                while ( (line=tr.ReadLine()) != null)
                {
                    if ( String.IsNullOrEmpty(line))
                    {
                        continue;
                    }
                    if ( DeleteFileW(line) )
                    {
                        Console.Out.WriteLine("I: deleted [{0}]", line);
                    }
                    else
                    {
                        int LastError = System.Runtime.InteropServices.Marshal.GetLastWin32Error();

                        if (LastError == 2) // file not found
                        {
                            Console.Error.WriteLine("W: file not found [{0}]", line);
                        }
                        else
                        {
                            if (!ClearReadonlyFlag(line))
                            {
                                hasErrors = true;
                                //Console.Error.WriteLine("attribute readonly could not be removed [{0}]", line);
                            }
                            else
                            {
                                if ( !DeleteFileW(line) )
                                {
                                    hasErrors = true;
                                    Console.Error.WriteLine("E: rc [{0}] DeleteFile [{1}] after removing the readonly flag the file could still not be deleted",
                                        System.Runtime.InteropServices.Marshal.GetLastWin32Error(),
                                        line);
                                }
                            }
                        }
                    }
                }
            }
            return hasErrors ? 2 : 0;
        }
        static bool ClearReadonlyFlag(string filename)
        {
            uint CurrFlags = GetFileAttributes(filename);
            if (CurrFlags == uint.MaxValue) // 0xFFFF filename == INVALID_FILE_ATTRIBUTES
            {
                Console.Error.WriteLine("E: rc [{0}] GetFileAttributes [{1}]",
                    System.Runtime.InteropServices.Marshal.GetLastWin32Error(),
                    filename);
                return false;
            }

            bool isReadonly =   (CurrFlags & (uint)FileAttributes.Readonly)   != 0;
            bool isSystem =     (CurrFlags & (uint)FileAttributes.System)     != 0;
            bool isHidden =     (CurrFlags & (uint)FileAttributes.Hidden)     != 0;

            uint NewFlags = CurrFlags & (~(uint)FileAttributes.Readonly);       // clear out the readonly flag
            if ( ! SetFileAttributes(filename, NewFlags) )
            {
                Console.Error.WriteLine("E: rc [{0}] SetFileAttributes [{1}]",
                    System.Runtime.InteropServices.Marshal.GetLastWin32Error(),
                    filename);
                return false;
            }

            return true;
        }
    }
}
