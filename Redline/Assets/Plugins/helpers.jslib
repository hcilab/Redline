mergeInto(LibraryManager.library, {
    RemoveLoader: function () {
        console.log("Loading finished!");
        document.getElementById('loader').remove();
    },

    /**
     * @return {number}
     */
    GetSetNumber: function () {
        return GetParams().set;
    },

    /**
     * @return {number}
     */
    GetId: function () {
        return GetParams().pid;
    },

    /**
     * @return {number}
     */
    GetBarType: function () {
        return -1;
    }
});
