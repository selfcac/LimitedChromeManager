using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Socket2Process
{
    //https://www.codeproject.com/Articles/14828/How-To-Get-Process-Owner-ID-and-Current-User-SID

    public static class ProcessUserSid
    {
        private static bool ProcessTokenToSidStruct(IntPtr token, out IntPtr SID, Action<string> errorLog)
        {
            bool result = false;
            const int bufLength = 256; // actuall need 36

            TOKEN_USER tokUser;
            IntPtr tokenInformation = IntPtr.Zero;
            SID = IntPtr.Zero;

            try
            {
                int dataLength = bufLength; // Usally you call GetTokenInformation() with null,0 to get size
                                            //          we skip that and give it 256 always (bigger than the actual 36)
                tokenInformation = Marshal.AllocHGlobal(dataLength);
                result = GetTokenInformation(token,
                        TOKEN_INFORMATION_CLASS.TokenUser, tokenInformation, dataLength, ref dataLength);
                if (result)
                {
                    tokUser = (TOKEN_USER)Marshal.PtrToStructure(tokenInformation, typeof(TOKEN_USER));
                    SID = tokUser.User.Sid;
                    if (SID == IntPtr.Zero)
                    {
                        errorLog?.Invoke("Sid in sid struct is null\n"
                        + "LastWin32Error: " + Win32ApiUtils.LaseError() );
                    }
                }
                else
                {
                    errorLog?.Invoke("Can't get sid struct from token\n"
                       + "LastWin32Error: " + Win32ApiUtils.LaseError() );
                }
            }
            catch (Exception ex)
            {
                errorLog?.Invoke("Can't get sid from process token\n"
                       + "LastWin32Error: " + Win32ApiUtils.LaseError() + "\n" + ex.ToString());
                result = false;
            }
            finally
            {
                Marshal.FreeHGlobal(tokenInformation);
            }

            return result;
        }

        public static bool ProcessHandleToSidStruct(IntPtr pToken, out IntPtr SID, Action<string> errorLog)
        {
            bool result = false;
            int Access = TOKEN_QUERY;

            IntPtr procToken = IntPtr.Zero;
            SID = IntPtr.Zero;

            try
            {
                if (OpenProcessToken(pToken, Access, ref procToken))
                {
                    result = ProcessTokenToSidStruct(procToken, out SID, errorLog);
                    if (!result)
                    {
                        errorLog?.Invoke("Can't get sid token of process token\n"
                            + "LastWin32Error: " + Win32ApiUtils.LaseError() );
                    }
                    
                    CloseHandle(procToken);
                }
                return result;
            }
            catch (Exception ex)
            {
                errorLog?.Invoke("Can't get sid from process handle\n"
                       + "LastWin32Error: " + Win32ApiUtils.LaseError() + "\n" + ex.ToString());
                return false;
            }
        }

        public static string sidFromProcess(IntPtr processHandle, Action<string> errorLog)
        {
            string resultSID = "";
            IntPtr _SID = IntPtr.Zero;
            try
            {
                if (ProcessHandleToSidStruct(processHandle, out _SID,errorLog))
                {
                    if (!ConvertSidToStringSid(_SID, ref resultSID))
                    {
                        errorLog?.Invoke("Can't convert sid to string\n"
                          + "LastWin32Error: " + Win32ApiUtils.LaseError());

                        // May return code `(1337) The security ID structure is invalid`
                        //  even tough not documented here:
                        //  (Maybe because the test try before the process had the chance to fill it with data?? fast at start??)
                        //      https://docs.microsoft.com/en-gb/windows/win32/api/sddl/nf-sddl-convertstringsidtosida?redirectedfrom=MSDN
                    }
                }
            }
            catch (Exception ex) {
                errorLog?.Invoke("Can't get sid from process\n"
                        + "LastWin32Error: " + Win32ApiUtils.LaseError() + "\n" + ex.ToString());
            }

            if (resultSID == "")
            {
                errorLog?.Invoke("Can't get sid from process\n"
                          + "LastWin32Error: " + Win32ApiUtils.LaseError() );
            }

            return resultSID;
        }

        /// <summary>
        /// Get Sid from process
        /// </summary>
        /// <param name="PID">process id</param>
        /// <param name="errorLog">how to handle errors</param>
        /// <remarks>If you try this too fast, you can get Error codes 0 (ERROR_SUCCESS) or 6 (INVALID_HANDLE)</remarks>
        /// <returns></returns>
        public static string sidFromProcess(uint PID, Action<string> errorLog)
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
                    result = sidFromProcess(handle,errorLog);
                }

                if (handle.Equals(IntPtr.Zero))
                {
                    errorLog?.Invoke("Can't open handle for SID! (Got null)\n"
                        + "LastWin32Error: " + Win32ApiUtils.LaseError() );
                }
            }
            catch (Exception ex)
            {
                errorLog?.Invoke("Can't get sid from process\n" 
                        + "LastWin32Error: " + Win32ApiUtils.LaseError()  + "\n" + ex.ToString());
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
