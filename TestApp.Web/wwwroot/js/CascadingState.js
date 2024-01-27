var CascadingState = {
    setLoggedIn: async (el, loggedIn) => {
        var cascade = el.closest("._CascadingState");
        await dispatch(cascade, "SetLoggedIn", {
            loggedIn: loggedIn
        });
    },
    logIn: async (el) => await CascadingState.setLoggedIn(el, { Id: 1, FirstName: "John" }),
    logOut: async (el) => await CascadingState.setLoggedIn(el, {})
};
