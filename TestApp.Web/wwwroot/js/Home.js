var Home = {
    init: () => {
        $(window).scroll(Home.actions.updateClone);
        $(window).resize(Home.actions.updateClone);
    },
    elements: {
        scrollClone: () => $("#scroll-clone"),
        fixedClone: () => $("#fixed-clone")
    },
    actions: {
        updateClone: () => {
            var rect = Home.elements
                .scrollClone()[0]
                .getBoundingClientRect();

            Home.elements
                .fixedClone()
                .css(
                    {
                        "display": rect.top < 0 ? "block" : "none",
                        "width": rect.width + "px"
                    });
        }
    }
};
$(Home.init);
