using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Spi;

namespace rm
{
    class RemoveFiles
    {
        public static bool Run(IEnumerable<string> FullFilenames, Action<string> OnDeleted, Action<string> OnNotFound, Action<int,string,string> OnError)
        {
            bool ErrorOccured = false;
            foreach (string FullFilename in FullFilenames)
            {
                ErrorOccured = RemoveFile(FullFilename, OnDeleted, OnNotFound, OnError, ErrorOccured);
            }
            return ErrorOccured;
        }

        private static bool RemoveFile(string FullFilename, Action<string> OnDeleted, Action<string> OnNotFound, Action<int, string, string> OnError, bool ErrorOccured)
        {
            if (Native.DeleteFileW(FullFilename))
            {
                OnDeleted?.Invoke(FullFilename);
            }
            else
            {
                int LastError = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                if (LastError == 2) // file not found
                {
                    OnNotFound?.Invoke(FullFilename);
                }
                else
                {
                    if (!ClearReadonlyFlag(FullFilename, OnError))
                    {
                        ErrorOccured = true;
                        //Console.Error.WriteLine("attribute readonly could not be removed [{0}]", line);
                    }
                    else
                    {
                        if (Native.DeleteFileW(FullFilename))
                        {
                            OnDeleted?.Invoke(FullFilename);
                        }
                        else
                        {
                            ErrorOccured = true;
                            Console.Error.WriteLine("E: rc [{0}] DeleteFile [{1}] after removing the readonly flag the file could still not be deleted",
                                System.Runtime.InteropServices.Marshal.GetLastWin32Error(),
                                FullFilename);
                        }
                    }
                }
            }

            return ErrorOccured;
        }

        static bool ClearReadonlyFlag(string filename, Action<int,string,string> OnError)
        {
            uint CurrFlags = Native.GetFileAttributes(filename);
            if (CurrFlags == uint.MaxValue) // 0xFFFF filename == INVALID_FILE_ATTRIBUTES
            {
                OnError?.Invoke(System.Runtime.InteropServices.Marshal.GetLastWin32Error(), "GetFileAttributes", filename);
                return false;
            }

            bool isReadonly = (CurrFlags & (uint)FileAttributes.Readonly) != 0;
            bool isSystem = (CurrFlags & (uint)FileAttributes.System) != 0;
            bool isHidden = (CurrFlags & (uint)FileAttributes.Hidden) != 0;

            uint NewFlags = CurrFlags & (~(uint)FileAttributes.Readonly);       // clear out the readonly flag
            if (!Native.SetFileAttributes(filename, NewFlags))
            {
                OnError?.Invoke(System.Runtime.InteropServices.Marshal.GetLastWin32Error(), "SetFileAttributes", filename);
                return false;
            }

            return true;
        }
    }
}
