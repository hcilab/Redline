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

    /**
    * @return {number}
    */
    GetGender: function() {
      return !isNaN(GetParams().gender) && isFinite( GetParams().gender ) ? GetParams().gender : -1;
    },

    /**
    * @return {string}
    */
    GetHostAddress: function() {
      var host = window.location.hostname;
      var bufferSize = lengthBytesUTF8(host) + 1;
      var buffer = _malloc(bufferSize);
      stringToUTF8(host, buffer, bufferSize);
      return buffer;
    },

    /**
    * @return
    */
    RedirectOnEnd: function () {
        window.location.href = "/redirect_next_page";
    }
});
