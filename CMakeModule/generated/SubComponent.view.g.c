
#include "SubComponent.view.g.h"

    // State dumps and restorations are not supported
    void _SubComponent_render(SubComponent_t *state, writer_t *writer)
    {
        writer_append(writer, "<!--SubComponent-->");
        writer_append(writer, ("<div"));writer_append(writer, " class=\"");writer_append(writer, "d-flex");writer_append(writer, ("\">"));
        writer_append(writer, ("<h4>"));
        writer_append(writer, state->content ? state->content : "");
        writer_append(writer, "</h4>");
        writer_append(writer, "</div>");
        writer_append(writer, "\n");
        writer_append(writer, "    <h1>Verbatim HTML and Templated Content (");
        writer_append(writer, state->content);
        writer_append(writer, ")</h1>\n");
        writer_append(writer, "");
        writer_append(writer, "<!--/SubComponent-->");
    }
    char *SubComponent_render(SubComponent_t *state)
    {
        writer_t writer = {0};
        _SubComponent_render(state, &writer);
        char *result = writer_tostr(&writer);
        writer_free(&writer);
        return result;
    }
// View action controllers are not supported
//}

