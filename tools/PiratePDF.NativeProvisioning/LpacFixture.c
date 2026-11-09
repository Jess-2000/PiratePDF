/* Controlled, CRT-free launch fixture. Compiled only for explicit LPAC feasibility work. */
typedef unsigned long DWORD;
typedef void *HANDLE;
__declspec(dllimport) HANDLE __stdcall GetStdHandle(DWORD);
__declspec(dllimport) int __stdcall WriteFile(HANDLE, const void *, DWORD, DWORD *, void *);
__declspec(dllimport) void __stdcall ExitProcess(DWORD);

void fixture_start(void)
{
    static const char message[] = "lpac-controlled-fixture-started\r\n";
    DWORD written = 0;
    if (!WriteFile(GetStdHandle((DWORD)-11), message, sizeof(message) - 1, &written, 0)) ExitProcess(71);
    ExitProcess(written == sizeof(message) - 1 ? 0 : 72);
}
