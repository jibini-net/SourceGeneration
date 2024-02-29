#include "writer.h"

#include <string.h>
#include <stdlib.h>

#define _RZ_MALLOC       malloc
#define _RZ_FREE         free
#define _RZ_MEMCPY       memcpy
#define _RZ_STRLEN       strlen
#define _RZ_MAX(a, b)    ((a > b) ? (a) : (b))
#define _RZ_MIN(a, b)    ((a < b) ? (a) : (b))

void _writer_add_page(writer_t *ptr)
{
    writer_page_t *new_page = (writer_page_t *)_RZ_MALLOC(sizeof(writer_page_t));
    new_page->_prev = ptr->head;
    
    ptr->head = new_page;
    ptr->end_of_page = &new_page->buffer[0];
}

void writer_append(writer_t *ptr, char *segment)
{
    size_t remaining = _RZ_STRLEN(segment);
    while (*segment != '\0')
    {
        if (!ptr->head 
            || ptr->end_of_page > &ptr->head->buffer[WRITER_PAGE_SIZE - 1])
        {
            _writer_add_page(ptr);
        }

        size_t to_copy = _RZ_MIN(
            WRITER_PAGE_SIZE - (ptr->end_of_page - &ptr->head->buffer[0]),
            remaining);

        _RZ_MEMCPY(ptr->end_of_page, segment, to_copy);

        remaining -= to_copy;
        segment = &segment[to_copy];

        ptr->end_of_page = &ptr->end_of_page[to_copy];
    }
}

void _writer_tostr(writer_t *ptr, writer_page_t *page, size_t offset, char **out_buff, size_t *len)
{
    // Page sizes are summed while recursively traversing the chain
    size_t page_len = WRITER_PAGE_SIZE;
    if (ptr->head == page)
    {
        page_len = ptr->end_of_page - &ptr->head->buffer[0];
    }

    if (page->_prev)
    {
        _writer_tostr(ptr, page->_prev, offset + page_len, out_buff, len);
    } else
    {
        // Output buffer and length are created at deepest recursion
        *len = offset + page_len;
        *out_buff = (char *)_RZ_MALLOC(*len + 1);
        (*out_buff)[*len] = '\0';
    }

    // Segments are copied from individual page buffers as recursion is exiting
    // (the final size of the output buffer is known)
    char *write_to = &(*out_buff)[*len - offset - page_len];
    _RZ_MEMCPY(write_to, page->buffer, page_len);
}

char *writer_tostr(writer_t *ptr)
{
    if (!ptr->head)
    {
        return NULL;
    }

    char *result;
    size_t len;
    _writer_tostr(ptr, ptr->head, 0, &result, &len);

    return result;
}

void writer_free(writer_t *ptr)
{
    for (writer_page_t *next; ptr->head; ptr->head = next)
    {
        next = ptr->head->_prev;
        _RZ_FREE(ptr->head);
    }
    ptr->end_of_page = NULL;
}