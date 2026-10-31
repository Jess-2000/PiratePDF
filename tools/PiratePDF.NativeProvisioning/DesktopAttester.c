/* Fixed, CRT-free qualification attester. No workload, command line, CLR or PDF input.
   stdin: exactly 16 parent challenge bytes, then a one-byte release after verification.
   stdout: LE32(560), magic[8], challenge[16], PID, TID, two uint64 handles,
   station WCHAR[128], desktop WCHAR[128]. All unused bytes remain zero.
   The owning parent must enforce its startup deadline and terminate the Job on failure. */
typedef unsigned long DWORD;
typedef unsigned short WCHAR;
typedef unsigned long long UINT64;
typedef void *HANDLE;
__declspec(dllimport) HANDLE __stdcall GetStdHandle(DWORD);
__declspec(dllimport) DWORD __stdcall GetCurrentProcessId(void);
__declspec(dllimport) DWORD __stdcall GetCurrentThreadId(void);
__declspec(dllimport) int __stdcall ReadFile(HANDLE, void *, DWORD, DWORD *, void *);
__declspec(dllimport) int __stdcall WriteFile(HANDLE, const void *, DWORD, DWORD *, void *);
__declspec(dllimport) void __stdcall ExitProcess(DWORD);
__declspec(dllimport) HANDLE __stdcall GetProcessWindowStation(void);
__declspec(dllimport) HANDLE __stdcall GetThreadDesktop(DWORD);
__declspec(dllimport) int __stdcall GetUserObjectInformationW(HANDLE, int, void *, DWORD, DWORD *);

static unsigned char report[560];

static void put32(DWORD offset, DWORD value)
{
    for (DWORD index = 0; index < 4; ++index) report[offset + index] = (unsigned char)(value >> (index * 8));
}

static void put_handle(DWORD offset, HANDLE handle)
{
    UINT64 value = (UINT64)handle;
    for (DWORD index = 0; index < 8; ++index) report[offset + index] = (unsigned char)(value >> (index * 8));
}

static int transfer(HANDLE stream, void *buffer, DWORD size, int writing)
{
    DWORD offset = 0;
    while (offset < size)
    {
        DWORD count = 0;
        int success = writing
            ? WriteFile(stream, (unsigned char *)buffer + offset, size - offset, &count, 0)
            : ReadFile(stream, (unsigned char *)buffer + offset, size - offset, &count, 0);
        if (!success || !count || count > size - offset) return 0;
        offset += count;
    }
    return 1;
}

static int object_name(HANDLE handle, DWORD offset)
{
    DWORD needed = 0;
    if (!handle || !GetUserObjectInformationW(handle, 2, report + offset, 256, &needed)) return 0;
    if (needed < 4 || needed > 256 || needed % 2) return 0;
    WCHAR *name = (WCHAR *)(report + offset);
    if (name[needed / 2 - 1]) return 0;
    for (DWORD index = 0; index + 1 < needed / 2; ++index)
        if (name[index] < 32 || name[index] > 126 || name[index] == '\\') return 0;
    return 1;
}

void desktop_attester_start(void)
{
    HANDLE input = GetStdHandle((DWORD)-10), output = GetStdHandle((DWORD)-11);
    if (!transfer(input, report + 8, 16, 0)) ExitProcess(71);
    static const unsigned char magic[8] = { 'P', 'P', 'H', '2', 'B', 'A', '0', '1' };
    for (DWORD index = 0; index < 8; ++index) report[index] = magic[index];
    DWORD thread = GetCurrentThreadId();
    HANDLE station = GetProcessWindowStation(), desktop = GetThreadDesktop(thread);
    if (!object_name(station, 48) || !object_name(desktop, 304)) ExitProcess(72);
    put32(24, GetCurrentProcessId()); put32(28, thread);
    put_handle(32, station); put_handle(40, desktop);
    unsigned char prefix[4] = { 0x30, 0x02, 0, 0 }, release = 0;
    if (!transfer(output, prefix, 4, 1) || !transfer(output, report, sizeof(report), 1)) ExitProcess(73);
    /* Remain alive so the parent can inspect reported handles from this exact process.
       Release authorizes exit only; this binary never accepts or executes a workload. */
    if (!transfer(input, &release, 1, 0) || release != 0xa5) ExitProcess(74);
    ExitProcess(0);
}
