#include <stdlib.h>
#include <stdio.h>
#include "writer.h"
#include "generated/components.h"

int main(int arg_c, char **arg_v)
{
    {
        writer_t writer = {0};
        writer_append(&writer, "Hello, world!");
        writer_append(&writer, " Foo bar\n");

        char *result = writer_tostr(&writer);
        writer_free(&writer);

        printf("%s\n", result);
        free(result);
    }

    {
        Test_t test = { .count = 1 };
        char *test_html = Test_render(&test);
        
        printf("%s\n", test_html);

        free(test_html);
    }
}