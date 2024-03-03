# "Rezacht Core"
Descriptor language transpiler for templated web content in C

Visit the [wiki](https://github.com/jibini-net/SourceGeneration/wiki) for basic syntax and usage instructions.

This is a port for microcontrollers which omits
 - Reactive UI and supporting JS
 - Component action interfaces
 - Generated view controllers
 - Generated SQL data layers
 - HTML escaping and some security handling
 - Some child-parent relationship features
 - Dependency injection pipeline

Primarily, this port allows
 - Efficient concatenation of numerous strings
 - Component state variables
 - Rendering of static HTML
 - Component and sub-component rendering
 - Conditional and flow control logic

Component state in dynamic memory must be managed externally. Generated HTML and content from `writer_t` must be freed manually.

---

## Static templated HTML content

Components are defined in a custom declarative markup syntax. Components
are written as `.view` files and (in the .NET realm) tie into DI and routing to provide
serverside rendering of content. The core implementation provides static HTML generation
from templates with replacement values and control flow logic.

Components can rely on each other (including recursively) and can be broken up to keep
code tidy.

### State

Each component has a set of fields which can be referenced via the `state` pointer.

```
state {
    int count,
    {char *}message = {"Default C value expression"}
}
```

### HTML Nodes

Document markup is written in fully parenthesized prefix notation. An HTML element
is denoted as a lowercase identifier followed by a parenthesized list of parameters
and children.

```
p(<">Hello, world!</">)
```

```
div(| class = {"bg-dark text-light"} |
    p(<">Foo bar</">)
)
```

There are keywords and special tag types which indicate special generated constructs
or view logic (`if`, `for`, etc.):

```
if({state->count > 0}

    h1(<">Success block</">)
    
    h1(<">Failure block</">)
)

for({int i = 0; i < 10; i++}

 <">Repeated text</">
)
```

Multiple nodes can be grouped into one using `<>` and `</>` and multiline strings
surrounded with `<">` and `</">`.

```
if({state-> count > 0}

    <>
    h1(<">Success block</">)
    p(<">More content</">)
    </>
    
    p(
        <">
        Failure block
        with more lines this time
        </">
    )
)
```

Visit the [wiki](https://github.com/jibini-net/SourceGeneration/wiki) for more indepth
specifications of the HTML element grammar. Some features will differ.
