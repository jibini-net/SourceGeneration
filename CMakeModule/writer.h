#pragma once
#include <stddef.h>

#define WRITER_PAGE_SIZE (size_t)512

typedef struct writer_page
{
    char buffer[WRITER_PAGE_SIZE];
    struct writer_page *_prev;
} writer_page_t;

typedef struct writer
{
    writer_page_t *head;
    char *end_of_page;
} writer_t;

void writer_append(writer_t *ptr, char *segment);
char *writer_tostr(writer_t *ptr);
void writer_free(writer_t *ptr);