/***********************************************************************

  Copyright (c) 2005 Inv Softworks LLC.
  All rights reserved.

  Delete Stream example application.

  You can use this code freely for any commercial or non-commercial
  purpose. However if you use this code in your program, you should
  add the string "Contains code by Inv Softworks LLC, www.flexhex.com"
  in your copyright notice text.

***********************************************************************/

#include <windows.h>
#include <stdio.h>


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
  int iRetCode = EXIT_SUCCESS;

  if (argc != 2) {
    printf("\nDelete stream program: www.flexhex.com\n\nUsage:\n  DS stream\n\nExample:\n  DS C:\\file.dat:text\n\n");
    exit(EXIT_SUCCESS);
  }

  try {
    if (!::DeleteFile(argv[1])) throw ::GetLastError();
  }
  catch (DWORD dwErrCode) {
    PrintError(dwErrCode);
    iRetCode = EXIT_FAILURE;
  }

  exit(iRetCode);
}
