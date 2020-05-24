/***********************************************************************

  Copyright (c) 2005 Inv Softworks LLC.
  All rights reserved.

  List Streams example application.

  You can use this code freely for any commercial or non-commercial
  purpose. However if you use this code in your program, you should
  add the string "Contains code by Inv Softworks LLC, www.flexhex.com"
  in your copyright notice text.

***********************************************************************/

#include <windows.h>
#include <tchar.h>
#include <stdio.h>

#include "AltStreams.h"


void PrintError(DWORD dwErr) {
  char szMsg[256];
  DWORD dwFlags = FORMAT_MESSAGE_IGNORE_INSERTS |
                  FORMAT_MESSAGE_MAX_WIDTH_MASK |
                  FORMAT_MESSAGE_FROM_SYSTEM;

  if (!::FormatMessage(dwFlags, NULL, dwErr, 0, szMsg, sizeof(szMsg), NULL)) strcpy(szMsg, "Unknown error.");
  printf(szMsg);
  printf("\n");
}


void main(int argc, char *argv[]) {
  NTQUERYINFORMATIONFILE NtQueryInformationFile;
  int iRetCode = EXIT_FAILURE;

  if (argc != 2) {
    printf("\nList streams program: www.flexhex.com\n\nUsage:\n  LS file\n\nExample:\n  LS C:\\file.dat\n\n");
    exit(EXIT_SUCCESS);
  }

  try {
    LPBYTE pInfoBlock = NULL;
    ULONG uInfoBlockSize = 0;
    IO_STATUS_BLOCK ioStatus;
    NTSTATUS status;
    HANDLE hFile;

    // Load function pointer
    (FARPROC&)NtQueryInformationFile = ::GetProcAddress(::GetModuleHandle("ntdll.dll"), "NtQueryInformationFile");
    if (NtQueryInformationFile == NULL) throw ::GetLastError();

    // Obtain SE_BACKUP_NAME privilege (required for opening a directory)
    HANDLE hToken = NULL;
    TOKEN_PRIVILEGES tp;
    try {
      if (!::OpenProcessToken(::GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES, &hToken)) throw ::GetLastError();
      if (!::LookupPrivilegeValue(NULL, SE_BACKUP_NAME, &tp.Privileges[0].Luid))  throw ::GetLastError();
      tp.PrivilegeCount = 1;
      tp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
      if (!::AdjustTokenPrivileges(hToken, FALSE, &tp, sizeof(TOKEN_PRIVILEGES), NULL, NULL))  throw ::GetLastError();
    }
    catch (DWORD) { }   // Ignore errors
    if (hToken) ::CloseHandle(hToken);

    hFile = ::CreateFile(argv[1], 0, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, NULL);
    if (hFile == INVALID_HANDLE_VALUE) throw ::GetLastError();

    // Get stream information block.
    // The amount of memory required for info block is unknown, so we
    // allocate 16, 32, 48 kb and so on until the block is sufficient.
    do {
      uInfoBlockSize += 16 * 1024;
      delete [] pInfoBlock;
      pInfoBlock = new BYTE [uInfoBlockSize];
      ((PFILE_STREAM_INFORMATION)pInfoBlock)->StreamNameLength = 0;
      status = NtQueryInformationFile(hFile, &ioStatus, (LPVOID)pInfoBlock, uInfoBlockSize, FileStreamInformation);
    } while (status == STATUS_BUFFER_OVERFLOW);
    ::CloseHandle(hFile);

    PFILE_STREAM_INFORMATION pStreamInfo = (PFILE_STREAM_INFORMATION)(LPVOID)pInfoBlock;
    ULONGLONG uTotalSize = 0;
    LARGE_INTEGER fsize;
    WCHAR wszStreamName[MAX_PATH];
    char szStreamName[MAX_PATH], szPath[MAX_PATH];
    LPSTR pszName;
    int len;

    if (!::GetFullPathName(argv[1], MAX_PATH, szPath, &pszName)) throw ::GetLastError();
    printf("%s\n", szPath);

    // Loop for all streams
    for (;;) {
      // Check if stream info block is empty (directory may have no stream)
      if (pStreamInfo->StreamNameLength == 0) break; // No stream found

      // Get stream name
      memcpy(wszStreamName, pStreamInfo->StreamName, pStreamInfo->StreamNameLength);
      wszStreamName[pStreamInfo->StreamNameLength / sizeof(WCHAR)] = L'\0';

      // Remove attribute tag and convert to char
      LPWSTR pTag = wcsstr(wszStreamName, L":$DATA");
      if (pTag) *pTag = L'\0';
      len = ::WideCharToMultiByte(CP_ACP, 0, wszStreamName, -1, szStreamName, MAX_PATH, NULL, NULL);

      // Full path including stream name
      strcpy(szPath, argv[1]);
      if (strcmp(szStreamName, ":")) {
        strcat(szPath, szStreamName);   // Named stream - attach stream name
        iRetCode = EXIT_SUCCESS;        // Alternate stream found
      }

      // Get stream size
      hFile = ::CreateFile(szPath, 0, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_EXISTING, 0, NULL);
      if (hFile == INVALID_HANDLE_VALUE) throw ::GetLastError();
      if (!::GetFileSizeEx(hFile, &fsize)) throw ::GetLastError();
      ::CloseHandle(hFile);

      // Append spaces up to position 40
      if (len < 40) {
        strcat(szStreamName, "                                        ");
        szStreamName[40] = '\0';
      }
      else
        strcat(szStreamName, " ");

      printf("  %s%I64u\n", szStreamName, fsize.QuadPart);
      uTotalSize += fsize.QuadPart;   // Compute total file size

      if (pStreamInfo->NextEntryOffset == 0) break;   // No more stream info records
      pStreamInfo = (PFILE_STREAM_INFORMATION)((LPBYTE)pStreamInfo + pStreamInfo->NextEntryOffset);   // Next stream info record
    }

    if (pStreamInfo->StreamNameLength)
      printf("Total size: %I64u bytes.\n", uTotalSize);
    else
      printf("No streams found.");
  }
  catch (DWORD dwErrCode) {
    PrintError(dwErrCode);
    iRetCode = EXIT_FAILURE;
  }

  exit(iRetCode);
}
