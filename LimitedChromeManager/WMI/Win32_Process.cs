
namespace LimitedChromeManager.WMI
{
    enum EWin32_Process
    {
        /*string */
        CreationClassName,
        /*string */
        Caption,
        /*string */
        CommandLine,
        /*DateTime*/
        CreationDate,
        /*string */
        CSCreationClassName,
        /*string */
        CSName,
        /*string */
        Description,
        /*string */
        ExecutablePath,
        /*uint16 */
        ExecutionState,
        /*string */
        Handle,
        /*uint32 */
        HandleCount,
        /*datetime*/
        InstallDate,
        /*uint64 */
        KernelModeTime,
        /*uint32 */
        MaximumWorkingSetSize,
        /*uint32 */
        MinimumWorkingSetSize,
        /*string */
        Name,
        /*string */
        OSCreationClassName,
        /*string */
        OSName,
        /*uint64 */
        OtherOperationCount,
        /*uint64 */
        OtherTransferCount,
        /*uint32 */
        PageFaults,
        /*uint32 */
        PageFileUsage,
        /*uint32 */
        ParentProcessId,
        /*uint32 */
        PeakPageFileUsage,
        /*uint64 */
        PeakVirtualSize,
        /*uint32 */
        PeakWorkingSetSize,
        /*uint32 */
        Priority /*= NULL,*/,
        /*uint64 */
        PrivatePageCount,
        /*uint32 */
        ProcessId,
        /*uint32 */
        QuotaNonPagedPoolUsage,
        /*uint32 */
        QuotaPagedPoolUsage,
        /*uint32 */
        QuotaPeakNonPagedPoolUsage,
        /*uint32 */
        QuotaPeakPagedPoolUsage,
        /*uint64 */
        ReadOperationCount,
        /*uint64 */
        ReadTransferCount,
        /*uint32 */
        SessionId,
        /*string */
        Status,
        /*datetime*/
        TerminationDate,
        /*uint32 */
        ThreadCount,
        /*uint64 */
        UserModeTime,
        /*uint64 */
        VirtualSize,
        /*string */
        WindowsVersion,
        /*uint64 */
        WorkingSetSize,
        /*uint64 */
        WriteOperationCount,
        /*uint64 */
        WriteTransferCount,
    }

    enum EWin32_Start
    {
        /*uint8[]  */
        SECURITY_DESCRIPTOR,
        /*uint64 */
        TIME_CREATED,
        /*uint32 */
        ProcessID,
        /*uint32 */
        ParentProcessID,
        /*uint8[]  */
        Sid,
        /*string */
        ProcessName,
        /*uint32 */
        SessionID,
    }

    enum EWin32_Stop
    {
        /*uint8[]  */
        SECURITY_DESCRIPTOR,
        /*uint64 */
        TIME_CREATED,
        /*uint32 */
        ProcessID,
        /*uint32 */
        ParentProcessID,
        /*uint8[]  */
        Sid,
        /*uint32 */
        ExitStatus,
        /*string */
        ProcessName,
        /*uint32 */
        SessionID,
    }
}
