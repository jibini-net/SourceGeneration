var Drawer = {
    toggle: async (el) => {
        var startScroll = document.documentElement.scrollTop;
        var startDocHeight = document.documentElement.scrollHeight;
        var startState = $(el).data("state");

        await dispatch(el, "Toggle");

        if (startState === "open") {
            var offsetFromHeight = startScroll - startDocHeight;
            startScroll = document.documentElement.scrollHeight + offsetFromHeight;
        }
        window.scrollTo({ top: startScroll, left: 0, behavior: "instant" });
    }
};
