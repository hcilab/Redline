mergeInto(LibraryManager.library, {
    RemoveLoader: function () {
        console.log("Loading finished!");
        document.getElementById('loader').remove();
    },

    /**
     * @return {number}
     */
    GetSetNumber: function () {
        return !isNaN(GetParams().set) && isFinite(GetParams().set) ? GetParams().set : 2;
    },

    /**
     * @return {number}
     */
    GetId: function () {
        return !isNaN(GetParams().pid) && isFinite(GetParams().pid) ? GetParams().pid : 123456789;
    },

    /**
     * @return {number}
     */
    GetBarType: function () {
        return !isNaN(GetParams().condition) && isFinite( GetParams().condition ) ? GetParams().condition : -1;
    },

    RedirectOnEnd: function () {
        window.location.href = "/redirect_next_page";
    }
});
