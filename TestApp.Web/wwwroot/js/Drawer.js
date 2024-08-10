var Drawer = {
    toggle: async (el) => {
        var startScroll = document.documentElement.scrollTop;
        var startState = $(el).data("state");
        await dispatch(el, 'Toggle');
        if (startState === "closed") {
            window.scrollTo({ top: startScroll, left: 0, behavior: "instant" });
        }
    }
};
