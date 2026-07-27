#pragma once

#define TCIFLUSH 0
#define TCIOFLUSH 2

int tcflush(int fd, int queueSelector);
