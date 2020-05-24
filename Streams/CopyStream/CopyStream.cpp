/***********************************************************************

  Copyright (c) 2005 Inv Softworks LLC.
  All rights reserved.

  Copy Stream example application.

  You can use this code freely for any commercial or non-commercial
  purpose. However if you use this code in your program, you should
  add the string "Contains code by Inv Softworks LLC, www.flexhex.com"
  in your copyright notice text.

***********************************************************************/

#include <windows.h>
#include <stdio.h>


#define ALLOWED_ATTRIBUTES (FILE_ATTRIBUTE_ARCHIVE | FILE_ATTRIBUTE_HIDDEN | FILE_ATTRIBUTE_READONLY | FILE_ATTRIBUTE_SYSTEM | FILE_ATTRIBUTE_NOT_CONTENT_INDEXED)


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
  BY_HANDLE_FILE_INFORMATION bhfi;
  HANDLE hInFile, hOutFile;
  BYTE buf[64*1024];
  DWORD dwBytesRead, dwBytesWritten;
  int iRetCode = EXIT_SUCCESS;

  if (argc != 3) {
    printf("\nStream copy program: www.flexhex.com\n\nUsage:\n  CS fromstream tostream\n\nExample:\n  CS c:\\some.txt d:\\file.dat:text\n\n");
    exit(EXIT_SUCCESS);
  }
  
  try {
    hInFile = ::CreateFile(argv[1], GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (hInFile == INVALID_HANDLE_VALUE) throw ::GetLastError();
    if (!::GetFileInformationByHandle(hInFile, &bhfi)) throw ::GetLastError();

    hOutFile = ::CreateFile(argv[2], GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (hOutFile == INVALID_HANDLE_VALUE) throw ::GetLastError();

    do {
      if (!::ReadFile(hInFile, buf, sizeof(buf), &dwBytesRead, NULL)) throw ::GetLastError();
      if (dwBytesRead) {
        if (!::WriteFile(hOutFile, buf, dwBytesRead, &dwBytesWritten, NULL)) throw ::GetLastError();
        if (dwBytesWritten < dwBytesRead) throw (DWORD)ERROR_HANDLE_DISK_FULL;
      }
    } while (dwBytesRead == sizeof(buf));

    ::CloseHandle(hInFile);

    // Set output file attributes
    if (!::SetFileTime(hOutFile, &bhfi.ftCreationTime, &bhfi.ftLastAccessTime, &bhfi.ftLastWriteTime)) throw ::GetLastError();
    ::CloseHandle(hOutFile);
    if (!::SetFileAttributes(argv[2], bhfi.dwFileAttributes & ALLOWED_ATTRIBUTES)) throw ::GetLastError();
  }
  catch (DWORD dwErrCode) {
    PrintError(dwErrCode);
    iRetCode = EXIT_FAILURE;
  }

  exit(iRetCode);
}
