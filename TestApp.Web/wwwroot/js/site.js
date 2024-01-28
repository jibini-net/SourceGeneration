var Layout = {
    init: () => {
        setInterval(Layout.actions.toggleBlinkCursor, 1000);
    },
    actions: {
        toggleBlinkCursor: () => $(".blink-cursor").toggleClass("d-none")
    }
};
$(Layout.init);
