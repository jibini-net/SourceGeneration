
#include "Test.view.g.h"

    // State dumps and restorations are not supported
    void _Test_render(Test_t *state, writer_t *writer)
    {
        writer_append(writer, "<!--Test-->");
        if (state->count) {
        {

        writer_append(writer, ({
                SubComponent_t component = {};
                _RZ_MEMSET(&component, 0, sizeof(SubComponent_t));
                component.content = ("Flex is epic");
                //CORE
                //component.Children.AddRange(new RenderDelegate[] {
                //    
                //});
                _SubComponent_render(&component, writer);
                "";
            }));
        
        }
        }
        else {
        writer_append(writer, "Not epic");
        }
        writer_append(writer, "<!--/Test-->");
    }
    char *Test_render(Test_t *state)
    {
        writer_t writer = {0};
        _Test_render(state, &writer);
        char *result = writer_tostr(&writer);
        writer_free(&writer);
        return result;
    }
// View action controllers are not supported
//}

