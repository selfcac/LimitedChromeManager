using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Socket2Process
{
    public static class ProcessUserSid
    {
        private static bool ProcessTokenToSidStruct(IntPtr token, out IntPtr SID)
        {
            bool result = false;
            const int bufLength = 256; // actuall need 36

            TOKEN_USER tokUser;
            IntPtr tokenInformation = IntPtr.Zero;
            SID = IntPtr.Zero;

            try
            {
                int dataLength = bufLength;
                tokenInformation = Marshal.AllocHGlobal(dataLength);
                result = GetTokenInformation(token,
                        TOKEN_INFORMATION_CLASS.TokenUser, tokenInformation, dataLength, ref dataLength);
                if (result)
                {
                    tokUser = (TOKEN_USER)Marshal.PtrToStructure(tokenInformation, typeof(TOKEN_USER));
                    SID = tokUser.User.Sid;
                    if (SID == IntPtr.Zero)
                    {
                        Console.WriteLine("Problem Token.SidPtr, " + Marshal.GetLastWin32Error());
                    }
                }
                else
                {
                    Console.WriteLine("Problem TokenInformaion, " + Marshal.GetLastWin32Error());
                }
                return result;
            }
            catch (Exception err)
            {
                return false;
            }
            finally
            {
                Marshal.FreeHGlobal(tokenInformation);
            }
        }

        public static bool DumpUserInfo(IntPtr pToken, out IntPtr SID)
        {
            bool result = false;
            int Access = TOKEN_QUERY;

            IntPtr procToken = IntPtr.Zero;
            SID = IntPtr.Zero;

            try
            {
                if (OpenProcessToken(pToken, Access, ref procToken))
                {
                    result = ProcessTokenToSidStruct(procToken, out SID);
                    if (!result)
                    {
                        Console.WriteLine("Problem Token->Sid 1, " + Marshal.GetLastWin32Error());
                    }
                    
                    CloseHandle(procToken);
                }
                return result;
            }
            catch (Exception err)
            {
                Console.WriteLine("Err User 3");
                return false;
            }
        }

        public static string sidFromProcess(IntPtr processHandle)
        {
            string resultSID = "";

            IntPtr _SID = IntPtr.Zero;
            try
            {
                if (DumpUserInfo(processHandle, out _SID))
                {
                    ConvertSidToStringSid(_SID, ref resultSID);
                }
            }
            catch (Exception) {
                Console.WriteLine("Err User 2");
            }

            return resultSID;
        }

        public static string sidFromProcess(uint PID)
        {
            string result = "";
            IntPtr handle = IntPtr.Zero;
            try
            {
                // Why Query is enough : .
                //      https://posts.specterops.io/understanding-and-defending-against-access-token-theft-finding-alternatives-to-winlogon-exe-80696c8a73b
                handle = OpenProcess(ProcessAccessFlags.QueryInformation, false, PID);
                if (!handle.Equals(IntPtr.Zero))
                {
                    result = sidFromProcess(handle);
                }

                if (handle.Equals(IntPtr.Zero))
                {
                    Console.WriteLine("Can't open handle for SID! (Got null) Err:" + Marshal.GetLastWin32Error());
                }
                else if (result == "")
                {
                    Console.WriteLine("Can't process SID! (Got '') Err:" + Marshal.GetLastWin32Error());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Err User 1 -> " + PID );
            }
            finally
            {
                if (!handle.Equals(IntPtr.Zero))
                {
                    CloseHandle(handle);
                }
            }
            return result ;
        }

        // ============================================================================
        // =================        Win32 API Interop               ===================
        // ============================================================================

        #region win32api
        public const int TOKEN_QUERY = 0X00000008;
        public const int ERROR_NO_MORE_ITEMS = 259;

        [StructLayout(LayoutKind.Sequential)]
        public struct SID_AND_ATTRIBUTES
        {
            public IntPtr Sid;
            public int Attributes;
        }

        public struct TOKEN_USER
        {
            public SID_AND_ATTRIBUTES User;
        }

        public enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId
        }

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        [DllImport("kernel32.dll", EntryPoint = "OpenProcess")]
        public static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

        [DllImport("advapi32")]
        public static extern bool OpenProcessToken(
            IntPtr ProcessHandle, // handle to process
            int DesiredAccess, // desired access to process
            ref IntPtr TokenHandle // handle to open access token
        );

        [DllImport("kernel32")]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport("advapi32", CharSet = CharSet.Auto)]
        public static extern bool GetTokenInformation(
            IntPtr hToken,
            TOKEN_INFORMATION_CLASS tokenInfoClass,
            IntPtr TokenInformation,
            int tokeInfoLength,
            ref int reqLength
        );

        [DllImport("advapi32", CharSet = CharSet.Auto)]
        public static extern bool ConvertSidToStringSid(
           IntPtr pSID,
           [In, Out, MarshalAs(UnmanagedType.LPTStr)] ref string pStringSid
       );

        #endregion
    }
}
