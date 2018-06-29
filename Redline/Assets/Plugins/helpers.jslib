mergeInto( LibraryManager.library, {
  RemoveLoader: function() {
    console.log("Loading finished!");
    document.getElementById('loader').remove();
  },

  GetSetNumber: function() {
    let params = new URLSearchParams(location.search.slice(1));
    return params.get('set');
  }

  GetBarType: function() {
    let params = new URLSearchParams(location.search.slice(1));
    return params.get('bar');
  }
});
